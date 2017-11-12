"use strict";

console.log("Loading customer table lambda function");

var aws = require("aws-sdk");

/**
 * customer_table_name is stored as an environment variable
 */
var customerTable = process.env.customer_table_name;

/**
 * Demonstrates a simple HTTP endpoint that provides access to Create, Read, Update, and Delete (CRUD) funtions
 * for a dynamoDB table through an AWS API Gateway.
 *
 * @param {any} event event json
 * @param {any} context Node.js context object
 * @param {any} callback callback
 *
 */
exports.handler = (event, context, callback) => {
    var dynamo = new aws.DynamoDB();

    const done = (err, res) => callback(null,
        {
            statusCode: err ? "400" : "200",
            body: err ? err.message : JSON.stringify(res),
            headers: {
                'Content-Type': "application/json",
                'Access-Control-Allow-Origin': "*"
            }
        });

    if (event.httpMethod !== "POST") {
        done(new Error(`Unsupported http method "${event.httpMethod}"`));
    }

    const command = JSON.parse(event.body);

    switch (command.function) {
        case "updateItem":
            {
                const updateParams = {
                    TableName: customerTable,
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
            break;

        case "deleteItem":
            {
                const deleteParams = {
                    TableName: customerTable,
                    Key: {
                        "id": { "S": command.id }
                    }
                };

                dynamo.deleteItem(deleteParams, done);
            }
            break;

        case "getItem":
            {
                const getParams = {
                    TableName: customerTable,
                    Key: {
                        "id": { "S": command.id }
                    }
                };

                dynamo.getItem(getParams, done);
            }
            break;

        case "putItem":
            {
                const putParams = {
                    TableName: customerTable,
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
            break;

        default:
            {
                done(new Error(`Unsupported db function "${event.body.Function}"`));
            }
            break;
    }
};