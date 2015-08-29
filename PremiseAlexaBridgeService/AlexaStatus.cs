using System;
using System.ServiceModel.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using Premise;

namespace PremiseAlexaBridgeService
{
    public class AlexaStatus
    {
        bool _health;
        string _description;
        string _accessToken;

        public AlexaStatus ()
        {
            _health = false;
            _description = "Health is unknown.";
            _accessToken = "dan";
        }

        public bool Health { get { return _health; } }
        public string HealthDescription { get { return _description; } }
        public string AccessToken { get { return _accessToken; } }

        public void GetAlexaStatus(List<IRemotePremiseObject> statusRecords)
        {
            if (statusRecords.Count > 0)
            {
                IRemotePremiseObject record = statusRecords[0];
                _health = Convert.ToBoolean(record.GetValue("Health"));
                _description = record.GetValue("HealthDescription").ToString();
                _accessToken = record.GetValue("AccessToken").ToString();
            }
        }
    }
}
