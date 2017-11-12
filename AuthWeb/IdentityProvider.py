
def getIdentityProvider(provider, appID, appSecret, roleARN):
	from AmazonIdentityProvider   import AmazonIdentityProvider
	from FacebookIdentityProvider import FacebookIdentityProvider
	from GoogleIdentityProvider   import GoogleIdentityProvider
	
	ip = None
	if provider == 'google':
		ip = GoogleIdentityProvider(appID, appSecret, roleARN)  
	elif provider == 'facebook':
		ip = FacebookIdentityProvider(appID, appSecret, roleARN)
	elif provider == 'amazon':
		ip = AmazonIdentityProvider(appID, appSecret, roleARN)
		
	assert ip != None
	
	return ip

class IdentityProvider:

	APP_ID = None
	APP_SECRET = None
	ROLE_ARN = None

	def __init__(self, appID, appSecret, roleARN):
		self.APP_ID = appID
		self.APP_SECRET = appSecret
		self.ROLE_ARN = roleARN

	def oauthCallback(self,code):
			# exchange authorization code  
	#		print('--- exchanging token for code : ' + code)
	#		token = self.doGetToken(code)
	#		print('--- received token : ' + str(token))
			
			# Call user service
	#		print('--- Getting user Profile for access_token : ' + self.getAccessToken(token))
	#		profile = self.doGetUserProfile(self.getAccessToken(token))
	#		print('--- received profile : ' + str(profile))
			
			# call AWS STS 
	#		print('--- Getting AWS Temp Credentials for token : ' + self.getIDToken(token))
	#		credentials = self.doGetAccessCredentials(self.getIDToken(token), profile)
	#		print('--- received credentials : ' + str(credentials))
			
	#		return credentials, profile
			return ''
	
	def fixToken(self, code) :
		return self.doGetToken2(code)
	
	def getAccessToken(self, token):
		# default for all except FaceBook
		return token['access_token']

	def getIDToken(self,token):    
		# default for all, except Google, Facebook
		return token['access_token']

	def loginURL(self):
		raise NotImplementedError("Please Implement this method in subclasses")

	def doGetToken(self,code,redirect,grant_type):
		raise NotImplementedError("Please Implement this method in subclasses")

	def doGetUserProfile(self,token):
		raise NotImplementedError("Please Implement this method in subclasses")

	def getRoleARN(self):
		raise NotImplementedError("Please Implement this method in subclasses")

	def doGetAccessCredentials(self, token, profile):
		from boto.sts.connection import STSConnection
		
		conn = STSConnection(anon=True, debug=1)
		
		roleARN = self.getRoleARN()
		email   = profile['email'][:32] # Max 32 characters
		
		providerID = ''
		if profile['provider'] == 'Facebook':
			providerID = 'graph.facebook.com'
		elif profile['provider'] == 'Amazon':
			providerID = 'www.amazon.com'
			
		if providerID == '':
			assumedRole = conn.assume_role_with_web_identity(role_arn=roleARN, role_session_name=email, web_identity_token=token)
		else:
			assumedRole = conn.assume_role_with_web_identity(role_arn=roleARN, role_session_name=email, web_identity_token=token, provider_id=providerID)
			
		return assumedRole.credentials.to_dict()

