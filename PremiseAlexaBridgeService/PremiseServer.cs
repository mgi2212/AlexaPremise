using System;
using System.Configuration;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    public sealed class PremiseServer
    {
        private static readonly PremiseServer instance = new PremiseServer();

        internal int AlexaDeviceLimit;
        internal bool AlexaCheckStateBeforeSetValue;
        internal string AlexaStatusClassPath;
        internal string AlexaApplianceClassPath;
        internal string AlexaEndpointClassPath;
        internal string AlexaLocationClassPath;
        internal string AlexaPowerStateClassPath;
        internal string AlexaDimmerStateClassPath;


        internal string PremiseServerAddress;
        internal string PremiseUserName;
        internal string PremiseUserPassword;
        internal string PremiseHomeObject;

        private PremiseServer()
        {
            this.PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            this.PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            this.PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            this.PremiseHomeObject = ConfigurationManager.AppSettings["premiseHomeObject"];
            this.AlexaStatusClassPath = ConfigurationManager.AppSettings["premiseAlexaStatusClassPath"];
            this.AlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];
            this.AlexaLocationClassPath = ConfigurationManager.AppSettings["premiseAlexaLocationClassPath"];
            this.AlexaEndpointClassPath = ConfigurationManager.AppSettings["premiseAlexaEndpointClassPath"];
            this.AlexaPowerStateClassPath = ConfigurationManager.AppSettings["premisePowerStateClassPath"];
            this.AlexaDimmerStateClassPath = ConfigurationManager.AppSettings["premiseDimmerClassPath"];
            try
            {
                this.AlexaDeviceLimit = int.Parse(ConfigurationManager.AppSettings["premiseAlexaDeviceLimit"]);
            }
            catch (Exception)
            {
                this.AlexaDeviceLimit = 300;
            }

            try
            {
                this.AlexaCheckStateBeforeSetValue = bool.Parse(ConfigurationManager.AppSettings["AlexaCheckStateBeforeSetValue"]);
            }
            catch (Exception)
            {
                this.AlexaCheckStateBeforeSetValue = true;
            }
        }

        public IPremiseObject ConnectToServer(SYSClient client)
        {
            return client.Connect(this.PremiseServerAddress).GetAwaiter().GetResult(); // TODO: , _premiseUser, _premisePassword);
        }

        public void DisconnectServer(SYSClient client)
        {
            client.Disconnect();
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }
    }
}
