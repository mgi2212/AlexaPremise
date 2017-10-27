using System.Collections.Generic;
using Alexa.SmartHomeAPI.V3;

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

        AlexaProperty GetPropertyState();

        List<AlexaProperty> GetPropertyStates();

        bool HasAlexaProperty(string property);

        bool HasPremiseProperty(string property);

        void ProcessControllerDirective();

        #endregion Methods
    }
}