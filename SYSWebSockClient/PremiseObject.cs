using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SYSWebSockClient
{
    using IPremiseObjectCollection = ICollection<IPremiseObject>;

    public class PremiseObject : IPremiseObject
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

        Task<IPremiseObject> IPremiseObject.CreateObject(IPremiseObject type, string name)
        {
            var future = new CreateObjectFuture(_objectId, (type as PremiseObject)?._objectId, name);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.CreateObject(string type, string name)
        {
            var future = new CreateObjectFuture(_objectId, type, name);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task IPremiseObject.Delete()
        {
            var future = new DeleteObjectFuture(_objectId, string.Empty);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.DeleteChildObject(string subObjectId)
        {
            if (!IsValidObjectId(subObjectId))
            {
                throw new ArgumentException("Invalid parameter", nameof(subObjectId));
            }

            var future = new DeleteObjectFuture(_objectId, subObjectId);

            _client.Send(future, out Task task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetAggregatedProperties()
        {
            var future = new GetAggregatedPropertiesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetAll()
        {
            var future = new GetAllFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetChildren()
        {
            var future = new GetChildrenFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetClass()
        {
            var future = new GetClassFuture(_objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetClasses()
        {
            var future = new GetClassesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetConnectedObjects()
        {
            var future = new GetConnectedObjectsFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetCreatableObjects()
        {
            var future = new GetCreatableObjectsFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<string> IPremiseObject.GetDescription()
        {
            var future = new GetDescriptionFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<string> IPremiseObject.GetDisplayName()
        {
            var future = new GetDisplayNameFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetMethods()
        {
            var future = new GetMethodsFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<string> IPremiseObject.GetName()
        {
            var future = new GetNameFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetObject(string subObject)
        {
            var future = new GetObjectFuture(_objectId, subObject);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<string> IPremiseObject.GetObjectID()
        {
            var future = new GetObjectIDFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetParent()
        {
            var future = new GetParentFuture(_objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<string> IPremiseObject.GetPath()
        {
            var future = new GetPathFuture(_objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetProperties()
        {
            var future = new GetPropertiesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<string> IPremiseObject.GetPropertyAsText(string propertyId)
        {
            var future = new GetPropertyAsTextFuture(_objectId, propertyId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<string> IPremiseObject.GetPropertyAsText(IPremiseObject property)
        {
            var future = new GetPropertyAsTextFuture(_objectId, (property as PremiseObject)?._objectId);

            _client.Send(future, out Task<string> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetRefValue(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            var future = new GetValueFuture(_objectId, name);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.GetRoot()
        {
            var future = new GetRootFuture(_objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSubClasses()
        {
            var future = new GetSubClassesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSuperClasses()
        {
            var future = new GetSuperClassesFuture(_objectId);

            _client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<ExpectedReturnType> IPremiseObject.GetValue<ExpectedReturnType>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var future = new GetValueFuture(_objectId, name);

            _client.Send(future, out Task<ExpectedReturnType> task);
            return task;
        }

        Task<dynamic> IPremiseObject.GetValue(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var future = new GetValueFuture(_objectId, name);
            _client.Send(future, out Task<dynamic> task);
            return task;
        }

        Task<bool> IPremiseObject.IsChildOf()
        {
            throw new NotImplementedException();
        }

        Task<bool> IPremiseObject.IsOfType(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                throw new ArgumentNullException(nameof(typeId));
            }

            var future = new IsOfTypeFuture(_objectId, typeId);

            _client.Send(future, out Task<bool> task);
            return task;
        }

        Task<bool> IPremiseObject.IsOfType(IPremiseObject typeId)
        {
            var future = new IsOfTypeFuture(_objectId, (typeId as PremiseObject)?._objectId);

            _client.Send(future, out Task<bool> task);
            return task;
        }

        public bool IsValidObject()
        {
            return IsValidObjectId(_objectId);
        }

        Task<dynamic> IPremiseObject.Select(ICollection<string> returnClause, dynamic whereClause)
        {
            var future = new SelectFuture(_objectId, returnClause, whereClause);

            _client.Send(future, out Task<dynamic> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.SetClass(IPremiseObject classObject)
        {
            var future = new SetClassFuture(_objectId, (classObject as PremiseObject)?._objectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task<IPremiseObject> IPremiseObject.SetClass(string classObjectId)
        {
            if (string.IsNullOrEmpty(classObjectId))
            {
                throw new ArgumentNullException(nameof(classObjectId));
            }

            var future = new SetClassFuture(_objectId, classObjectId);

            _client.Send(future, out Task<IPremiseObject> task);
            return task;
        }

        Task IPremiseObject.SetDescription(string name)
        {
            var future = new SetDescriptionFuture(_objectId, name);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.SetDisplayName(string name)
        {
            var future = new SetDisplayNameFuture(_objectId, name);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.SetName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            var future = new SetNameFuture(_objectId, name);

            _client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.SetValue(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            var future = new SetValueFuture(_objectId, name, value);

            _client.Send(future, out Task task);
            return task;
        }

        Task<IPremiseSubscription> IPremiseObject.Subscribe(string propertyName, string alexaController, Action<dynamic> callback)
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