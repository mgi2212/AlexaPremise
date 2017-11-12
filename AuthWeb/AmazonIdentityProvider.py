import json
from http.client import HTTPSConnection
from wsgiref import headers
from urllib import request
import urllib
import codecs
from IdentityProvider import IdentityProvider 
from flask import request, Response
import requests


class AmazonIdentityProvider(IdentityProvider):  

	def loginURL(self):
		loginURL = 'https://www.amazon.com/ap/oa?'
		loginURL = loginURL + 'client_id=' + self.APP_ID + '&'
		loginURL = loginURL + 'scope=profile&'
		loginURL = loginURL + 'redirect_uri=https%3A%2F%2F' + request.headers['Host'] + '%2Foauth2callback%2Famazon&'
		loginURL = loginURL + 'response_type=code'
		return loginURL
		
	def accountlinkURL(self):
		loginURL = 'https://www.amazon.com/ap/oa?'
		loginURL = loginURL + 'client_id=' + self.APP_ID + '&'
		loginURL = loginURL + 'scope=profile&'
		loginURL = loginURL + 'redirect_uri=https%3A%2F%2F' + request.headers['Host'] + '%2Foauth2callback%2Famazon&'
		loginURL = loginURL + 'response_type=code'
		return loginURL
	
	def doGetToken2(self, code):
		base_redirect_querystring = '&redirect_uri='
		redirect = base_redirect_querystring + 'https%3A%2F%2F' + request.headers['Host'] + '%2Foauth2accountlinkcallback'
		
		base_grant_type_querystring = 'grant_type='
		grant_type = base_grant_type_querystring + 'authorization_code'
		
		return self.doGetToken(code, redirect,grant_type)
	
	def doGetToken(self, code, redirect, grant_type):
		base_redirect_querystring = '&redirect_uri='
		default_redirect = base_redirect_querystring + 'https%3A%2F%2F' + request.headers['Host'] + '%2Foauth2callback%2Famazon'
		
		base_grant_type_querystring = 'grant_type='
		default_grant_type = base_grant_type_querystring + 'authorization_code'
		
		try:
			if redirect is None:
				print('--- doGetToken : redirect value is empty. Swapping with default')
				redirect = default_redirect
			else:
				print ('--- doGetToken : redirect value : ' + redirect)
				redirect = base_redirect_querystring + redirect
		except :
				print (err)
				print ('--- doGetToken : redirect value is undefined. Swapping with default')
				redirect = default_redirect
		
		try:
			if grant_type is None:
				print('--- doGetToken : grant_type value is empty. Swapping with default')
				grant_type = default_grant_type
			else:
				print ('--- doGetToken : grant_type value : ' + grant_type)
				grant_type = base_grant_type_querystring + grant_type
		except :
				print (err)
				print ('--- doGetToken : grant_type value is undefined. Swapping with default')
				grant_type = default_grant_type
		
		host = 'api.amazon.com'
		path = '/auth/o2/token'
		headers = {'Content-type': 'application/x-www-form-urlencoded'}

		data = grant_type
		data = data + '&code=' + code 
		data = data + redirect
		data = data + '&client_id=' + self.APP_ID
		data = data + '&client_secret=' + self.APP_SECRET
	
		conn = httplib.HTTPSConnection(host)
		conn.request('POST', path, data, headers)
		resp = conn.getresponse()
		reader = codecs.getreader("utf-8")
		return json.load(reader(resp))
	
	def do2ProxyToken(self,code):
		return ''

	def doGetUserProfile(self, token):
		
		url = 'https://api.amazon.com/user/profile?'
		url = url + '&access_token=' + token

		response = urlopen(url)
		reader = codecs.getreader("utf-8")
		amazonProfile = json.load(reader(response))
	
		return { 'name' : amazonProfile['name'], 'firstname' : amazonProfile['name'].split()[0],
				 'email' : amazonProfile['email'], 'picture' : '/static/img/amazon-logo-50.png',
				 'provider' : 'Amazon'}
	
	def getRoleARN(self):
		return self.ROLE_ARN

	def getProxyTokenRedirect() :
		return 'https%3A%2F%2F' + request.headers['Host'] + '%2Foauth2tokencallback'

	def doGetTokenOriginal(self,code):
	
		host = 'api.amazon.com'
		path = '/auth/o2/token'
		headers = {'Content-type': 'application/x-www-form-urlencoded'}

		data = 'grant_type=authorization_code'
		data = data + '&code=' + code 
		data = data + urllib.parse.urlencode('&redirect_uri=https://' + request.headers['Host'] + '/oauth2callback/amazon')
		data = data + '&client_id=' + self.APP_ID
		data = data + '&client_secret=' + self.APP_SECRET

		conn = httplib.HTTPSConnection(host)
		conn.request('POST', path, data, headers)
		resp = conn.getresponse()
		
		#new code
		reader = codecs.getreader("utf-8")
		reader_token = reader(resp)
		token = json.load(reader_token)
		return token

	def postTokenData(self,data):
	
		host = 'pitangui.amazon.com'
		path = '/api/skill/link/M2T72YRX0CB92Y'
		headers = {'Content-type': 'application/json;charset UTF-8', 'Cache-Control': 'no-store', 'Pragma': 'no-cache'}
		
		conn = httplib.HTTPSConnection(host)
		conn.request('POST', path, data, headers)
		resp = conn.getresponse()
		reader = codecs.getreader("utf-8")
		return json.load(reader(resp))