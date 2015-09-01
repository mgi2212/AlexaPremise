using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.ServiceModel;
using Premise;

namespace PremiseAlexaBridgeService
{

    public sealed class PremiseServer
    {
        private static readonly PremiseServer instance = new PremiseServer();

        static PremiseServer()
        {

        }

        private PremiseServer()
        {
            _premiseServer = ConfigurationManager.AppSettings["premiseServer"];
            _premiseUser = ConfigurationManager.AppSettings["premiseUser"];
            _premisePassword = ConfigurationManager.AppSettings["premisePassword"];
            _premiseHomeObject = ConfigurationManager.AppSettings["premiseHomeObject"];


            _server = new SYSMiniBrokerClass();
            _root = _server.Connect(_premiseServer, _premiseUser, _premisePassword);
            _home = _root.GetObject(_premiseHomeObject);

            Refresh();
        }

        private string _premiseServer;
        private string _premiseUser;
        private string _premisePassword;
        private string _premiseHomeObject;
        private string _premiseAlexaStatusClassPath;
        private string _premiseAlexaApplianceClassPath;

        private volatile bool _refreshing;

        private readonly SYSMiniBrokerClass _server;
        private readonly IRemotePremiseObject _root;
        private readonly IRemotePremiseObject _home;
        private SortedList<string, Appliance> _appliances;
        private AlexaStatus _alexaStatus;

        public AlexaStatus Status {  get { return _alexaStatus; } }

        public void Refresh()
        {
            if (_refreshing)
            { 
                return;
            }
            _refreshing = true;

            if (_root == null)
            {
                throw new Exception("Invalid sys root object.");
            }
            _premiseAlexaStatusClassPath = ConfigurationManager.AppSettings["premiseAlexaStatusClassPath"];
            _premiseAlexaApplianceClassPath = ConfigurationManager.AppSettings["premiseAlexaApplianceClassPath"];

            List<IRemotePremiseObject> statusRecords = new List<IRemotePremiseObject>();
            GetChildrenOfType(statusRecords, _home, _premiseAlexaStatusClassPath);
            if (statusRecords.Count > 0)
            {
                IRemotePremiseObject tmpStatus = statusRecords[0];
                tmpStatus.SetValue("RefreshDevices", true);

                if (_alexaStatus == null)
                {
                    _alexaStatus = new AlexaStatus();
                    _alexaStatus.RefreshRequested += RefreshRequested;
                    _alexaStatus.SetAlexaStatus(tmpStatus);
                }
                else
                {
                    _alexaStatus.UpdateAlexaStatus(tmpStatus);
                }

                refreshSchema();

                tmpStatus.SetValue("LastRefreshed", DateTime.Now);
                tmpStatus.SetValue("RefreshDevices", false);
            }
            else
            {
                _refreshing = false;
                throw new Exception("Can't find AlexaStatus instance.");
            }
            _refreshing = false;
        }

        private void RefreshRequested(object sender, EventArgs e)
        {

            var thread = new Thread(Refresh);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public IRemotePremiseObject Home
        {
            get { return _home; }
        }

        private static string getPropertyText(IRemotePremiseObject premiseObject, string propertyName)
        {
            if (premiseObject == null)
            {
                throw new Exception("Null Premise Object");
            }
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new Exception("Invalid Property Name");
            }
            var value = premiseObject.GetValue(propertyName);
            if (value == null)
            {
                throw new Exception("Cant find property");
            }
            return string.IsNullOrWhiteSpace(value.ToString()) ? string.Format("{0} {1}", premiseObject.Parent.Description, premiseObject.Description) : value.ToString();
        }

        public SortedList<string, Appliance> Appliances
        {
            get { return _appliances; }
        }

        /// <summary>
        /// Recurses the premise dom and builds a sorted list of objects of a specific premise class
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="obj"></param>
        /// <param name="pType"></param>
        private void GetChildrenOfType(SortedList<string, IRemotePremiseObject> collection, IRemotePremiseObject obj, string pType)
        {
            if ((obj == null) || (obj.children == null) || (obj.children.Count <= 0)) 
            {
                return;
            }
            foreach (IRemotePremiseObject premiseObject in obj.children)
            {
                if (premiseObject == null) // premise enumeration bug returns 1 more object than is actually there
                {
                    continue;
                }

                if (premiseObject.IsOfExplicitType(pType))
                {
                    string friendlyName = "";
                    try
                    {
                        friendlyName = getPropertyText(premiseObject, "FriendlyName");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("Error: path={0} key={1} message={2}", premiseObject.Path, "Cant find friendlyname", ex.Message));
                        continue;
                    }

                    try
                    {
                        collection.Add(friendlyName, premiseObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("Error: path={0} key={1} message={2}", premiseObject.Path, friendlyName, ex.Message));
                        continue;
                    }
                }
                else
                { 
                    GetChildrenOfType(collection, premiseObject, pType);
                }

            }
        }

        /// <summary>
        /// Recurses the premise dom and builds a list of objects of a specific premise class
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="obj"></param>
        /// <param name="pType"></param>
        private void GetChildrenOfType(List<IRemotePremiseObject> collection, IRemotePremiseObject obj, string pType)
        {
            if ((obj == null) || (obj.children == null) || (obj.children.Count <= 0))
            {
                return;
            }

            foreach (IRemotePremiseObject premiseObject in obj.children)
            {
                if (premiseObject == null) // premise enumeration bug returns 1 more object than is actually there
                {
                    continue;
                }

                if (premiseObject.IsOfExplicitType(pType))
                {
                    try
                    {
                        collection.Add(premiseObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("Error: path={0} message={1}", premiseObject.Path, ex.Message));
                        continue;
                    }
                }
                else
                {
                    GetChildrenOfType(collection, premiseObject, pType);
                }
            }
        }

        private void refreshSchema()
        {
            if (_appliances == null)
            {
                _appliances = new SortedList<string, Appliance>();
            }
            else { 
                _appliances.Clear();
            }

            SortedList<string, IRemotePremiseObject> tmpCollection = new SortedList<string, IRemotePremiseObject>();
            GetChildrenOfType(tmpCollection, _home, _premiseAlexaApplianceClassPath);

            foreach (KeyValuePair<string, IRemotePremiseObject> kvp in tmpCollection)
            {
                IRemotePremiseObject premiseObject = kvp.Value;
                try
                {

                    bool hasDimmer = (premiseObject.IsOfExplicitType("sys://Schema/Device/Dimmer"));

                    Appliance appliance = new Appliance()
                    {
                        applianceId = Guid.Parse(premiseObject.ObjectID).ToString("D"), // format 'D' removes curly braces
                        manufacturerName = "Premise Object",
                        modelName = premiseObject.Class.Name,
                        version = "1.0",
                        friendlyName = kvp.Key,
                        friendlyDescription = getPropertyText(premiseObject, "FriendlyDescription"), 
                        isReachable =  true,
                        additionalApplianceDetails = new AdditionalApplianceDetails() { dimmable = hasDimmer, path = premiseObject.Path }
                    };
                    _appliances.Add(kvp.Key, appliance);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(string.Format("Error: path={0} key={1} message={2}", premiseObject.Path, kvp.Key, ex.Message));
                    continue;
                }

            }
        }

        public static PremiseServer Instance
        {
            get { return instance; }
        }
    }
}
