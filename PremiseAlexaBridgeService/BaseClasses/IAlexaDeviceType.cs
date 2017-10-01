using Alexa.SmartHomeAPI.V3;
using System;
using System.Collections.Generic;
using SYSWebSockClient;
using Alexa.Controller;

namespace Alexa
{
    interface IAlexaDeviceType
    {
        List<AlexaProperty> FindRelatedProperties(IPremiseObject endpoint, string currentController);
        Dictionary<string, IPremiseSubscription> SubcribeToSupportedProperties(IPremiseObject endpoint, DiscoveryEndpoint discoveryEndpoint, Action<dynamic> callback);
    }
}
