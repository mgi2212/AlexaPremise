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
        internal string AlexaLocationClassPath;

        internal string PremiseServerAddress;
        internal string PremiseUserName;
        internal string PremiseUserPassword;
        internal string PremiseHomeObject;

        //internal IPremiseObject RootObject;
        //internal IPremiseObject HomeObject;

        private PremiseServer()
        {
            this.PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            this.PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            this.PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            this.PremiseHomeObject = ConfigurationManager.AppSettings["premiseHomeObject"];
            this.AlexaStatusClassPath = ConfigurationManager.AppSettings["premiseAlexaStatusClassPath"];
            this.AlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];
            this.AlexaLocationClassPath = ConfigurationManager.AppSettings["premiseAlexaLocationClassPath"];

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
            //this.HomeObject = 

            return client.Connect(this.PremiseServerAddress).GetAwaiter().GetResult(); // TODO: , _premiseUser, _premisePassword);

            //this.RootObject = this.HomeObject.GetRoot().GetAwaiter().GetResult();
        }

        public void DisconnectServer(SYSClient client)
        {
            //this.HomeObject = null;
            //this.RootObject = null;
            client.Disconnect();
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }

    }
}
