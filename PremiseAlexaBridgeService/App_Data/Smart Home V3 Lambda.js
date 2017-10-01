'use strict';
var https = require('https');
var http = require('http');
var AWS = require("aws-sdk");
var log = log;

function log(title, msg) {
    console.log(':' + title + ':');
    console.log(msg);
}

var my_client_secret = 'd9bd211cb8c9af7b8db1eb3ba52cc9b31fab94604cbf6804110788e79fbb535e';
var my_client_id = 'amzn1.application-oa2-client.76f9bb6cb75a4eb18b9886f9c3d32631';
var LWA_TOKEN_URI = "https://api.amazon.com/auth/o2/token";
var LWA_HEADERS = {
    "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8"
};

const healthyResponse = {
    "header": {
        "messageId": "",
        "namespace": "Alexa.ConnectedHome.System",
        "name": "HealthCheckResponse",
        "payloadVersion": "2"
    },
    "payload": {
        "isHealthy": true,
        "description": "The system is currently healthy."
    }
};

exports.handler = function (event, context) {

    var customer_endpoint = '';
    log('event', JSON.stringify(event));

    switch (event.directive.header.namespace) {

        case 'Alexa':
            if (event.directive.header.name === "ReportState") {
                getCustomerProfile(event, context, "ReportState");
            }
            break;
        case 'Alexa.Discovery':
            getCustomerProfile(event, context, "Discovery");
            break;
        case 'Alexa.PowerController':
            getCustomerProfile(event, context, "Control/SetPowerState");
            break;
        case 'Alexa.SceneController':
            getCustomerProfile(event, context, "Control/Scene");
            break;
        case 'Alexa.BrightnessController':
            if (event.directive.header.name === 'SetBrightness') {
                getCustomerProfile(event, context, "Control/" + event.directive.header.name);
            }
            else if (event.directive.header.name === 'AdjustBrightness') {
                getCustomerProfile(event, context, "Control/" + event.directive.header.name);
            }
            break;
        case 'Alexa.ColorTemperatureController':
        case 'Alexa.ColorController':
            getCustomerProfile(event, context, "Control/" + event.directive.header.name);
            break;
        case 'Alexa.Authorization':
            if (event.directive.header.name === 'AcceptGrant') {

                var lwa_params = 'grant_type=authorization_code';
                lwa_params += '&code=' + event.directive.payload.grant.code;
                lwa_params += '&client_id=' + my_client_id;
                lwa_params += '&client_secret=' + my_client_secret;

                callLWA(lwa_params, event, context, customer_endpoint);
            }
            break;

        default:
            // Warning! Logging this in production might be a security problem.
            log('Err', 'No supported namespace: ' + event.header.namespace);
            context.fail('Command Not Supported.');
            break;
    }
};

function callLWA(data, event, context, customer_endpoint) {
    // prepare request options
    var post_options = {
        host: 'api.amazon.com',
        port: 443,
        path: '/auth/o2/token',
        method: 'POST',
        headers: LWA_HEADERS
    };


    // Set up the request
    var result = "";
    var post_req = https.request(post_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
            var lwa_info = JSON.stringify(result);
            log("lwa_info", lwa_info);
            event.directive.payload.grant = JSON.parse(result);
            event.directive.payload.grant.client_id = my_client_id;
            event.directive.payload.grant.client_secret = my_client_secret;
            event.directive.payload.grantee.localAccessToken = 't110ftc0d';
            proxyEvent(event, context, "Authorization", customer_endpoint);
        });

        response.on('error', function (e) {
            console.log('Err', e.message);
            context.fail('Request to appliance cloud failed authorization.');
        });
    });
    log("out to lwa", data);
    post_req.write(data);
    post_req.end();
}

function getCustomerProfile(event, context, command) {

    // prepare request options
    var get_options = {
        host: 'api.amazon.com',
        port: 443,
        path: '/user/profile',
        method: 'GET',
        headers: {
            'x-amz-access-token': getBearerToken(event),
            'Accept': 'application/json',
            'Accept-Language': 'en-US'
        }
    };

    // Set up the request
    var result = "";
    var get_req = https.request(get_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
            log("customer_info", result);
            var customer_info = JSON.parse(result);
            getCustomerEndpoint(event, context, command, customer_info);
        });

        response.on('error', function (e) {
            console.log('Err', e.message);
            context.fail('Request to appliance cloud failed authorization.');
        });
    });

    var get_data = "";
    get_req.write(get_data);
    get_req.end();
}

function getCustomerEndpoint(event, context, command, customer_info) {

    var dynamodb = new AWS.DynamoDB();

    var params = {
        TableName: 'PremiseBridgeCustomer',
        Key: {
            "id": {
                "S": customer_info.user_id
            }
        }
    };

    dynamodb.getItem(params, function (err, data) {

        if (err) {
            log('Database Error', err.stack);
            return;
        } else if (data.Item === undefined) {
            log('getCustomerEndpointResponse', 'Record not found for ' + customer_info.user_id);
            context.fail('Failed to find customer endpoint.');
        } else {
            proxyEvent(event, context, command, data.Item);
        }
    });
}

function proxyEvent(event, context, command, customer_endpoint) {

    setLocalAccessToken(event, customer_endpoint);
    var post_data = JSON.stringify(event, 'utf-8');

    // prepare request options
    var post_options = {
        host: customer_endpoint.host.S,                     // REMOTE_CLOUD_HOSTNAME,
        port: customer_endpoint.port.S,                     // REMOTE_CLOUD_PORT,
        path: '/AlexaV3.svc/json/' + command + '/',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Content-Length': post_data.length
        }
    };
    log('Customer Endpoint', post_options);
    log('Send Directive', post_data);

    var result = "";
    var post_req = http.request(post_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {

            var jsonResult = JSON.parse(result);
            cleanUpResponse(event, jsonResult);

            log('Recv Response', JSON.stringify(jsonResult));

            context.succeed(jsonResult);
        });

        response.on('error', function (e) {
            console.log('Err', e.message);
            context.fail('Request to appliance cloud failed.');
        });
    });

    post_req.write(post_data);
    post_req.end();
}

function cleanUpResponse(event, response) {

    log('clean-up', response);
    var clean = false;

    switch (event.directive.header.namespace) {
        case 'Alexa.PowerController':
        case 'Alexa.BrightnessController':
        case 'Alexa.ColorController':
        case 'Alexa.ColorTemperatureController':
            clean = true;
            break;
    }

    if (clean === true) {
        response.context.properties.forEach(function (prop) {
            switch (prop.namespace) {
                case 'Alexa.ColorController':
                case 'Alexa.EndpointHealth':
                    if (prop.value.hasOwnProperty('__type')) {
                        delete prop.value.__type;
                    }
                    break;
            }
        });
    }
}

function setLocalAccessToken(event, customer_endpoint) {

    var local_access_token = customer_endpoint.access_token.S;

    switch (event.directive.header.namespace) {
        case 'Alexa':
            if (event.directive.header.name === "ReportState") {
                event.directive.endpoint.scope.localAccessToken = local_access_token;
            }
            break;
        case 'Alexa.Discovery':
            event.directive.payload.scope.localAccessToken = local_access_token;
            break;
        case 'Alexa.PowerController':
        case 'Alexa.SceneController':
        case 'Alexa.BrightnessController':
        case 'Alexa.ColorController':
        case 'Alexa.ColorTemperatureController':
            event.directive.endpoint.scope.localAccessToken = local_access_token;
            break;
        default:
            break;
    }
}

function getBearerToken(event) {
    var token = '';
    switch (event.directive.header.namespace) {
        case 'Alexa':
            if (event.directive.header.name === "ReportState") {
                token = event.directive.endpoint.scope.token;
            }
            break;
        case 'Alexa.Discovery':
            token = event.directive.payload.scope.token;
            break;
        case 'Alexa.PowerController':
        case 'Alexa.SceneController':
        case 'Alexa.BrightnessController':
        case 'Alexa.ColorController':
        case 'Alexa.ColorTemperatureController':
            token = event.directive.endpoint.scope.token;
            break;
        default:
            break;
    }
    return token;
}
