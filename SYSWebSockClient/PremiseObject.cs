using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SYSWebSockClient
{
    using IPremiseObjectCollection = ICollection<IPremiseObject>;

    public sealed class PremiseObject : IPremiseObject
    {
        #region Fields

        private readonly SYSClient _client;
        private readonly string _objectId;

        #endregion Fields

        #region Constructors

        public PremiseObject(string objectId)
        {
            _client = null;
            _objectId = objectId;
        }

        public PremiseObject(SYSClient client, string objectId)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            _objectId = objectId;
        }

        #endregion Constructors

        #region Methods

        Task<IPremiseObject> IPremiseObject.CreateObjectAsync(IPremiseObject type, string name)
        {
            var future = new CreateObjectFuture(_objectId, (type as PremiseObject)?._objectId, name);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.CreateObjectAsync(string type, string name)
        {
            var future = new CreateObjectFuture(_objectId, type, name);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task IPremiseObject.DeleteAsync()
        {
            var future = new DeleteObjectFuture(_objectId, string.Empty);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.DeleteChildObjectAsync(string subObjectId)
        {
            if (!IsValidObjectId(subObjectId))
            {
                throw new ArgumentException("Invalid parameter", nameof(subObjectId));
            }

            var future = new DeleteObjectFuture(_objectId, subObjectId);

            _client.Send(future, out Task task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetAggregatedPropertiesAsync()
        {
            var future = new GetAggregatedPropertiesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetAllAsync()
        {
            var future = new GetAllFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetChildrenAsync()
        {
            var future = new GetChildrenFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetClassAsync()
        {
            var future = new GetClassFuture(_objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetClassesAsync()
        {
            var future = new GetClassesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetConnectedObjectsAsync()
        {
            var future = new GetConnectedObjectsFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetCreatableObjectsAsync()
        {
            var future = new GetCreatableObjectsFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<string> IPremiseObject.GetDescriptionAsync()
        {
            var future = new GetDescriptionFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<string> IPremiseObject.GetDisplayNameAsync()
        {
            var future = new GetDisplayNameFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetMethodsAsync()
        {
            var future = new GetMethodsFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<string> IPremiseObject.GetNameAsync()
        {
            var future = new GetNameFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetObjectAsync(string subObject)
        {
            var future = new GetObjectFuture(_objectId, subObject);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<string> IPremiseObject.GetObjectIDAsync()
        {
            var future = new GetObjectIDFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetParentAsync()
        {
            var future = new GetParentFuture(_objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<string> IPremiseObject.GetPathAsync()
        {
            var future = new GetPathFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetPropertiesAsync()
        {
            var future = new GetPropertiesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<string> IPremiseObject.GetPropertyAsTextAsync(string propertyId)
        {
            var future = new GetPropertyAsTextFuture(_objectId, propertyId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<string> IPremiseObject.GetPropertyAsTextAsync(IPremiseObject property)
        {
            var future = new GetPropertyAsTextFuture(_objectId, (property as PremiseObject)?._objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetRefValueAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            var future = new GetValueFuture(_objectId, name);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetRootAsync()
        {
            var future = new GetRootFuture(_objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSubClassesAsync()
        {
            var future = new GetSubClassesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSuperClassesAsync()
        {
            var future = new GetSuperClassesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<ExpectedReturnType> IPremiseObject.GetValueAsync<ExpectedReturnType>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var future = new GetValueFuture(_objectId, name);

            _client.Send(future, out Task<ExpectedReturnType> task);
            return task;
        }

        Task<dynamic> IPremiseObject.GetValueAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var future = new GetValueFuture(_objectId, name);
            _client.Send(future, out Task<dynamic> task);
            return task;
        }

        Task<bool> IPremiseObject.IsChildOfAsync()
        {
            throw new NotImplementedException();
        }

        Task<bool> IPremiseObject.IsOfTypeAsync(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                throw new ArgumentNullException(nameof(typeId));
            }

            var future = new IsOfTypeFuture(_objectId, typeId);

            _client.Send(future, out Task<bool> task);
            return task;
        }

        Task<bool> IPremiseObject.IsOfTypeAsync(IPremiseObject typeId)
        {
            var future = new IsOfTypeFuture(_objectId, (typeId as PremiseObject)?._objectId);

            _client.Send(future, out Task<bool> task);
            return task;
        }

        public bool IsValidObject()
        {
            return IsValidObjectId(_objectId);
        }

        Task<dynamic> IPremiseObject.SelectAsync(ICollection<string> returnClause, dynamic whereClause)
        {
            var future = new SelectFuture(_objectId, returnClause, whereClause);

            _client.Send(future, out Task<dynamic> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.SetClassAsync(IPremiseObject classObject)
        {
            var future = new SetClassFuture(_objectId, (classObject as PremiseObject)?._objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.SetClassAsync(string classObjectId)
        {
            if (string.IsNullOrEmpty(classObjectId))
            {
                throw new ArgumentNullException(nameof(classObjectId));
            }

            var future = new SetClassFuture(_objectId, classObjectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task IPremiseObject.SetDescriptionAsync(string name)
        {
            var future = new SetDescriptionFuture(_objectId, name);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.SetDisplayNameAsync(string name)
        {
            var future = new SetDisplayNameFuture(_objectId, name);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.SetNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            var future = new SetNameFuture(_objectId, name);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.SetValueAsync(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            var future = new SetValueFuture(_objectId, name, value);

            _client.Send(future, out Task task);
            return task;
        }

        Task<IPremiseSubscription> IPremiseObject.SubscribeAsync(string propertyName, string alexaController, Action<dynamic> callback)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException(nameof(propertyName));
            }

            string clientSideSubscriptionId = FutureId.Next().ToString();

            Subscription subscription = new Subscription(clientSideSubscriptionId)
            {
                sysObjectId = _objectId,
                propertyName = propertyName,
                alexaControllerName = alexaController,
                callback = callback
            };

            _client.AddSubscription(clientSideSubscriptionId, subscription);

            var future = new SubscribeFuture(_objectId, propertyName, clientSideSubscriptionId);

            _client.Send(future, clientSideSubscriptionId, out Task<IPremiseSubscription> task);
            return task;
        }

        public override string ToString()
        {
            return _objectId;
        }

        IPremiseObject IPremiseObject.WrapObjectId(string objectId)
        {
            var premiseObject = new PremiseObject(_client, objectId);
            return premiseObject;
        }

        private static bool IsValidObjectId(string objectId)
        {
            return !(string.IsNullOrEmpty(objectId) || objectId == "{00000000-0000-0000-0000-000000000000}");
        }

        #endregion Methods
    }
}