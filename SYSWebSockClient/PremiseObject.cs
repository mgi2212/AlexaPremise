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

            Task task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<string> IPremiseObject.GetName()
        {
            var future = new GetNameFuture(this.ObjectId);

            Task<string> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task IPremiseObject.SetDisplayName(string name)
        {
            var future = new SetDisplayNameFuture(this.ObjectId, name);

            Task task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<string> IPremiseObject.GetDisplayName()
        {
            var future = new GetDisplayNameFuture(this.ObjectId);

            Task<string> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task IPremiseObject.SetDescription(string name)
        {
            var future = new SetDescriptionFuture(this.ObjectId, name);

            Task task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<string> IPremiseObject.GetDescription()
        {
            var future = new GetDescriptionFuture(this.ObjectId);

            Task<string> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<string> IPremiseObject.GetObjectID()
        {
            var future = new GetObjectIDFuture(this.ObjectId);

            Task<string> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<string> IPremiseObject.GetPath()
        {
            var future = new GetPathFuture(this.ObjectId);

            Task<string> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetObject(string subObject)
        {
            var future = new GetObjectFuture(this.ObjectId, subObject);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetParent()
        {
            var future = new GetParentFuture(this.ObjectId);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetRoot()
        {
            var future = new GetRootFuture(this.ObjectId);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetClass()
        {
            var future = new GetClassFuture(this.ObjectId);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.SetClass(IPremiseObject classObjectId)
        {
            var future = new SetClassFuture(this.ObjectId, (classObjectId as PremiseObject).ObjectId);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.SetClass(string classObjectId)
        {
            var future = new SetClassFuture(this.ObjectId, classObjectId);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<ExpectedReturnType> IPremiseObject.GetValue<ExpectedReturnType>(string name)
        {
            var future = new GetValueFuture(this.ObjectId, name);

            Task<ExpectedReturnType> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<dynamic> IPremiseObject.GetValue(string name)
        {
            var future = new GetValueFuture(this.ObjectId, name);

            Task<dynamic> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.GetRefValue(string name)
        {
            var future = new GetValueFuture(this.ObjectId, name);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task IPremiseObject.SetValue(string name, string value)
        {
            var future = new SetValueFuture(this.ObjectId, name, value);

            Task task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.CreateObject(IPremiseObject type, string name)
        {
            var future = new CreateObjectFuture(this.ObjectId, (type as PremiseObject).ObjectId, name);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObject> IPremiseObject.CreateObject(string type, string name)
        {
            var future = new CreateObjectFuture(this.ObjectId, type, name);

            Task<IPremiseObject> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task IPremiseObject.Delete()
        {
            var future = new DeleteObjectFuture(this.ObjectId, string.Empty);

            Task task;
            this.Client.Send(future, out task);
            return task;
        }

        Task IPremiseObject.DeleteChildObject(string subObjectId)
        {
            if (string.IsNullOrEmpty(subObjectId))
                throw new ArgumentException("Invalid parameter", "subObjectId");

            var future = new DeleteObjectFuture(this.ObjectId, subObjectId);

            Task task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObjectCollection> IPremiseObject.GetAll()
        {
            var future = new GetAllFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObjectCollection> IPremiseObject.GetChildren()
        {
            var future = new GetChildrenFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<IPremiseObjectCollection> IPremiseObject.GetClasses()
        {
            var future = new GetClassesFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSubClasses()
        {
            var future = new GetSubClassesFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetSuperClasses()
        {
            var future = new GetSuperClassesFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetProperties()
        {
            var future = new GetPropertiesFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetAggregatedProperties()
        {
            var future = new GetAggregatedPropertiesFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetMethods()
        {
            var future = new GetMethodsFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetConnectedObjects()
        {
            var future = new GetConnectedObjectsFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseObjectCollection> IPremiseObject.GetCreatableObjects()
        {
            var future = new GetCreatableObjectsFuture(this.ObjectId);

            Task<IPremiseObjectCollection> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<string> IPremiseObject.GetPropertyAsText(string propertyId)
        {
            var future = new GetPropertyAsTextFuture(this.ObjectId, propertyId);

            Task<string> task;
            this.Client.Send(future, out task);
            return task;
        }
        Task<string> IPremiseObject.GetPropertyAsText(IPremiseObject property)
        {
            var future = new GetPropertyAsTextFuture(this.ObjectId, (property as PremiseObject).ObjectId);

            Task<string> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<bool> IPremiseObject.IsChildOf()
        {
            return null;
        }
        Task<bool> IPremiseObject.IsOfType(string typeId)
        {
            var future = new IsOfTypeFuture(this.ObjectId, typeId);

            Task<bool> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<bool> IPremiseObject.IsOfType(IPremiseObject typeId)
        {
            var future = new IsOfTypeFuture(this.ObjectId, (typeId as PremiseObject).ObjectId);

            Task<bool> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<dynamic> IPremiseObject.Select(ICollection<string> returnClause, dynamic whereClause)
        {
            var future = new SelectFuture(this.ObjectId, returnClause, whereClause);

            Task<dynamic> task;
            this.Client.Send(future, out task);
            return task;
        }

        Task<IPremiseSubscription> IPremiseObject.Subscribe(string propertyName, Action<dynamic> callback)
        {
            string clientSideSubscriptionId = FutureId.Next().ToString();

            this.Client.AddSubscription(clientSideSubscriptionId, callback);

            var future = new SubscribeFuture(this.ObjectId, propertyName, clientSideSubscriptionId);

            Task<IPremiseSubscription> task;
            this.Client.Send(future, out task);
            return task;
        }

        public override string ToString()
        {
            return this.ObjectId;
        }
    }
}
