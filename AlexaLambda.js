var https = require('http');
var REMOTE_CLOUD_BASE_PATH = "/Alexa/";
var REMOTE_CLOUD_HOSTNAME = "alexa.quigleys.us";
var REMOTE_CLOUD_PORT = 8733;
var log = log;

exports.handler = function (event, context) {

    log('Input', event);

    switch (event.header.namespace) {

        case 'System':
            proxyEvent(event, context, 'System');
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

    // change access token
    event.payload.accessToken = "random";

    //log('Event', event);

    var post_data = JSON.stringify(event, 'utf-8');

    // prepare request options
    var post_options = {
        host: REMOTE_CLOUD_HOSTNAME,
        port: REMOTE_CLOUD_PORT,
        path: REMOTE_CLOUD_BASE_PATH + path +'/',
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
            context.succeed(result);
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
    console.log('*************** ' + title + ' End*************');
}

