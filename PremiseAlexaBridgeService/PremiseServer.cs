using System;
using System.Configuration;
using SYSWebSockClient;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;

namespace PremiseAlexaBridgeService
{
    public sealed class PremiseServer
    {
        private static readonly PremiseServer instance = new PremiseServer();

        internal static int AlexaDeviceLimit;
        internal static bool AlexaCheckStateBeforeSetValue;
        internal static string AlexaStatusClassPath;
        internal static string AlexaApplianceClassPath;
        internal static string AlexaEndpointClassPath;
        internal static string AlexaLocationClassPath;
        internal static string AlexaPowerStateClassPath;
        internal static string AlexaDimmerStateClassPath;
        internal static string PremiseServerAddress;
        internal static string PremiseUserName;
        internal static string PremiseUserPassword;
        internal static string PremiseHomeObject;

        private PremiseServer()
        {
            PremiseServerAddress = ConfigurationManager.AppSettings["premiseServer"];
            PremiseUserName = ConfigurationManager.AppSettings["premiseUser"];
            PremiseUserPassword = ConfigurationManager.AppSettings["premisePassword"];
            PremiseHomeObject = ConfigurationManager.AppSettings["premiseHomeObject"];
            AlexaStatusClassPath = ConfigurationManager.AppSettings["premiseAlexaStatusClassPath"];
            AlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];
            AlexaLocationClassPath = ConfigurationManager.AppSettings["premiseAlexaLocationClassPath"];
            AlexaEndpointClassPath = ConfigurationManager.AppSettings["premiseAlexaEndpointClassPath"];
            AlexaPowerStateClassPath = ConfigurationManager.AppSettings["premisePowerStateClassPath"];
            AlexaDimmerStateClassPath = ConfigurationManager.AppSettings["premiseDimmerClassPath"];
            try
            {
                AlexaDeviceLimit = int.Parse(ConfigurationManager.AppSettings["premiseAlexaDeviceLimit"]);
            }
            catch (Exception)
            {
                AlexaDeviceLimit = 300;
            }

            try
            {
                AlexaCheckStateBeforeSetValue = bool.Parse(ConfigurationManager.AppSettings["AlexaCheckStateBeforeSetValue"]);
            }
            catch (Exception)
            {
                AlexaCheckStateBeforeSetValue = true;
            }

            _sysClient = new SYSClient();
            CheckStatus();
        }

        ~PremiseServer()
        {
            if (_homeObject != null)
            {
                _homeObject = null;
                _rootObject = null;
                _sysClient.Disconnect();
            }
        }

        private static IPremiseObject _homeObject;
        private static IPremiseObject _rootObject;
        private static SYSClient _sysClient;

        public static void CheckStatus()
        {
            try
            {

                if ((_homeObject == null) && (!isClientConnected()))
                {
                    _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                }
                else if ((_homeObject != null) && (!isClientConnected()))
                {
                    _homeObject = null;
                    _rootObject = null;
                    _homeObject = _sysClient.Connect(PremiseServerAddress).GetAwaiter().GetResult();
                    _rootObject = _homeObject.GetRoot().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _homeObject = null;
                _rootObject = null;
                Debug.WriteLine(ex.Message);
                // eat it in retail build
            }
        }

        public static IPremiseObject SysRootObject
        {
            get
            {
                CheckStatus();
                return _rootObject;
            }
        }
        
        public static IPremiseObject SysHomeObject
        {
            get
            {
                CheckStatus();
                return _homeObject;
            }
        }


        public static IPremiseObject ConnectToServer(SYSClient client)
        {
            CheckStatus();
            //return client.Connect(PremiseServer.PremiseServerAddress).GetAwaiter().GetResult(); // TODO: , _premiseUser, _premisePassword);
            return _homeObject;
        }

        public static void DisconnectServer(SYSClient client)
        {
            CheckStatus();
            return;

            //client.Disconnect();
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }

        private static bool isClientConnected()
        {
            return (_sysClient.ConnectionState == System.Net.WebSockets.WebSocketState.Open);
        }
    }
}
