"use strict";
var https = require("https");
var aws = require("aws-sdk");

/**
 * Constants
 */
const lwaHost = "api.amazon.com";
const lwaAuthPath = "/auth/o2/token";
const lwaProfilePath = "/user/profile";
const requiredEnvironmentVars = ["skill_client_secret", "skill_client_id", "customer_table", "device_cloud_path"];

/**
 * Environment variables
 */
var skillClientSecret = process.env.skill_client_secret;
var skillClientId = process.env.skill_client_id;
var customerTable = process.env.customer_table;

/**
 * Simple Log helper
 *
 * @param {any} title
 * @param {any} msg
 */
function log(title, msg) {
    console.log(`${title}:`);
    console.log(msg);
}

/**
 * Check for missing environment variables.
 */
function validateEnvironment() {
    for (var i = 0, len = requiredEnvironmentVars.length; i < len; i++) {
        if (!process.env.hasOwnProperty(requiredEnvironmentVars[i])) {
            console.log(`Required ${requiredEnvironmentVars[i]} variable missing in environment!`);
            return i;
        }
    }
    return -1;
}

/**
 * This routine runs when the lambda is loaded and logs any missing environment variables.
 */
const i = validateEnvironment();
if (i > -1) {
    console.log(`Required environment variable ${requiredEnvironmentVars[i]} is missing.`);
    return;
}

/**
 * This is the main entry point for events sent to this lambda function.  It filters out
 * Cloudwatch keep-alive events and determines the route to use on the premise hub service.
 *
 * @param {} event
 * @param {} context
 * @returns {}
 */
exports.handler = function (event, context) {
    log("event", JSON.stringify(event));

    // filter out scheduled keep-alive events
    if (event.hasOwnProperty("detail-type")) {
        if (event["detail-type"] === "Scheduled Event") {
            context.succeed(event);
        } else {
            context.fail("Unknown event.");
        }
        return;
    }

    const directive = {
        start: new Date(),
        msInLambda: 0,
        command: event.directive.header.namespace,
        device_cloud_path: process.env.device_cloud_path.replace(/\/?$/, "/"),
        customer_data: {},
        endpoint: {}
    };

    switch (event.directive.header.namespace) {
        case "Alexa":
            if (event.directive.header.name === "ReportState") {
                directive.device_cloud_path += "ReportState/";
                getCustomerProfile(event, context, directive);
            }
            break;

        case "Alexa.Discovery":
            directive.device_cloud_path += "Discovery/";
            getCustomerProfile(event, context, directive);
            break;

        case "Alexa.PowerController":
            directive.device_cloud_path += "Control/SetPowerState/";
            getCustomerProfile(event, context, directive);
            break;

        case "Alexa.SceneController":
            directive.device_cloud_path += "Control/Scene/";
            getCustomerProfile(event, context, directive);
            break;

        case "Alexa.BrightnessController":
        case "Alexa.ThermostatController":
        case "Alexa.ColorTemperatureController":
        case "Alexa.ColorController":
            directive.device_cloud_path += `Control/ ${event.directive.header.name} / `;
            getCustomerProfile(event, context, directive);
            break;

        case "Alexa.Speaker":
            directive.device_cloud_path += "Control/Speaker/";
            getCustomerProfile(event, context, directive);
            break;

        case "Alexa.InputController":
            directive.device_cloud_path += "Control/InputController/";
            getCustomerProfile(event, context, directive);
            break;

        case "Alexa.Authorization":
            if (event.directive.header.name === "AcceptGrant") {
                directive.device_cloud_path += "Authorization/";
                authorizeLwa(event, context, directive);
            }
            break;

        default:
            {
                const message = `Namespace unsupported: ${event.directive.header.namespace}`;
                log("Error", message);
                context.fail(message);
            }
            break;
    }
};

/**
 * This function is only called during the skill linking process, and provides the first
 * access and refresh tokens for sending proactive state updates (psu) to Alexa. It also
 * returns an expiry time in seconds.  These values are sent to the premise hub service
 * and stored there. Subsequent refresh token requests are handled by the premise hub service.
 *
 * @param {any} event
 * @param {any} context
 * @param {any} directive
 */
function authorizeLwa(event, context, directive) {
    // prepare request options
    const options = {
        host: lwaHost,
        port: 443,
        path: lwaAuthPath,
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8"
        }
    };

    var result = "";
    const post = https.request(options, function (response) {
        response.setEncoding("utf-8");

        response.on("data", function (chunk) {
            result += chunk;
        });

        response.on("end", function () {
            event.directive.payload.grant = JSON.parse(result);
            event.directive.payload.grant.client_id = skillClientId;
            event.directive.payload.grant.client_secret = skillClientSecret;
            getCustomerProfile(event, context, directive);
        });

        response.on("error", function (e) {
            const message = `Error: Request to Login with Amazon failed authorization with error:${e.message} `;
            console.log(message);
            context.fail(message);
        });
    });

    let data = "grant_type=authorization_code";
    data += `& code=${event.directive.payload.grant.code} `;
    data += `& client_id=${skillClientId} `;
    data += `& client_secret=${skillClientSecret} `;

    post.write(data);
    post.end();
}

/**
 * Retrieves customer profile information from Amazon, which includes the customer name, email and ID.
 *
 * @param {any} event
 * @param {any} context
 * @param {any} directive
 */
function getCustomerProfile(event, context, directive) {
    // prepare request options
    const options = {
        host: lwaHost,
        port: 443,
        path: lwaProfilePath,
        method: "GET",
        headers: {
            'x-amz-access-token': getBearerToken(event),
            'Accept': "application/json",
            'Accept-Language': "en-US"
        }
    };

    // define a buffer for the response
    var result = "";
    const request = https.request(options, function (response) {
        response.setEncoding("utf-8");

        response.on("data", function (chunk) {
            result += chunk;
        });

        response.on("end", function () {
            directive.customer_data = JSON.parse(result);
            getCustomerEndpoint(event, context, directive);
        });

        response.on("error", function (e) {
            const message = `Request for customer profile failed with error:${e.message}.`;
            console.log(message);
            context.fail(message);
        });
    });

    const data = "";
    request.write(data);
    request.end();
}

/**
 * Retrieves the customer record in a dynamoDb table that contains the endpoint (IP/DNS)
 * address of the customer's premise hub and a customer supplied access token.
 *
 * @param {any} event
 * @param {any} context
 * @param {any} directive
 */
function getCustomerEndpoint(event, context, directive) {
    const dynamodb = new aws.DynamoDB();

    const params = {
        TableName: customerTable,
        Key: {
            "id": {
                "S": directive.customer_data.user_id
            }
        }
    };

    dynamodb.getItem(params, function (error, data) {
        if (error) {
            log("Database Error", error.stack);
            context.fail(`Error accessing customer table${error.message} `);
        } else if (data.Item === undefined) {
            log("getCustomerEndpointResponse", `Customer record not found for ${directive.customer_data.user_id}:${directive.customer_data.email} `);
            context.fail("Failed to find customer endpoint.");
        } else {
            command.endpoint = data;
            sendDirective(event, context, directive);
        }
    });
}

/**
 * Sends the directive to the premise hub service and returns the response.
 *
 * @param {any} event
 * @param {any} context
 * @param {any} directive
 */
function sendDirective(event, context, directive) {
    var start = new Date();

    setLocalAccessToken(event, directive);

    const data = JSON.stringify(event, "utf-8");

    // prepare post options
    const options = {
        host: directive.endpoint.host.S,
        port: directive.endpoint.port.S,
        path: directive.app_path,
        method: "POST",
        headers: {
            'Content-Type': "application/json",
            'Content-Length': data.length
        }
    };

    var result = "";
    var postReq = https.request(options, function (response) {
        response.setEncoding("utf-8");

        response.on("data", function (chunk) {
            result += chunk;
        });

        response.on("end", function () {
            var jsonResult = JSON.parse(result);
            cleanUpResponse(event, jsonResult);
            const end = new Date() - start;
            console.info("Round trip time: %dms", end);
            log("Response", JSON.stringify(jsonResult));
            context.succeed(jsonResult);
        });

        response.on("error", function (e) {
            const message = `Error: Directive to client cloud failed with error:${e.message} `;
            console.log(message);
            context.fail(message);
        });
    });

    postReq.on("socket", function (socket) {
        socket.setTimeout(6000);
        socket.on("timeout", function () {
            postReq.abort();
        });
    });

    postReq.on("error", function (err) {
        var message;
        if (err.code === "ECONNRESET") {
            message = `Send directive to appliance cloud timed out. :${err.code} `;
            console.log(message);
            context.fail(message);
            return;
        }
        message = `Send directive received unexpected soccet error: ${err.code} `;
        console.log(message);
        context.fail(message);
    });

    directive.msInLambda = new Date() - directive.start;
    console.log(JSON.stringify(directive));

    postReq.write(data);
    postReq.end();
}

/**
 * Microsoft's serialization framework decorates json with __type value when
 * seralizing polymorphic objects, this routine removes them, so the json returned
 * to Alexa is correct.
 *
 * @param {any} event
 * @param {any} response
 */
function cleanUpResponse(event, response) {
    var clean = false;

    switch (event.directive.header.namespace) {
        case "Alexa.BrightnessController":
        case "Alexa.ColorController":
        case "Alexa.ColorTemperatureController":
        case "Alexa.InputController":
        case "Alexa.PowerController":
        case "Alexa.Speaker":
        case "Alexa.TemperatureSensor":
        case "Alexa.ThermostatController":
            clean = true;
            break;

        default:
    }

    switch (event.directive.header.name) {
        case "ReportState":
            clean = true;
            break;

        case "ErrorResponse":
            if (response.event.payload.hasOwnProperty("__type")) {
                delete response.event.payload.__type;
                return;
            }
            break;

        default:
    }

    if (clean !== true) {
        return;
    }

    response.context.properties.forEach(function (prop) {
        switch (prop.namespace) {
            case "Alexa.ColorController":
            case "Alexa.EndpointHealth":
            case "Alexa.TemperatureSensor":
            case "Alexa.ThermostatController":
                if (prop.value.hasOwnProperty("__type")) {
                    delete prop.value.__type;
                }
                break;
        }
    });
}

/**
 * The premise hub service requires an access token set by the customer and
 * stored in the dynamoDb table. This function adds it to the directive sent
 * to the hub.
 * @param {any} event
 * @param {any} directive
 */
function setLocalAccessToken(event, directive) {
    const localAccessToken = directive.customer_data.access_token.S;

    switch (event.directive.header.namespace) {
        case "Alexa":
            if (event.directive.header.name === "ReportState") {
                event.directive.endpoint.scope.localAccessToken = localAccessToken;
            }
            break;

        case "Alexa.Discovery":
            event.directive.payload.scope.localAccessToken = localAccessToken;
            break;

        case "Alexa.BrightnessController":
        case "Alexa.ColorController":
        case "Alexa.ColorTemperatureController":
        case "Alexa.InputController":
        case "Alexa.PowerController":
        case "Alexa.SceneController":
        case "Alexa.Speaker":
        case "Alexa.ThermostatController":
        case "Alexa.TemperatureSensor":
            event.directive.endpoint.scope.localAccessToken = localAccessToken;
            break;

        case "Alexa.Authorization":
            event.directive.payload.grantee.localAccessToken = localAccessToken;
            break;

        default:
            break;
    }
}

/**
 * This function returns the bearer token required to access the Amazon customer profile.
 * Required because the location of the token in the directive json is dependent on the
 * type of directive.
 * @param {any} event
 */
function getBearerToken(event) {
    var token = "";
    switch (event.directive.header.namespace) {
        case "Alexa":
            if (event.directive.header.name === "ReportState") {
                token = event.directive.endpoint.scope.token;
            }
            break;

        case "Alexa.Discovery":
            token = event.directive.payload.scope.token;
            break;

        case "Alexa.BrightnessController":
        case "Alexa.ColorController":
        case "Alexa.ColorTemperatureController":
        case "Alexa.InputController":
        case "Alexa.PowerController":
        case "Alexa.SceneController":
        case "Alexa.Speaker":
        case "Alexa.TemperatureSensor":
        case "Alexa.ThermostatController":
            token = event.directive.endpoint.scope.token;
            break;

        case "Alexa.Authorization":
            token = event.directive.payload.grantee.token;
            break;

        default:
            break;
    }
    return token;
}