/* eslint-disable  func-names */
/* eslint quote-props: ["error", "consistent"]*/
/**
 * This sample demonstrates a simple skill built with the Amazon Alexa Skills 
 * nodejs skill development kit.
 * This sample supports multiple lauguages. (en-US, en-GB, de-DE).
 * The Intent Schema, Custom Slots and Sample Utterances for this skill, as well
 * as testing instructions are located at https://github.com/alexa/skill-sample-nodejs-fact
 **/

'use strict';

const Alexa = require('alexa-sdk');
const AWS = require("aws-sdk");
const APP_ID = "amzn1.ask.skill.cee579ed-7745-4814-8e8f-976090d40dcb";  // TODO replace with your app ID (OPTIONAL).

const languageStrings = {
    'en-GB': {
        translation: {
            SKILL_NAME: 'Premise Custom Skill',
            OCCUPIED_MESSAGE:'is occupied',
            HELP_MESSAGE: 'You can ask me if a specific room is occupied or what it\'s status is, you can say exit... What can I help you with?',
        HELP_REPROMPT: 'What can I help you with?',
    STOP_MESSAGE: 'Goodbye!',
    },
    },
'en-US': {
    translation: {
            SKILL_NAME: 'Premise Custom Skill',
            OCCUPIED_MESSAGE:'is occupied',
            HELP_MESSAGE: 'You can ask me if a specific room is occupied or what it\'s status is, you can say exit... What can I help you with?',
            HELP_REPROMPT: 'What can I help you with?',
                        STOP_MESSAGE: 'Goodbye!',
                        },
    },
    'de-DE': {
        translation: {
                SKILL_NAME: 'Premise auf Deutsch',
                OCCUPIED_MESSAGE:'is occupied',
                HELP_MESSAGE: 'Du kannst sagen, „ob ein bestimmtes Zimmer belegt ist oder in welchem Modus es ist?“, oder du kannst „Beenden“ sagen... Wie kann ich dir helfen?',
                HELP_REPROMPT: 'Wie kann ich dir helfen?',
                STOP_MESSAGE: 'Auf Wiedersehen!',
                },
    },
};

var log = log;
var https = require('https');

const querySpaceModeRequest = {
    "header": {
        "messageId": "",
        "namespace": "Alexa.ConnectedHome.Query",
        "name": "GetSpaceMode",
        "payloadVersion": "2"
    },
    "payload": {
        "accessToken": "",
        "space": {
            "name": ""
        }
    }
};


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
            //log('getCustomerProfileResponse', JSON.stringify(customer_info));
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

function getCustomerEndpoint(instance, customer_info) {

    var dynamodb = new AWS.DynamoDB();

    var params = {
        TableName : 'PremiseBridgeCustomer',
        Key : {
            "id" : {
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
            log('getCustomerEndpointResponse', 'Record not found for ' + customer_info.email);
            instance.emit('Failed to find customer endpoint.');
        } else {
            //log('getCustomerEndpointResponse', 'Record found for ' + customer_info.email);
            //log('customer_info', data.Item);
            queryEndpoint(instance, customer_info, data.Item);
        }
        
    });    
}

function queryEndpoint(instance, customer_info, customer_endpoint) {

    var query = querySpaceModeRequest;
    query.payload.accessToken = customer_endpoint.access_token.S;
    
    var room_prefix = instance.event.request.intent.slots.Prefix.value;
    var room = instance.event.request.intent.slots.Room.value; 
    //log("room", room);
    //log("room_prefix", room_prefix);

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
    log('queryData', JSON.stringify(JSON.parse(post_data)));

    // prepare request options
    var post_options = {
        host: customer_endpoint.host.S,                 // REMOTE_CLOUD_HOSTNAME,
        port: customer_endpoint.port.S,                 // REMOTE_CLOUD_PORT,
        path: customer_endpoint.app_path.S + 'Query/',  //path: REMOTE_CLOUD_BASE_PATH + command + '/',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Content-Length': post_data.length
        }
    };
    //log('postOptions', post_options);

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
            console.log('Err', e.message);
            context.fail('Request to appliance cloud failed.');
        });
    });

    post_req.write(post_data);
    post_req.end();
}

function emitResult(instance, customer_info, result) {
    //console.log(JSON.stringify(instance.event));

    var speechOutput = "In " + customer_info.name + "'s house, ";

    //log("roomStatus", result.payload.roomStatus);
    if (result.header.name === "NoSuchTargetError") {
        speechOutput += "that room does not exist, or cannot be queried.";
    }
    else {
        var room = result.payload.roomStatus.friendlyName;
        //log("room_", room);
        var mode = instance.event.request.intent.slots.Mode.value; 
        var occupied = result.payload.roomStatus.occupied;
        var temperature = result.payload.roomStatus.currentTemperature;
        var lightsOn = result.payload.roomStatus.lightsOnCount;
        //log("temperature", temperature);
        if (occupied === "True") {
            speechOutput += "the " + room + " is occupied and is currently using the " + result.payload.roomStatus.mode + " scene. There are " + result.payload.roomStatus.deviceCount + " devices that I can control in this space. This room has been occupied " + result.payload.roomStatus.occupancyCount + " times since midnight ";
        } else {
            speechOutput += "the " + room + " is not occupied. There are " + result.payload.roomStatus.deviceCount + "  devices that I can control in this space. This room has been occupied " + result.payload.roomStatus.occupancyCount + " times since midnight ";
        }
        if (temperature !== undefined) {
            speechOutput += ". The current temperature is " + temperature + " degrees";
        }
        if (lightsOn !== undefined) {
            speechOutput += " and there are ";
            if (lightsOn === "0") {
                speechOutput += "no lights on.";
            } else {
                speechOutput += lightsOn + " lights on.";
            }
        } else {
            speechOutput += ".";
        }

    }
    log("output", speechOutput);
    instance.emit(':tellWithCard', speechOutput, instance.t('SKILL_NAME'), speechOutput);
}


const handlers = {
    'LaunchRequest': function () {
        this.emit('Hello');
    },
    'IsRoom': function () {
        // Use this.t() to get corresponding language data
        //console.log(JSON.stringify(this.event));
        getCustomerProfile(this);

    },
    'AMAZON.HelpIntent': function () {
        const speechOutput = this.t('HELP_MESSAGE');
        const reprompt = this.t('HELP_MESSAGE');
        this.emit(':ask', speechOutput, reprompt);
    },
    'AMAZON.CancelIntent': function () {
        this.emit(':tell', this.t('STOP_MESSAGE'));
    },
    'AMAZON.StopIntent': function () {
        this.emit(':tell', this.t('STOP_MESSAGE'));
    },
    'SessionEndedRequest': function () {
        this.emit(':tell', this.t('STOP_MESSAGE'));
    },
};


exports.handler = (event, context) => {
    var alexa = Alexa.handler(event, context);
    alexa.APP_ID = APP_ID;
    //log ("event", event);
    // To enable string internationalization (i18n) features, set a resources object.
    alexa.resources = languageStrings;
    alexa.registerHandlers(handlers);
    alexa.execute();
};

function log(title, msg) {
    console.log('************** ' + title + ' *************');
    console.log(msg);
    console.log('************ ' + title + ' End ***********');
}

