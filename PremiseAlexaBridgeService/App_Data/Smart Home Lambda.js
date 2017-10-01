'use strict';
var https = require('https');
var AWS = require("aws-sdk");
var log = log;

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

// handles events from Alexa service
exports.handler = function (event, context) {

    switch (event.header.namespace) {

        case 'Alexa.ConnectedHome.System':
            if (event.header.name === 'HealthCheckRequest') {
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

// queries amazon for customer profile information (name, email, user_id)
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

    // Set up the request
    var result = "";
    var get_req = https.request(get_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
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

// Queries a dynamoDb table for customer endpoint using user_id as the key
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

// proxies the event to the AlexaPremise service running locally at the customer endpoint URI
// requires globally valid ssl cert for the local service
function proxyEvent(event, context, command, customer_endpoint) {

    // replace the accessToken with the one from the customer db account
    event.payload.accessToken = customer_endpoint.access_token.S;

    // Set up the request
    var post_data = JSON.stringify(event, 'utf-8');

    // prepare request options
    var post_options = {
        host: customer_endpoint.host.S,                     // REMOTE_CLOUD_HOSTNAME,
        port: customer_endpoint.port.S,                     // REMOTE_CLOUD_PORT,
        path: customer_endpoint.app_path.S + command + '/', // path: REMOTE_CLOUD_BASE_PATH + command + '/',
        method: 'POST',
        headers: {
            'Content-Type': 'text/plain',                   // Endpoint uses newtonsoft deserialization which requires a raw post
            'Content-Length': post_data.length
        }
    };
    log('Customer Endpoint', post_options);
    log('Directive', JSON.stringify(JSON.parse(post_data)));

    var result = "";
    var post_req = https.request(post_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {

            var directive = JSON.parse(result);
            log('Response', JSON.stringify(directive));
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
    console.log(':' + title + ':');
    console.log(msg);
}