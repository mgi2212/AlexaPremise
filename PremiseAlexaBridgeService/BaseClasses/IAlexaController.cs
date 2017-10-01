using Alexa.SmartHomeAPI.V3;

namespace Alexa.Controller
{
    public interface IAlexaController
    {
        void ProcessControllerDirective();
        AlexaProperty GetPropertyState();
        string GetNameSpace();
        string [] GetDirectiveNames();
        bool HasAlexaProperty(string property);
        bool HasPremiseProperty(string property);
        string GetAssemblyTypeName();
        string GetAlexaProperty();
    }
}
