using Alexa.SmartHomeAPI.V3;

namespace Alexa.Controller
{
    public interface IAlexaController
    {
        void ProcessControllerDirective();
        AlexaProperty GetPropertyState();
    }
}
