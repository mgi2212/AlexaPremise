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

        internal int AlexaDeviceLimit;
        internal bool AlexaCheckStateBeforeSetValue;
        internal string AlexaStatusClassPath;
        internal string AlexaApplianceClassPath;

        internal string PremiseServerAddress;
        internal string PremiseUserName;
        internal string PremiseUserPassword;
        internal string PremiseHomeObject;

        //private readonly SYSClient Server;
        //private SYSClient Server;
        internal IPremiseObject RootObject;
        internal IPremiseObject HomeObject;
        internal IPremiseObject AlexaStatus;

        private PremiseServer()
        {
            this.PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            this.PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            this.PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            this.PremiseHomeObject = ConfigurationManager.AppSettings["premiseHomeObject"];
            this.AlexaStatusClassPath = ConfigurationManager.AppSettings["premiseAlexaStatusClassPath"];
            this.AlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];

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

            //this.Server = new SYSClient();
            //this.ConnectToServer();
        }

        public void ConnectToServer(SYSClient client)
        {
            this.HomeObject = client.Connect(this.PremiseServerAddress).GetAwaiter().GetResult(); // TODO: , _premiseUser, _premisePassword);

            this.RootObject = this.HomeObject.GetRoot().GetAwaiter().GetResult();

            var returnClause = new string[] { "OID","Name" };
            dynamic whereClause = new System.Dynamic.ExpandoObject();
            whereClause.TypeOf = this.AlexaStatusClassPath;

            var statusRecords = this.HomeObject.Select(returnClause, whereClause).GetAwaiter().GetResult();

            foreach (var item in statusRecords)
            {
                var objectId = (string) item.OID;
                this.AlexaStatus = this.RootObject.WrapObjectId(objectId);
                break;
            }
            //TODO: No AlexaStatus Object in Sys is bad
        }

        public void DisconnectServer(SYSClient client)
        {
            this.HomeObject = null;
            this.RootObject = null;
            this.AlexaStatus = null;
            client.Disconnect();
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }

    }
}
