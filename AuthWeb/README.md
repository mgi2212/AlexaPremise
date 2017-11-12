# Custom Oauth Authorization Web Application
Sample python-backed web application that demonstrats the use of an _Auth Grant Type_ custom Authorization URL for linking your customers to your Alexa Smart Home skill. It also provides for the identification of a unique per-customer endpoint for use by a device cloud. This is usefull for managing load based on region, and beta test groups. 

## Features:
1. Authenticates with Login with Amazon (LWA) and alternatively Google or Facebook
2. Requests customer profile information and stores a unique customer identifier in a DynamoDB table along with endpoint data that can be manually entered or modified.
3. No Personally Identifiable Information PII is retained.
4. Designed for use behind a load balancer for scalability.
5. Provisions for role-based identity management.
6. Designed to be deployed as an Elastic Beanstalk application.

## Requirements:
- Python v3.x.x
- Flask v0.10.1 https://pypi.python.org/pypi/Flask/0.12.2
- Jinja2 v2.7 https://pypi.python.org/pypi/Jinja2/2.10
- MarkupSafe v0.18 https://pypi.python.org/pypi/MarkupSafe 
- Werkzeug v0.9.1 https://pypi.python.org/pypi/Werkzeug/0.12.2
- boto v2.9.6 https://pypi.python.org/pypi/boto/2.48.0 
- itsdangerous v0.21 https://pypi.python.org/pypi/itsdangerous/0.24

## Setup and Configuration
See [frontend-setup-authwebsite.md](../frontend-setup-authwebsite.md) in the instructions folder.
