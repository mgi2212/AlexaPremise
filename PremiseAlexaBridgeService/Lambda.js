var https = require('https');
var REMOTE_CLOUD_BASE_PATH = "/PremiseAlexaBridgeService.svc/json/";
var REMOTE_CLOUD_HOSTNAME = "alexa.yourdomain";
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

    log('Input', event);

    switch (event.header.namespace) {

        case 'System':
            if (event.header.name == 'HealthCheckRequest') {
                context.succeed(JSON.parse(healthyResponse));
            }
            break;
        case 'Control':
            proxyEvent(event, context, 'Control');
            break;
        case 'Discovery':
            proxyEvent(event, context, 'Discovery');
            break;

        default:
            // Warning! Logging this in production might be a security problem.
            log('Err', 'No supported namespace: ' + event.header.namespace);
            context.fail('Something went wrong');
            break;
    }
};

function proxyEvent(event, context, path) {

    // this is where we need to look up the endpoint for a customer
    event.payload.accessToken = "random";

    var post_data = JSON.stringify(event, 'utf-8');

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

    var result = "";

    // Set up the request
    var post_req = https.request(post_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
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
    console.log('*************** ' + title + ' Begin **********');
    console.log(msg);
    console.log('*************** ' + title + ' End*************');
}
