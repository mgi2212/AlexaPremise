using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace PremiseAlexaBridgeService
{
    
    [DataContract(Namespace ="")]
    public class AdditionalApplianceDetails
    {
        [DataMember(Name = "dimmable", Order = 1)]
        public bool dimmable { get; set; }

        [DataMember(Name = "path", Order = 2)]
        public string path { get; set; }

    }

    [DataContract(Namespace = "")]
    public class Appliance
    {
        [DataMember(Name = "applianceId",Order =1)]
        public string applianceId { get; set; }
        [DataMember(Name = "manufacturerName", EmitDefaultValue = false, Order = 2)]
        public string manufacturerName { get; set; }
        [DataMember(Name = "modelName", EmitDefaultValue = false, Order = 3)]
        public string modelName { get; set; }
        [DataMember(Name = "version", EmitDefaultValue = false, Order = 4)]
        public string version { get; set; }
        [DataMember(Name = "friendlyName", EmitDefaultValue = false, Order = 5)]
        public string friendlyName { get; set; }
        [DataMember(Name = "friendlyDescription", EmitDefaultValue = false, Order = 6)]
        public string friendlyDescription { get; set; }
        [DataMember(Name = "isReachable", EmitDefaultValue = false, Order = 7)]
        public bool isReachable { get; set; }
        [DataMember(Name = "additionalApplianceDetails", EmitDefaultValue = false, Order = 8)]
        public AdditionalApplianceDetails additionalApplianceDetails { get; set; }
    }


    [DataContract(Namespace = "")]
    public class ApplianceIdPayload
    {

        [DataMember(Name = "applianceId", Order = 1)]
        public string applianceId { get; set; }

        [DataMember(Name = "additionalApplianceDetails", EmitDefaultValue = false, Order = 2)]
        public AdditionalApplianceDetails additionalApplianceDetails { get; set; }
    }


    [DataContract(Name="payload", Namespace = "")]
    public class ApplianceControlRequestPayload
    {
        [DataMember(Name = "accessToken", Order = 1)]
        public string accessToken { get; set; }

        [DataMember(Name = "switchControlAction", EmitDefaultValue = false, Order = 2)]
        public string switchControlAction;

        [DataMember(Name = "adjustmentType", EmitDefaultValue = false, Order = 3)]
        public string adjustmentType;

        [DataMember(Name = "adjustmentUnit", EmitDefaultValue = false, Order = 4)]
        public string adjustmentUnit;

        [DataMember(Name = "adjustmentValue", EmitDefaultValue = false, Order = 5)]
        public string adjustmentValue;

        [DataMember(Name = "appliance", Order = 6)]
        public ApplianceIdPayload appliance;
    }

    [DataContract(Namespace = "")]
    public class ApplianceControlResponsePayload
    {
        [DataMember(Name = "success", Order = 1)]
        public bool success { get; set; }
        [DataMember(Name = "exception", EmitDefaultValue = false, Order = 2)]
        public ExceptionResponsePayload exception { get; set; }
    }

}
