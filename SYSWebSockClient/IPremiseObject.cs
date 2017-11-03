using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SYSWebSockClient
{
    using IPremiseObjectCollection = ICollection<IPremiseObject>;

    public interface IPremiseObject
    {
        IPremiseObject WrapObjectId(string objectId);

        Task SetName(string name);

        Task<string> GetName();

        Task SetDisplayName(string name);

        Task<string> GetDisplayName();

        Task SetDescription(string name);

        Task<string> GetDescription();

        Task<string> GetObjectID();

        Task<string> GetPath();

        Task<IPremiseObject> GetObject(string subObject);

        Task<IPremiseObject> GetParent();

        Task<IPremiseObject> GetRoot();

        Task<IPremiseObject> GetClass();

        Task<IPremiseObject> SetClass(IPremiseObject type);

        Task<IPremiseObject> SetClass(string classObjectId);

        Task<ExpectedT> GetValue<ExpectedT>(string name);

        Task<dynamic> GetValue(string name);

        Task<IPremiseObject> GetRefValue(string name);

        Task SetValue(string name, string value);

        Task<IPremiseObject> CreateObject(IPremiseObject type, string name);

        Task<IPremiseObject> CreateObject(string type, string name);

        Task Delete();

        Task DeleteChildObject(string subObjectId);

        Task<IPremiseObjectCollection> GetAll();

        Task<IPremiseObjectCollection> GetChildren();

        Task<IPremiseObjectCollection> GetClasses();

        Task<IPremiseObjectCollection> GetSubClasses();

        Task<IPremiseObjectCollection> GetSuperClasses();

        Task<IPremiseObjectCollection> GetProperties();

        Task<IPremiseObjectCollection> GetAggregatedProperties();

        Task<IPremiseObjectCollection> GetMethods();

        Task<IPremiseObjectCollection> GetConnectedObjects();

        Task<IPremiseObjectCollection> GetCreatableObjects();

        Task<string> GetPropertyAsText(string propertyId);

        Task<string> GetPropertyAsText(IPremiseObject property);

        Task<bool> IsChildOf();

        Task<bool> IsOfType(string typeId);

        Task<bool> IsOfType(IPremiseObject typeId);

        Task<dynamic> Select(ICollection<string> returnClause, dynamic whereClause);

        Task<IPremiseSubscription> Subscribe(string propertyName, string alexaControllerName, Action<dynamic> callback);

        bool IsValidObject();

#if FOO
		Task SetFlags();
		Task GetFlags();
		Task SubscribeToProperty();
		Task SubscribeToCreate();
		Task SubscribeToDelete();
		Task Unsubscribe();
		Task GetAggregatedPropertyValue();
		Task FindClassProperty();
		Task InvokeProperty();
		Task PlayMacros();
		Task InvokeActionObject();
		Task RampProperty();
		Task GetDelegatedObjectEx();
		Task SetValueForced();
		Task Subscribe();
		Task FindPropertyByName();
		Task FindClassMethod();
		Task InvokeMethod();
		Task RunCommand();
		Task ImportXML();
		Task GetObjectsByType();
		Task GetNewObjectCollection();
		Task GetObjectsByPropertyName();
		Task GetObjectsByPropertyValue();
		Task GetObjectsByPropertySearch();
		Task GetObjectsByTypeAndPropertyValue();
		Task InsertEx();
#endif
    }
}