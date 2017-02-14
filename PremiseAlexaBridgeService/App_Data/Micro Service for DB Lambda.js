'use strict';

console.log('Loading function');
var AWS = require("aws-sdk");
var log = log;

/**
 * Demonstrates a simple HTTP endpoint using API Gateway. 
 */
exports.handler = (event, context, callback) => {
    //console.log('Received event:', JSON.stringify(event, null, 2));

    var dynamo = new AWS.DynamoDB();

    const done = (err, res) => callback(null, {
        statusCode: err ? '400' : '200',
        body: err ? err.message : JSON.stringify(res),
        headers: {
            'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'
        },
    });

    switch (event.httpMethod) {
        case 'POST':
            var command = JSON.parse(event.body);
            //console.log(JSON.stringify(command.dbBody));

            if (command.function === "updateItem") {

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
            }
            else if (command.function === "deleteItem") {

                var deleteParams = {
                    TableName: 'PremiseBridgeCustomer',
                    Key: {
                        "id": { "S": command.id }
                    }
                };

                dynamo.deleteItem(deleteParams, done);
            }
            else if (command.function === "getItem") {

                var getParams = {
                    TableName: 'PremiseBridgeCustomer',
                    Key: {
                        "id": { "S": command.id }
                    }
                };

                dynamo.getItem(getParams, done);
            }
            else if (command.function === "putItem") {

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
            }
            else {
                done(new Error(`Unsupported db function "${event.body.Function}"`));
            }
            break;
        default:
            done(new Error(`Unsupported http method "${event.httpMethod}"`));
    }
};
