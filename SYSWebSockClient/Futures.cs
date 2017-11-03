using System.Collections.Generic;

namespace SYSWebSockClient
{
    #region SetName

    internal class SetNameFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public SetNameFuture(string objectId, string name)
            : base(objectId, "setName")
        {
            @params = name;
        }

        #endregion Constructors
    }

    #endregion SetName

    #region GetName

    internal class GetNameFuture : JsonRPCFuture
    {
        #region Constructors

        public GetNameFuture(string objectId)
            : base(objectId, "getName")
        {
        }

        #endregion Constructors
    }

    #endregion GetName

    #region SetDisplayName

    internal class SetDisplayNameFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public SetDisplayNameFuture(string objectId, string name)
            : base(objectId, "setDisplayName")
        {
            @params = name;
        }

        #endregion Constructors
    }

    #endregion SetDisplayName

    #region GetDisplayName

    internal class GetDisplayNameFuture : JsonRPCFuture
    {
        #region Constructors

        public GetDisplayNameFuture(string objectId)
            : base(objectId, "getDisplayName")
        {
        }

        #endregion Constructors
    }

    #endregion GetDisplayName

    #region SetDescription

    internal class SetDescriptionFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public SetDescriptionFuture(string objectId, string name)
            : base(objectId, "setDescription")
        {
            @params = name;
        }

        #endregion Constructors
    }

    #endregion SetDescription

    #region GetDescription

    internal class GetDescriptionFuture : JsonRPCFuture
    {
        #region Constructors

        public GetDescriptionFuture(string objectId)
            : base(objectId, "getDescription")
        {
        }

        #endregion Constructors
    }

    #endregion GetDescription

    #region SetValue

    internal class SetValueFuture : JsonRPCFuture
    {
        #region Fields

        public Params @params;

        #endregion Fields

        #region Constructors

        public SetValueFuture(string objectId, string name, string value)
            : base(objectId, "setValue")
        {
            @params = new Params(name, value);
        }

        #endregion Constructors

        #region Classes

        internal class Params
        {
            #region Fields

            public string name;
            public string value;

            #endregion Fields

            #region Constructors

            public Params(
                string propertyName,
                string value)
            {
                name = propertyName;
                this.value = value;
            }

            #endregion Constructors
        }

        #endregion Classes
    }

    #endregion SetValue

    #region GetValue

    internal class GetValueFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public GetValueFuture(string objectId, string name)
            : base(objectId, "getValue")
        {
            @params = name;
        }

        #endregion Constructors
    }

    #endregion GetValue

    #region GetPropertyAsText

    internal class GetPropertyAsTextFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public GetPropertyAsTextFuture(string objectId, string propertyId)
            : base(objectId, "getPropertyAsText")
        {
            @params = propertyId;
        }

        #endregion Constructors
    }

    #endregion GetPropertyAsText

    #region GetClass

    internal class GetClassFuture : JsonRPCFuture
    {
        #region Constructors

        public GetClassFuture(string objectId)
            : base(objectId, "getClass")
        {
        }

        #endregion Constructors
    }

    #endregion GetClass

    #region SetClass

    internal class SetClassFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public SetClassFuture(string objectId, string classObjectId)
            : base(objectId, "setClass")
        {
            @params = classObjectId;
        }

        #endregion Constructors
    }

    #endregion SetClass

    #region GetFlags

    internal class GetFlagsFuture : JsonRPCFuture
    {
        #region Fields

        private readonly string _name;
        private readonly string _value;

        #endregion Fields

        #region Constructors

        public GetFlagsFuture(string objectId, string name, string value)
            : base(objectId, "getFlags")
        {
            _name = name;
            _value = value;
        }

        #endregion Constructors
    }

    #endregion GetFlags

    #region GetParent

    internal class GetParentFuture : JsonRPCFuture
    {
        #region Constructors

        public GetParentFuture(string objectId)
            : base(objectId, "getParent")
        {
        }

        #endregion Constructors
    }

    #endregion GetParent

    #region GetRoot

    internal class GetRootFuture : JsonRPCFuture
    {
        #region Constructors

        public GetRootFuture(string objectId)
            : base(objectId, "getRoot")
        {
        }

        #endregion Constructors
    }

    #endregion GetRoot

    #region GetPath

    internal class GetPathFuture : JsonRPCFuture
    {
        #region Constructors

        public GetPathFuture(string objectId)
            : base(objectId, "getPath")
        {
        }

        #endregion Constructors
    }

    #endregion GetPath

    #region GetObjectID

    internal class GetObjectIDFuture : JsonRPCFuture
    {
        #region Constructors

        public GetObjectIDFuture(string objectId)
            : base(objectId, "getObjectID")
        {
        }

        #endregion Constructors
    }

    #endregion GetObjectID

    #region GetObject

    internal class GetObjectFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public GetObjectFuture(string objectId, string subObject)
            : base(objectId, "getObject")
        {
            @params = subObject;
        }

        #endregion Constructors
    }

    #endregion GetObject

    #region GetAll

    internal class GetAllFuture : JsonRPCFuture
    {
        #region Constructors

        public GetAllFuture(string objectId)
            : base(objectId, "getAll")
        {
        }

        #endregion Constructors
    }

    #endregion GetAll

    #region GetChildren

    internal class GetChildrenFuture : JsonRPCFuture
    {
        #region Constructors

        public GetChildrenFuture(string objectId)
            : base(objectId, "getChildren")
        {
        }

        #endregion Constructors
    }

    #endregion GetChildren

    #region GetClasses

    internal class GetClassesFuture : JsonRPCFuture
    {
        #region Constructors

        public GetClassesFuture(string objectId)
            : base(objectId, "getClasses")
        {
        }

        #endregion Constructors
    }

    #endregion GetClasses

    #region GetSubClasses

    internal class GetSubClassesFuture : JsonRPCFuture
    {
        #region Constructors

        public GetSubClassesFuture(string objectId)
            : base(objectId, "getSubClasses")
        {
        }

        #endregion Constructors
    }

    #endregion GetSubClasses

    #region GetSuperClasses

    internal class GetSuperClassesFuture : JsonRPCFuture
    {
        #region Constructors

        public GetSuperClassesFuture(string objectId)
            : base(objectId, "getSuperClasses")
        {
        }

        #endregion Constructors
    }

    #endregion GetSuperClasses

    #region GetProperties

    internal class GetPropertiesFuture : JsonRPCFuture
    {
        #region Constructors

        public GetPropertiesFuture(string objectId)
            : base(objectId, "getProperties")
        {
        }

        #endregion Constructors
    }

    #endregion GetProperties

    #region GetAggregatedProperties

    internal class GetAggregatedPropertiesFuture : JsonRPCFuture
    {
        #region Constructors

        public GetAggregatedPropertiesFuture(string objectId)
            : base(objectId, "getAggregatedProperties")
        {
        }

        #endregion Constructors
    }

    #endregion GetAggregatedProperties

    #region GetMethods

    internal class GetMethodsFuture : JsonRPCFuture
    {
        #region Constructors

        public GetMethodsFuture(string objectId)
            : base(objectId, "getMethods")
        {
        }

        #endregion Constructors
    }

    #endregion GetMethods

    #region GetConnectedObjects

    internal class GetConnectedObjectsFuture : JsonRPCFuture
    {
        #region Constructors

        public GetConnectedObjectsFuture(string objectId)
            : base(objectId, "getConnectedObjects")
        {
        }

        #endregion Constructors
    }

    #endregion GetConnectedObjects

    #region GetCreatableObjects

    internal class GetCreatableObjectsFuture : JsonRPCFuture
    {
        #region Constructors

        public GetCreatableObjectsFuture(string objectId)
            : base(objectId, "getCreatableObjects")
        {
        }

        #endregion Constructors
    }

    #endregion GetCreatableObjects

    #region CreateObject

    internal class CreateObjectFuture : JsonRPCFuture
    {
        #region Fields

        public Params @params;

        #endregion Fields

        #region Constructors

        public CreateObjectFuture(string objectId, string type, string name)
            : base(objectId, "createObject")
        {
            @params = new Params(type, name);
        }

        #endregion Constructors

        #region Classes

        internal class Params
        {
            #region Fields

            public string name;
            public string type;

            #endregion Fields

            #region Constructors

            public Params(
                string type,
                string name)
            {
                this.type = type;
                this.name = name;
            }

            #endregion Constructors
        }

        #endregion Classes
    }

    #endregion CreateObject

    #region DeleteObject

    internal class DeleteObjectFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public DeleteObjectFuture(string objectId, string subObjectId)
            : base(objectId, "deleteObject")
        {
            @params = subObjectId;
        }

        #endregion Constructors
    }

    #endregion DeleteObject

    #region IsOfType

    internal class IsOfTypeFuture : JsonRPCFuture
    {
        #region Fields

        public string @params;

        #endregion Fields

        #region Constructors

        public IsOfTypeFuture(string objectId, string typeId)
            : base(objectId, "isOfType")
        {
            @params = typeId;
        }

        #endregion Constructors
    }

    #endregion IsOfType

    #region Subscribe

    internal class SubscribeFuture : JsonRPCFuture
    {
        #region Fields

        public Params @params;

        #endregion Fields

        #region Constructors

        public SubscribeFuture(string objectId, string propertyName, string callbackMethod)
            : base(objectId, "subscribeToProperty")
        {
            @params = new Params(objectId, propertyName, callbackMethod);
        }

        #endregion Constructors

        #region Classes

        internal class Params
        {
            #region Fields

            public string callbackMethod;
            public string objectId;
            public string propertyName;

            #endregion Fields

            #region Constructors

            public Params(
                string objectId,
                string propertyName,
                string callbackMethod)
            {
                this.objectId = objectId;
                this.propertyName = propertyName;
                this.callbackMethod = callbackMethod;
            }

            #endregion Constructors
        }

        #endregion Classes
    }

    #endregion Subscribe

    #region Unsubscribe

    internal class UnsubscribeFuture : JsonRPCFuture
    {
        #region Fields

        public long @params;

        #endregion Fields

        #region Constructors

        public UnsubscribeFuture(string objectId, long subscriptionId)
            : base(objectId, "unsubscribe")
        {
            @params = subscriptionId;
        }

        #endregion Constructors
    }

    #endregion Unsubscribe

    #region SelectFuture

    internal class SelectFuture : JsonRPCFuture
    {
        #region Fields

        public Params @params;

        #endregion Fields

        #region Constructors

        public SelectFuture(string objectId, dynamic returnClause, dynamic whereClause)
            : base(objectId, "select")
        {
            @params = new Params(returnClause, whereClause);
        }

        #endregion Constructors

        #region Classes

        internal class Params
        {
            #region Fields

            public ICollection<string> @return;
            public dynamic where;

            #endregion Fields

            #region Constructors

            public Params(dynamic returnClause, dynamic whereClause)
            {
                @return = returnClause;
                where = whereClause;
            }

            #endregion Constructors
        }

        #endregion Classes
    }

    #endregion SelectFuture
}