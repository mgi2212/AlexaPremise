using System;
using System.ServiceModel.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
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
            _server = new Premise.SYSMiniBrokerClass();
            _home = _server.Connect("192.168.1.65", "danq", "Lazelle");
            _home = _home.GetObject("sys://home");

            Refresh();
        }

        private SYSMiniBrokerClass _server;
        private IRemotePremiseObject _home;
        private List<Appliance> _appliances;
        private AlexaStatus _alexaStatus;

        public AlexaStatus Status {  get { return _alexaStatus; } }

        public void Refresh()
        {
            refreshSchema();

            _alexaStatus = new AlexaStatus();

            List<IRemotePremiseObject> collection = new List<IRemotePremiseObject>();
            GetChildrenOfType(collection, _home, "sys://Schema/Modules/Alexa/Classes/AlexaStatus");
            _alexaStatus.GetAlexaStatus(collection);

        }

        public IRemotePremiseObject Home
        {
            get { return _home; }
        }

        public List<Appliance> Appliances
        {
            get { return _appliances; }
        }
        /// <summary>
        /// Recurses the premise dom and builds a list of objects of a specific premise class
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="obj"></param>
        /// <param name="pType"></param>
        private void GetChildrenOfType(List<IRemotePremiseObject> collection, IRemotePremiseObject obj, string pType)
        {
            if (obj.children.Count > 0)
            {
                foreach (IRemotePremiseObject pObj in obj.children)
                {
                    if (pObj == null)  // premise enumeration bug returns 1 more object than is actually there
                    {
                        continue;
                    }
                    if (pObj.children.Count > 0)
                    {
                        GetChildrenOfType(collection, pObj, pType);
                    }
                    if (pObj.IsOfExplicitType(pType))
                    {
                        collection.Add(pObj);
                    }
                }
            }
        }

        private void refreshSchema()
        {
            List<IRemotePremiseObject> collection = new List<IRemotePremiseObject>();
            GetChildrenOfType(collection, _home, "sys://Schema/Modules/Alexa/Classes/AlexaLightEx");

            _appliances = new List<Appliance>();

            foreach (IRemotePremiseObject premiseObject in collection)
            {
                try
                {

                bool hasDimmer = (premiseObject.IsOfExplicitType("sys://Schema/Device/Dimmer"));

                Appliance appliance = new Appliance()
                {
                    applianceId = Guid.Parse(premiseObject.ObjectID).ToString("D"),
                    manufacturerName = "AlexaLightEx",
                    modelName = premiseObject.Class.Name,
                    version = "1.0",
                    friendlyName = string.Format("{0} {1}", premiseObject.Parent.Description, premiseObject.DisplayName),
                    friendlyDescription = string.Format("{0} {1}", premiseObject.Parent.Description, premiseObject.DisplayName),
                    isReachable = true,
                    additionalApplianceDetails = new AdditionalApplianceDetails() { dimmable = hasDimmer, path = premiseObject.Path }
                };
                _appliances.Add(appliance);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
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
