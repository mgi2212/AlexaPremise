using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.ServiceModel;
using SYSWebSockClient;

namespace PremiseAlexaBridgeService
{
    public sealed class PremiseServer
    {
        private static readonly PremiseServer instance = new PremiseServer();

        internal string PremiseServerAddress;
        internal string PremiseUserName;
        internal string PremiseUserPassword;
        internal string PremiseHomeObject;
        internal string AlexaStatusClassPath;
        internal string AlexaApplianceClassPath;
        internal int AlexaDeviceLimit;

        private readonly SYSClient Server;
        internal IPremiseObject RootObject;
        internal IPremiseObject HomeObject;
        internal IPremiseObject AlexaStatus;
        //internal Dictionary<string, Appliance> Appliances;

        private PremiseServer()
        {
            this.PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            this.PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            this.PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            this.PremiseHomeObject = ConfigurationManager.AppSettings["premiseHomeObject"];
            this.AlexaStatusClassPath = ConfigurationManager.AppSettings["premiseAlexaStatusClassPath"];
            this.AlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];
            this.AlexaDeviceLimit = int.Parse(ConfigurationManager.AppSettings["premiseAlexaDeviceLimit"]);
            this.Server = new SYSClient();
            //this.Appliances = new Dictionary<string, Appliance>();
            this.ConnectToServer();
        }

        private void ConnectToServer()
        {
            this.HomeObject = Server.Connect(this.PremiseServerAddress).GetAwaiter().GetResult(); // TODO: , _premiseUser, _premisePassword);

            this.RootObject = this.HomeObject.GetRoot().GetAwaiter().GetResult();

            var returnClause = new string[] { "OID","Name" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();

            whereClause.TypeOf = this.AlexaStatusClassPath;
            //whereClause.TypeOf = "sys://Schema/Modules/Alexa/Classes/AlexaStatus";

            //whereClause.TypeOf = "{31E99AB9-79D2-4BC1-9982-9D615DE6644E}"; // this.AlexaStatusClassPath;

            var statusRecords = this.HomeObject.Select(returnClause, whereClause).GetAwaiter().GetResult();

            foreach (var item in statusRecords)
            {
                var objectId = (string) item.OID;
                this.AlexaStatus = this.RootObject.WrapObjectId(objectId);
                break;
            }
            //TODO: No AlexaStatus Object in Sys is bad
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }

    }
}
