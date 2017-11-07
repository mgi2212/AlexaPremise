using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SYSWebSockClient
{
    using IPremiseObjectCollection = ICollection<IPremiseObject>;

    public interface IPremiseObject
    {
        IPremiseObject WrapObjectId(string objectId);

        Task SetNameAsync(string name);

        Task<string> GetNameAsync();

        Task SetDisplayNameAsync(string name);

        Task<string> GetDisplayNameAsync();

        Task SetDescriptionAsync(string name);

        Task<string> GetDescriptionAsync();

        Task<string> GetObjectIDAsync();

        Task<string> GetPathAsync();

        Task<IPremiseObject> GetObjectAsync(string subObject);

        Task<IPremiseObject> GetParentAsync();

        Task<IPremiseObject> GetRootAsync();

        Task<IPremiseObject> GetClassAsync();

        Task<IPremiseObject> SetClassAsync(IPremiseObject type);

        Task<IPremiseObject> SetClassAsync(string classObjectId);

        Task<ExpectedT> GetValueAsync<ExpectedT>(string name);

        Task<dynamic> GetValueAsync(string name);

        Task<IPremiseObject> GetRefValueAsync(string name);

        Task SetValueAsync(string name, string value);

        Task<IPremiseObject> CreateObjectAsync(IPremiseObject type, string name);

        Task<IPremiseObject> CreateObjectAsync(string type, string name);

        Task DeleteAsync();

        Task DeleteChildObjectAsync(string subObjectId);

        Task<IPremiseObjectCollection> GetAllAsync();

        Task<IPremiseObjectCollection> GetChildrenAsync();

        Task<IPremiseObjectCollection> GetClassesAsync();

        Task<IPremiseObjectCollection> GetSubClassesAsync();

        Task<IPremiseObjectCollection> GetSuperClassesAsync();

        Task<IPremiseObjectCollection> GetPropertiesAsync();

        Task<IPremiseObjectCollection> GetAggregatedPropertiesAsync();

        Task<IPremiseObjectCollection> GetMethodsAsync();

        Task<IPremiseObjectCollection> GetConnectedObjectsAsync();

        Task<IPremiseObjectCollection> GetCreatableObjectsAsync();

        Task<string> GetPropertyAsTextAsync(string propertyId);

        Task<string> GetPropertyAsTextAsync(IPremiseObject property);

        Task<bool> IsChildOfAsync();

        Task<bool> IsOfTypeAsync(string typeId);

        Task<bool> IsOfTypeAsync(IPremiseObject typeId);

        Task<dynamic> SelectAsync(ICollection<string> returnClause, dynamic whereClause);

        Task<IPremiseSubscription> SubscribeAsync(string propertyName, string alexaControllerName, Action<dynamic> callback);

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