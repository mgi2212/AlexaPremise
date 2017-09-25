using Alexa.SmartHome.V3;

namespace Alexa.Controller
{
    interface IAlexaController
    {
        void ProcessControllerDirective();
        AlexaProperty GetPropertyState();
    }
}
