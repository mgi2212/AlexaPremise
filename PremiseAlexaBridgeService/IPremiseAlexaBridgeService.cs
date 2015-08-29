using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace PremiseAlexaBridgeService
{
    [ServiceContract(Namespace = "http://premisesystems.com")]
    public interface IPremiseAlexaService
    {
        [OperationContract]
        DiscoveryResponse Discover(DiscoveryRequest request);

        [OperationContract]
        ControlResponse Control(ControlRequest request);

        [OperationContract]
        HealthCheckResponse Health(HealthCheckRequest request);

    }

    public enum ControlRequestType
    {
        Unknown,
        SwitchOnOff,
        AdjustNumericalSetting
    }

}
