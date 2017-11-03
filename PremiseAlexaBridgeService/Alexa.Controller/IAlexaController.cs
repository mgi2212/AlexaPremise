using System.Collections.Generic;
using Alexa.SmartHomeAPI.V3;
using SYSWebSockClient;

namespace Alexa.Controller
{
    public interface IAlexaController
    {
        #region Methods

        string[] GetAlexaProperties();

        string GetAssemblyTypeName();

        string[] GetDirectiveNames();

        string GetNameSpace();

        string[] GetPremiseProperties();

        List<AlexaProperty> GetPropertyStates();

        bool HasAlexaProperty(string property);

        bool HasPremiseProperty(string property);

        string MapPremisePropertyToAlexaProperty(string premiseProperty);

        void ProcessControllerDirective();

        void SetEndpoint(IPremiseObject premiseObject);

        bool ValidateDirective();

        #endregion Methods
    }
}