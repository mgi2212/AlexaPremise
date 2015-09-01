using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.ServiceModel;
using Premise;

namespace PremiseAlexaBridgeService
{
    [ComVisible(true)]
    public class AlexaStatus
    {

        bool _health;
        string _description;
        string _accessToken;
        int _subscriptionID;
        IRemotePremiseObject _status;

        public AlexaStatus()
        {
            _health = false;
            _description = "Health is unknown.";
            _accessToken = "unknown";
            _subscriptionID = 0;
        }

        ~AlexaStatus()
        {
            if (_status != null)
            {
                _status.Unsubscribe(_subscriptionID);
            }
            _status = null;
        }

        public bool Health { get { return _health; } }
        public string HealthDescription { get { return _description; } }
        public string AccessToken { get { return _accessToken; } }

        public delegate void EventHandler(object sender, EventArgs e);
        public event EventHandler RefreshRequested;

        public void UnSubscribe()
        {
            if ((_status != null) && (_subscriptionID != 0))
            {
                _status.Unsubscribe(_subscriptionID);
            }
        }

        public void UpdateAlexaStatus(IRemotePremiseObject record)
        {
            _health = Convert.ToBoolean(_status.GetValue("Health"));
            _description = _status.GetValue("HealthDescription").ToString();
            _accessToken = _status.GetValue("AccessToken").ToString();
        }

        public void SetAlexaStatus(IRemotePremiseObject record)
        {
            _status = record;
            _subscriptionID = _status.SubscribeToProperty("RefreshDevices", "OnRefreshDevices", this);
            UpdateAlexaStatus(record);
        }

        [ComVisible(true)]
        public void OnRefreshDevices(int subID, IRemotePremiseObject objectChanged, IRemotePremiseObject property, object newValue)
        {
            try
            {
                if ((bool)newValue)
                {
                    if (RefreshRequested != null)
                    {
                        RefreshRequested(this, new EventArgs());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

        }
    }
}
