/* eslint-disable  func-names */
/* eslint quote-props: ["error", "consistent"]*/
/*
 * This sample demonstrates a custom Smart Home related skill built with the Amazon Alexa Skills nodejs skill development kit.
 */

'use strict';

const Alexa = require("alexa-sdk");
const AWS = require("aws-sdk");
const util = require('util');
const APP_ID = "amzn1.ask.skill.e926b099-c686-44c6-a36d-45b167f50277";

const languageStrings = {
    'en-GB': {
        translation: {
            SKILL_NAME: 'Premise Custom Skill',
            CUSTOMER_HOUSE: 'In %s\'s house, ',
            WELCOME_MESSAGE: 'Welcome to Premise. Try asking Premise for the status of a specific room. To control devices simply ask Alexa, to turn on the specific device.',
            WELCOME_REPROMPT: 'Try asking Premise for the status of a specific room.',
            HELP_MESSAGE: 'You can ask me if a specific room is occupied or what it\'s status is, you can say exit... What can I help you with?',
            HELP_REPROMPT: 'What can I help you with?',
            STOP_MESSAGE: 'Goodbye!',
            ROOM_NOT_FOUND: 'that room does not exist or cannot be queried.',
            ROOM_OCCUPIED: 'the %s is occupied and is currently using the %s scene. There are %d devices that I can control in this space. This room has been occupied %d times since midnight',
            ROOM_UNOCCUPIED: 'the %s is unoccupied. There are %d devices that I can control in this space. This room has been occupied %d times since midnight',
            ROOM_TEMPERATURE: '. The current temperature is %d degrees',
            NO_ROOM_LIGHTS: ' and there are no lights on.',
            ONE_ROOM_LIGHT: ' and there is one light on.',
            ROOM_LIGHTS: ' and there are %d lights on.'
        },
    },
    'en-US': {
        translation: {
            SKILL_NAME: 'Premise Custom Skill',
            CUSTOMER_HOUSE: 'In %s\'s house, ',
            WELCOME_MESSAGE: 'Welcome to Premise. Try asking Premise for the status of a specific room. To control devices simply ask Alexa, to turn on the specific device.',
            WELCOME_REPROMPT: 'Try asking Premise for the status of a specific room.',
            HELP_MESSAGE: 'You can ask me if a specific room is occupied or what it\'s status is, you can say exit... What can I help you with?',
            HELP_REPROMPT: 'What can I help you with?',
            STOP_MESSAGE: 'Goodbye!',
            ROOM_NOT_FOUND: 'that room does not exist or cannot be queried.',
            ROOM_OCCUPIED: 'the %s is occupied and is currently using the %s scene. There are %d devices that I can control in this space. This room has been occupied %d times since midnight',
            ROOM_UNOCCUPIED: 'the %s is unoccupied. There are %d devices that I can control in this space. This room has been occupied %d times since midnight',
            ROOM_TEMPERATURE: '. The current temperature is %d degrees',
            NO_ROOM_LIGHTS: ' and there are no lights on.',
            ONE_ROOM_LIGHT: ' and there is one light on.',
            ROOM_LIGHTS: ' and there are %d lights on.'
        },
    },
    'de-DE': {
        translation: {
            SKILL_NAME: 'Premise auf Deutsch',
            CUSTOMER_HOUSE: 'Im Haus von %s, ',
            WELCOME_MESSAGE: 'Willkommen bei Premise. Versuchen Sie, Premise für den Status eines bestimmten Raumes zu fragen. Um die Geräte zu kontrollieren, fragen Sie einfach Alexa, um das spezielle Gerät einzuschalten.',
            WELCOME_REPROMPT: 'Versuche, Premise für den Status eines bestimmten Raumes zu fragen.',
            HELP_MESSAGE: 'Du kannst sagen, „ob ein bestimmtes Zimmer belegt ist oder in welchem Modus es ist?“, oder du kannst „Beenden“ sagen... Wie kann ich dir helfen?',
            HELP_REPROMPT: 'Wie kann ich dir helfen?',
            STOP_MESSAGE: 'Auf Wiedersehen!',
            ROOM_NOT_FOUND: 'dieser Raum existiert nicht oder kann nicht abgefragt werden.',
            ROOM_OCCUPIED: 'die %s ist besetzt un benutzt derziet die %s szene. Est gibt %d Geräte, die ich in diesem Raum kontrollieren kann. Dieses Zimmer ist seit Mitternacht %d Mal besetz',
            ROOM_UNOCCUPIED: 'die %s ist unbesetzt. Es gibt %d Geräte, die ich in diesem Raum kontrollieren kann. Dieser Raum ist seit Mitternacht %d Mal besetzt',
            ROOM_TEMPERATURE: '. Die aktuelle Temperatur beträgt %d Grad',
            NO_ROOM_LIGHTS: ' und es gibtg kein Licht auf.',
            ONE_ROOM_LIGHT: ' und da ist ein Licht auf.',
            ROOM_LIGHTS: ' und da sind %d Lichter an.'
        },
    },
};

// Use this.t() to get corresponding language data
const handlers = {
    'IsRoom': function () {
        getCustomerProfile(this);
    },
    'LaunchRequest': function () {
        const speechOutput = this.t('WELCOME_MESSAGE');
        const reprompt = this.t('WELCOME_REPROMPT');
        this.emit(':ask', speechOutput, reprompt);
    },
    'SessionEndedRequest': function () {
        const speechOutput = this.t('STOP_MESSAGE');
        this.emit(':tell', speechOutput);
    },
    'AMAZON.HelpIntent': function () {
        const speechOutput = this.t('HELP_MESSAGE');
        const reprompt = this.t('HELP_REPROMPT');
        this.emit(':ask', speechOutput, reprompt);
    },
    'AMAZON.CancelIntent': function () {
        const speechOutput = this.t('STOP_MESSAGE');
        this.emit(':tell', speechOutput);
    },
    'AMAZON.StopIntent': function () {
        const speechOutput = this.t('STOP_MESSAGE');
        this.emit(':tell', speechOutput);
    },
    'Unhandled': function () {
        const speechOutput = this.t('HELP_MESSAGE');
        const reprompt = this.t('HELP_REPROMPT');
        this.emit(':ask', speechOutput, reprompt);
    }
};

const querySpaceModeRequest = {
    "header": {
        "messageId": "",
        "namespace": "Alexa.ConnectedHome.Query",
        "name": "GetSpaceModeRequest",
        "payloadVersion": "2"
    },
    "payload": {
        "accessToken": "",
        "space": {
            "name": ""
        }
    }
};

var log = log;
var https = require('https');

// get customer profile (name, email and cust_id)
function getCustomerProfile(instance) {

    // prepare request options
    var get_options = {
        host: 'api.amazon.com',
        port: 443,
        path: '/user/profile',
        method: 'GET',
        headers: {
            'x-amz-access-token': instance.event.session.user.accessToken,
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
            getCustomerEndpoint(instance, customer_info);
        });

        response.on('error', function (e) {
            console.log('Err', e.message);
            instance.emit(':tell', 'Request to third party application failed authorization.');
        });
    });

    var get_data = "";
    get_req.write(get_data);
    get_req.end();
}

// query dynamoDb for customer endpoint URI elements
function getCustomerEndpoint(instance, customer_info) {

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
            log('getCustomerEndpointResponse', 'Record not found for ' + customer_info.email);
            instance.emit('Failed to find customer endpoint.');
        } else {
            queryEndpoint(instance, customer_info, data.Item);
        }

    });
}

// send query to remote premise system 
function queryEndpoint(instance, customer_info, customer_endpoint) {

    var query = querySpaceModeRequest;
    query.payload.accessToken = customer_endpoint.access_token.S;

    var room_prefix = instance.event.request.intent.slots.Prefix.value;
    var room = instance.event.request.intent.slots.Room.value;

    // NLU bug?
    if (room === "is") {
        room = room_prefix;
        room_prefix = undefined;
    }

    if (room_prefix === undefined) {
        query.payload.space.name = room;
    } else {
        query.payload.space.name = room_prefix + room;
    }
    var post_data = JSON.stringify(query, 'utf-8');

    // prepare request options
    var post_options = {
        host: customer_endpoint.host.S,                 // REMOTE_CLOUD_HOSTNAME,
        port: customer_endpoint.port.S,                 // REMOTE_CLOUD_PORT,
        path: customer_endpoint.app_path.S + 'Query/',  // REMOTE_CLOUD_BASE_PATH + command + '/',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Content-Length': post_data.length
        }
    };

    // make the request
    var result = "";
    var post_req = https.request(post_options, function (response) {

        response.setEncoding('utf-8');

        response.on('data', function (chunk) {
            result += chunk;
        });

        response.on('end', function () {
            log('queryResponse', JSON.stringify(JSON.parse(result)));
            emitResult(instance, customer_info, JSON.parse(result));
        });

        response.on('error', function (e) {
            console.log('Error in query for the Premise endpoint.', e.message);
            instance.emit('Unable to query the Premise endpoint.');
        });
    });

    post_req.write(post_data);
    post_req.end();
}

// build response 
function emitResult(instance, customer_info, result) {

    const hFormat = instance.t('CUSTOMER_HOUSE');
    var speechOutput = util.format(hFormat, customer_info.name);

    if (result.header.name !== "GetSpaceModeResponse") {
        speechOutput += instance.t('ROOM_NOT_FOUND');
    }
    else {

        // Build room, scene, device count and occupancy report.
        var status = result.payload.roomStatus;

        if (status.occupied === "True") {
            const format = instance.t('ROOM_OCCUPIED');
            speechOutput += util.format(format, status.friendlyName, status.currentScene, status.deviceCount, status.occupancyCount);
        } else {
            const format = instance.t('ROOM_UNOCCUPIED');
            speechOutput += util.format(format, status.friendlyName, status.deviceCount, status.occupancyCount);
        }

        // add temperature if available
        if (status.currentTemperature !== undefined) {
            const format = instance.t('ROOM_TEMPERATURE');
            speechOutput += util.format(format, status.currentTemperature);
        }

        // report number of lights on 
        if (status.lightsOnCount !== undefined) {
            if (lightsOn === "0") {
                speechOutput += instance.t('NO_ROOM_LIGHTS');
            } else if (lightsOn === "1") {
                speechOutput += instance.t('ONE_ROOM_LIGHT');
            } else {
                const format = instance.t('ROOM_LIGHTS');
                speechOutput += util.format(format, status.lightsOnCount);
            }
        } else {
            speechOutput += ".";
        }

    }
    log("output", speechOutput);
    instance.emit(':tellWithCard', speechOutput, instance.t('SKILL_NAME'), speechOutput);
}

// event handler: register message handlers and lanugage strings
exports.handler = (event, context) => {
    var alexa = Alexa.handler(event, context);
    alexa.appId = APP_ID;
    log("event", event);
    // To enable string internationalization (i18n) features, set a resources object.
    alexa.resources = languageStrings;
    alexa.registerHandlers(handlers);
    alexa.execute();
};

function log(title, msg) {
    console.log(':' + title + ':');
    console.log(msg);
}

