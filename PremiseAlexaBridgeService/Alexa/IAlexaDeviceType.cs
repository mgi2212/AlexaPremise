using System;
using System.Collections.Generic;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;

namespace Alexa
{
    internal interface IAlexaDeviceType
    {
        #region Methods

        List<AlexaProperty> FindRelatedProperties(IPremiseObject endpoint, string currentController);

        Dictionary<string, IPremiseSubscription> SubcribeToSupportedProperties(IPremiseObject endpoint, DiscoveryEndpoint discoveryEndpoint, Action<dynamic> callback);

        #endregion Methods
    }
}