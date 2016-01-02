using System.ServiceModel;

namespace PremiseAlexaBridgeService
{
    [ServiceContract(Namespace = "http://premisesystems.com")]
    public interface IPremiseAlexaService
    {
        [OperationContract]
        DiscoveryResponse Discovery(DiscoveryRequest request);

        [OperationContract]
        ControlResponse Control(ControlRequest request);

        [OperationContract]
        SystemResponse System(SystemRequest request);
    }
}
