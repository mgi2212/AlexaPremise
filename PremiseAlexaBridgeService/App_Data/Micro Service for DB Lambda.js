/*
 * Demonstrates a simple CRUD microservice for a dynamoDb using API Gateway. 
 */

'use strict';
var AWS = require("aws-sdk");
var log = log;

exports.handler = (event, context, callback) => {

    if (event.httpMethod !== 'POST') {
        done(new Error(`Unsupported http method "${event.httpMethod}"`));
        return;
    }

    const done = (err, res) => callback(null, {
        statusCode: err ? '400' : '200',
        body: err ? err.message : JSON.stringify(res),
        headers: {
            'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'
        },
    });

    var dynamo = new AWS.DynamoDB();
    var command = JSON.parse(event.body);

    switch (command.function) {
        case 'updateItem': 
            var updateParams = {
                TableName: 'PremiseBridgeCustomer',
                Key: {
                    "id": { "S": command.id }
                },
                UpdateExpression: "SET host=:h, port=:p, app_path=:a, access_token=:t",
                "ExpressionAttributeValues": {
                    ":h": { "S": command.host },
                    ":p": { "S": command.port },
                    ":a": { "S": command.app_path },
                    ":t": { "S": command.access_token }
                },
                ReturnValues: "UPDATED_NEW"
            };
            dynamo.updateItem(updateParams, done);
            break;
                
        case 'deleteItem': 
            var deleteParams = {
                TableName: 'PremiseBridgeCustomer',
                Key: {
                    "id": { "S": command.id }
                }
            };
            dynamo.deleteItem(deleteParams, done);
            break;
                
        case 'getItem': 
            var getParams = {
                TableName: 'PremiseBridgeCustomer',
                Key: {
                    "id": { "S": command.id }
                }
            };
            dynamo.getItem(getParams, done);
            break;
                
        case 'putItem': 
            var putParams = {
                TableName: 'PremiseBridgeCustomer',
                Item: {
                    "id": { "S": command.id },
                    "host": { "S": command.host },
                    "port": { "S": command.port },
                    "app_path": { "S": command.app_path },
                    "access_token": { "S": command.access_token }
                }
            };
            dynamo.putItem(putParams, done);
            break;

        default: 
            done(new Error(`Unsupported db function "${event.body.Function}"`));
            break;
    }
};

function log(title, msg) {
    console.log(':' + title + ':');
    console.log(msg);
}
