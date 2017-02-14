//'use strict';
// This is the Smart Home skill for the Alexa-Premise bridge.
var https = require('https');
var AWS = require("aws-sdk");
var log = log;

//var REMOTE_CLOUD_BASE_PATH = "/Alexa.svc/jsons/";
//var REMOTE_CLOUD_HOSTNAME = "alexa.quigleys.us";
//var REMOTE_CLOUD_PORT = 8733;

var healthyResponse = {
    "header": {
        "messageId": "",
        "namespace": "Alexa.ConnectedHome.System",
        "name": "HealthCheckResponse",
        "payloadVersion": "2"
    },
    "payload": {
        "isHealthy": true,
        "description": "The system is currently healthy"
    }
};

exports.handler = function (event, context) {

    //log('alexaEventHandler', event);

    switch (event.header.namespace) {

        case 'Alexa.ConnectedHome.System':
            if (event.header.name == 'HealthCheckRequest') {
                log('HealthCheckRequest', event);
                healthyResponse.header.messageId = event.header.messageId;
                context.succeed(JSON.parse(healthyResponse));
            }
            break;
        case 'Alexa.ConnectedHome.Control':
            getCustomerProfile(event, context, 'Control');
            break;
        case 'Alexa.ConnectedHome.Discovery':
            getCustomerProfile(event, context, 'Discovery');
            break;
        case 'Alexa.ConnectedHome.Query':
            getCustomerProfile(event, context, 'Query');
            break;

        default:
            // Warning! Logging this in production might be a security problem.
            log('Err', 'No supported namespace: ' + event.header.namespace);
            context.fail('Command Not Supported.');
            break;
    }
};

function getCustomerProfile(event, context, command) {

    // prepare request options
    var get_options = {
        host: 'api.amazon.com',
        port: 443,
        path: '/user/profile',
        method: 'GET',
        headers: {
            'x-amz-access-token': event.payload.accessToken,
            'Accept': 'application/json',
            'Accept-Language': 'en-US'
        }
    };

    //log('getCustomerProfileRequest', get_options);

    // Set up the request
    var result = "";

    var get_req = https.request(get_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
            var customer_info = JSON.parse(result);
            log('getCustomerProfileResponse', JSON.stringify(customer_info));
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

    //log("getCustomerEndpointRequest", params);

    dynamodb.getItem(params, function (err, data) {

        if (err) {
            log('Database Error', err.stack);
            return;
        } else if (data.Item === undefined) {
            log('getCustomerEndpointResponse', 'Record not found for ' + customer_info.user_id);
            context.fail('Failed to find customer endpoint.');
        } else {
            //log('getCustomerEndpointResponse', 'Record found for ' + customer_info.email);
            proxyEvent(event, context, command, data.Item);
        }

    });
}

function proxyEvent(event, context, command, customer_endpoint) {

    // replace the accessToken with the one from the customer db account
    event.payload.accessToken = customer_endpoint.access_token.S;

    // Set up the request
    var post_data = JSON.stringify(event, 'utf-8');
    log('proxyEventData', JSON.stringify(JSON.parse(post_data)));

    // prepare request options
    var post_options = {
        host: customer_endpoint.host.S,                 // REMOTE_CLOUD_HOSTNAME,
        port: customer_endpoint.port.S,                 // REMOTE_CLOUD_PORT,
        path: customer_endpoint.app_path.S + command + '/', //path: REMOTE_CLOUD_BASE_PATH + command + '/',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Content-Length': post_data.length
        }
    };
    //log('proxyEventOptions', post_options);

    var result = "";
    var post_req = https.request(post_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {

            var directive = JSON.parse(result);
            log('proxyDirective', JSON.stringify(directive));

            //if (directive.payload.applianceResponseTimestamp !== undefined) {
            //    delete directive.payload.applianceResponseTimestamp;
            //    log('proxyDirectiveFixed', JSON.stringify(directive));
            //}
            context.succeed(directive);
        });

        response.on('error', function (e) {
            console.log('Err', e.message);
            context.fail('Request to appliance cloud failed.');
        });
    });

    post_req.write(post_data);
    post_req.end();
}

function log(title, msg) {
    console.log('************** ' + title + ' *************');
    console.log(msg);
    console.log('************ ' + title + ' End ***********');
}