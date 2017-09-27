namespace SYSWebSockClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IPremiseObjectCollection = System.Collections.Generic.ICollection<IPremiseObject>;

    public class PremiseObject : IPremiseObject
    {
        private SYSClient Client;
        private string ObjectId;

        public PremiseObject(string objectId)
        {
            this.Client = null;
            this.ObjectId = objectId;
        }

        public PremiseObject(SYSClient client, string objectId)
        {
            this.Client = client;
            this.ObjectId = objectId;
        }

        IPremiseObject IPremiseObject.WrapObjectId(string objectId)
        {
            var premiseObject = new PremiseObject(this.Client, objectId);
            return premiseObject;
        }


        Task IPremiseObject.SetName(string name)
        {
            var future = new SetNameFuture(this.ObjectId, name);

            this.Client.Send(future, out Task task);
            return task;
        }
        Task<string> IPremiseObject.GetName()
        {
            var future = new GetNameFuture(this.ObjectId);

            this.Client.Send(future, out Task<string> task);
            return task;
        }
        Task IPremiseObject.SetDisplayName(string name)
        {
            var future = new SetDisplayNameFuture(this.ObjectId, name);

            this.Client.Send(future, out Task task);
            return task;
        }
        Task<string> IPremiseObject.GetDisplayName()
        {
            var future = new GetDisplayNameFuture(this.ObjectId);

            this.Client.Send(future, out Task<string> task);
            return task;
        }

        Task IPremiseObject.SetDescription(string name)
        {
            var future = new SetDescriptionFuture(this.ObjectId, name);

            this.Client.Send(future, out Task task);
            return task;
        }
        Task<string> IPremiseObject.GetDescription()
        {
            var future = new GetDescriptionFuture(this.ObjectId);

            this.Client.Send(future, out Task<string> task);
            return task;
        }
        Task<string> IPremiseObject.GetObjectID()
        {
            var future = new GetObjectIDFuture(this.ObjectId);

            this.Client.Send(future, out Task<string> task);
            return task;
        }
        Task<string> IPremiseObject.GetPath()
        {
            var future = new GetPathFuture(this.ObjectId);

            this.Client.Send(future, out Task<string> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetObject(string subObject)
        {
            var future = new GetObjectFuture(this.ObjectId, subObject);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetParent()
        {
            var future = new GetParentFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetRoot()
        {
            var future = new GetRootFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetClass()
        {
            var future = new GetClassFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.SetClass(IPremiseObject classObjectId)
        {
            var future = new SetClassFuture(this.ObjectId, (classObjectId as PremiseObject).ObjectId);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.SetClass(string classObjectId)
        {
            var future = new SetClassFuture(this.ObjectId, classObjectId);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task<ExpectedReturnType> IPremiseObject.GetValue<ExpectedReturnType>(string name)
        {
            var future = new GetValueFuture(this.ObjectId, name);

            this.Client.Send(future, out Task<ExpectedReturnType> task);
            return task;
        }
        Task<dynamic> IPremiseObject.GetValue(string name)
        {
            var future = new GetValueFuture(this.ObjectId, name);
            // TODO - DANQ: find out why this will not return when a property is not found and fix 
            this.Client.Send(future, out Task<dynamic> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetRefValue(string name)
        {
            var future = new GetValueFuture(this.ObjectId, name);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task IPremiseObject.SetValue(string name, string value)
        {
            var future = new SetValueFuture(this.ObjectId, name, value);

            this.Client.Send(future, out Task task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.CreateObject(IPremiseObject type, string name)
        {
            var future = new CreateObjectFuture(this.ObjectId, (type as PremiseObject).ObjectId, name);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.CreateObject(string type, string name)
        {
            var future = new CreateObjectFuture(this.ObjectId, type, name);

            this.Client.Send(future, out Task<IPremiseObject> task);
            return task;
        }
        Task IPremiseObject.Delete()
        {
            var future = new DeleteObjectFuture(this.ObjectId, string.Empty);

            this.Client.Send(future, out Task task);
            return task;
        }

        Task IPremiseObject.DeleteChildObject(string subObjectId)
        {
            if (string.IsNullOrEmpty(subObjectId))
                throw new ArgumentException("Invalid parameter", "subObjectId");

            var future = new DeleteObjectFuture(this.ObjectId, subObjectId);

            this.Client.Send(future, out Task task);
            return task;
        }
        Task<IPremiseObjectCollection> IPremiseObject.GetAll()
        {
            var future = new GetAllFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }
        Task<IPremiseObjectCollection> IPremiseObject.GetChildren()
        {
            var future = new GetChildrenFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }
        Task<IPremiseObjectCollection> IPremiseObject.GetClasses()
        {
            var future = new GetClassesFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSubClasses()
        {
            var future = new GetSubClassesFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSuperClasses()
        {
            var future = new GetSuperClassesFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetProperties()
        {
            var future = new GetPropertiesFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetAggregatedProperties()
        {
            var future = new GetAggregatedPropertiesFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetMethods()
        {
            var future = new GetMethodsFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetConnectedObjects()
        {
            var future = new GetConnectedObjectsFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetCreatableObjects()
        {
            var future = new GetCreatableObjectsFuture(this.ObjectId);

            this.Client.Send(future, out Task<IPremiseObjectCollection> task);
            return task;
        }

        Task<string> IPremiseObject.GetPropertyAsText(string propertyId)
        {
            var future = new GetPropertyAsTextFuture(this.ObjectId, propertyId);

            this.Client.Send(future, out Task<string> task);
            return task;
        }
        Task<string> IPremiseObject.GetPropertyAsText(IPremiseObject property)
        {
            var future = new GetPropertyAsTextFuture(this.ObjectId, (property as PremiseObject).ObjectId);

            this.Client.Send(future, out Task<string> task);
            return task;
        }

        Task<bool> IPremiseObject.IsChildOf()
        {
            return null;
        }
        Task<bool> IPremiseObject.IsOfType(string typeId)
        {
            var future = new IsOfTypeFuture(this.ObjectId, typeId);

            this.Client.Send(future, out Task<bool> task);
            return task;
        }

        Task<bool> IPremiseObject.IsOfType(IPremiseObject typeId)
        {
            var future = new IsOfTypeFuture(this.ObjectId, (typeId as PremiseObject).ObjectId);

            this.Client.Send(future, out Task<bool> task);
            return task;
        }

        Task<dynamic> IPremiseObject.Select(ICollection<string> returnClause, dynamic whereClause)
        {
            var future = new SelectFuture(this.ObjectId, returnClause, whereClause);

            this.Client.Send(future, out Task<dynamic> task);
            return task;
        }

        Task<IPremiseSubscription> IPremiseObject.Subscribe(string propertyName, string alexaController, Action<dynamic> callback)
        {
            string clientSideSubscriptionId = FutureId.Next().ToString();

            Subscription subscription = new Subscription(clientSideSubscriptionId)
            {
                sysObjectId = this.ObjectId,
                propertyName = propertyName,
                alexaControllerName = alexaController,
                callback = callback
            };

            this.Client.AddSubscription(clientSideSubscriptionId, subscription);

            var future = new SubscribeFuture(this.ObjectId, propertyName, clientSideSubscriptionId);

            this.Client.Send(future, clientSideSubscriptionId, out Task<IPremiseSubscription> task);
            return task;
        }

        public override string ToString()
        {
            return this.ObjectId;
        }
    }
}
