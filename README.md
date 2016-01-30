# AlexaPremise

Alexa Premise Bridge

 If using PreWarmCache option changes to: 
 c:\windows\system32\inetsrv\config\applicationHost.config 

    <system.applicationHost>

        <applicationPools>
			...
            <add name="AlexaBridge" autoStart="true" enable32BitAppOnWin64="true" managedRuntimeVersion="v4.0" managedPipelineMode="Integrated" startMode="AlwaysRunning">
                <processModel identityType="ApplicationPoolIdentity" />
            </add>
			...
        </applicationPools>

        <sites>
			...

            <site name="AlexaBridge" id="2" serverAutoStart="true">
                <application path="/" applicationPool="AlexaBridge" preloadEnabled="true" serviceAutoStartEnabled="true" serviceAutoStartProvider="PreWarmMyCache">
                    <virtualDirectory path="/" physicalPath="C:\inetpub\wwwroot\AlexaBridge" />
                </application>
                <bindings>
                    <binding protocol="http" bindingInformation="*:80:mysite.domain.com" />
                    <binding protocol="https" bindingInformation="192.168.1.10:443:mysite.domain.com" sslFlags="0" />
                </bindings>
                <logFile logExtFileFlags="Date, Time, ClientIP, UserName, SiteName, ComputerName, ServerIP, Method, UriStem, UriQuery, HttpStatus, Win32Status, TimeTaken, ServerPort, UserAgent, Referer, Host, HttpSubStatus" logTargetW3C="File, ETW" />
                <traceFailedRequestsLogging enabled="true" />
            </site>

			...

        </sites>

		<serviceAutoStartProviders>
     			<add name="PreWarmMyCache" type="PremiseAlexaBridgeService.PreWarmCache, PremiseAlexaBridgeService" />
		</serviceAutoStartProviders> 


    </system.applicationHost>
