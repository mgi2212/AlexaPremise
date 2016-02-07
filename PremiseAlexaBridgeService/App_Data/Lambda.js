var https = require('https');
var REMOTE_CLOUD_BASE_PATH = "/PremiseAlexaBridgeService.svc/json/";
var REMOTE_CLOUD_HOSTNAME = "alexa.quigleys.us";
var REMOTE_CLOUD_PORT = 8733;
var log = log;
var healthyResponse = {
    "header": {
        "namespace": "System",
        "name": "HealthCheckResponse",
        "payloadVersion": "1"
    },
    "payload": {
        "isHealthy": true,
        "description": "The system is currently healthy"
    }
};

exports.handler = function (event, context) {

    // log('Input', event);

    switch (event.header.namespace) {

        case 'System':
            if (event.header.name == 'HealthCheckRequest') {
                context.succeed(JSON.parse(healthyResponse));
            }
            break;
        case 'Control':
            proxyEventToCustomer(event, context, 'Control');
            break;
        case 'Discovery':
            proxyEventToCustomer(event, context, 'Discovery');
            break;

        default:
            // Warning! Logging this in production might be a security problem.
            log('Err', 'Namespace Not Supported: ' + event.header.namespace);
            context.fail('Something went wrong');
            break;
    }
};

function proxyEventToCustomer(event, context, path) {

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

    //log('getCustomerInfoRequest', get_options);
    var result = "";

    // Set up the request
    var get_req = https.request(get_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
            //log('BeforeProxy', JSON.stringify(JSON.parse(result)));
            var json_result = JSON.parse(result);
            event.payload.accessToken = json_result.user_id; // the on prem system expects the amazon user id from this call
            proxyEvent(event, context, path);
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


function proxyEvent(event, context, path) {

    var post_data = JSON.stringify(event, 'utf-8');
    //log('proxyEvent', post_data);

    // prepare request options
    var post_options = {
        host: REMOTE_CLOUD_HOSTNAME,
        port: REMOTE_CLOUD_PORT,
        path: REMOTE_CLOUD_BASE_PATH + path + '/',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Content-Length': post_data.length
        }
    };

    // Set up the request

    var result = "";
    var post_req = https.request(post_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
            //log('Response', JSON.stringify(JSON.parse(result)));
            context.succeed(JSON.parse(result));
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
    console.log('*************** ' + title + ' *************');
    console.log(msg);
    console.log('************* ' + title + ' End ***********');
}