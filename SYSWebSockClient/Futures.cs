namespace SYSWebSockClient
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    #region SetName
    internal class SetNameFuture : JsonRPCFuture
    {
        public string @params;

        public SetNameFuture(string objectId, string name)
            : base(objectId, "setName")
        {
            this.@params = name;
        }
    }
    #endregion

    #region GetName
    internal class GetNameFuture : JsonRPCFuture
    {
        public GetNameFuture(string objectId)
            : base(objectId, "getName")
        {
        }
    }
    #endregion

    #region SetDisplayName
    internal class SetDisplayNameFuture : JsonRPCFuture
    {
        public string @params;

        public SetDisplayNameFuture(string objectId, string name)
            : base(objectId, "setDisplayName")
        {
            this.@params = name;
        }
    }
    #endregion

    #region GetDisplayName
    internal class GetDisplayNameFuture : JsonRPCFuture
    {
        public GetDisplayNameFuture(string objectId)
            : base(objectId, "getDisplayName")
        {
        }
    }
    #endregion

    #region SetDescription
    internal class SetDescriptionFuture : JsonRPCFuture
    {
        public string @params;

        public SetDescriptionFuture(string objectId, string name)
            : base(objectId, "setDescription")
        {
            this.@params = name;
        }
    }
    #endregion

    #region GetDescription
    internal class GetDescriptionFuture : JsonRPCFuture
    {
        public GetDescriptionFuture(string objectId)
            : base(objectId, "getDescription")
        {
        }
    }
    #endregion

    #region SetValue
    internal class SetValueFuture : JsonRPCFuture
    {
        public Params @params;

        public SetValueFuture(string objectId, string name, string value)
            : base(objectId, "setValue")
        {
            this.@params = new Params(name, value);
        }

        internal class Params
        {
            public string name;
            public string value;

            public Params(
                string propertyName,
                string value)
            {
                this.name = propertyName;
                this.value = value;
            }
        };
    }
    #endregion

    #region GetValue
    internal class GetValueFuture : JsonRPCFuture
    {
        public string @params;

        public GetValueFuture(string objectId, string name)
            : base(objectId, "getValue")
        {
            this.@params = name;
        }
    }
    #endregion

    #region GetPropertyAsText
    internal class GetPropertyAsTextFuture : JsonRPCFuture
    {
        public string @params;

        public GetPropertyAsTextFuture(string objectId, string propertyId)
            : base(objectId, "getPropertyAsText")
        {
            this.@params = propertyId;
        }
    }
    #endregion

    #region GetClass
    internal class GetClassFuture : JsonRPCFuture
    {
        public GetClassFuture(string objectId)
            : base(objectId, "getClass")
        {
        }
    }
    #endregion

    #region SetClass
    internal class SetClassFuture : JsonRPCFuture
    {
        public string @params;

        public SetClassFuture(string objectId, string classObjectId)
            : base(objectId, "setClass")
        {
            this.@params = classObjectId;
        }
    }
    #endregion

    #region GetFlags
    internal class GetFlagsFuture : JsonRPCFuture
    {
        public GetFlagsFuture(string objectId, string name, string value)
            : base(objectId, "getFlags")
        {
        }
    }
    #endregion

    #region GetParent
    internal class GetParentFuture : JsonRPCFuture
    {
        public GetParentFuture(string objectId)
            : base(objectId, "getParent")
        {
        }
    }
    #endregion

    #region GetRoot
    internal class GetRootFuture : JsonRPCFuture
    {
        public GetRootFuture(string objectId)
            : base(objectId, "getRoot")
        {
        }
    }
    #endregion

    #region GetPath
    internal class GetPathFuture : JsonRPCFuture
    {
        public GetPathFuture(string objectId)
            : base(objectId, "getPath")
        {
        }
    }
    #endregion

    #region GetObjectID
    internal class GetObjectIDFuture : JsonRPCFuture
    {
        public GetObjectIDFuture(string objectId)
            : base(objectId, "getObjectID")
        {
        }
    }
    #endregion

    #region GetObject
    internal class GetObjectFuture : JsonRPCFuture
    {
        public string @params;

        public GetObjectFuture(string objectId, string subObject)
            : base(objectId, "getObject")
        {
            this.@params = subObject;
        }
    }
    #endregion

    #region GetAll
    internal class GetAllFuture : JsonRPCFuture
    {
        public GetAllFuture(string objectId)
            : base(objectId, "getAll")
        {
        }
    }
    #endregion

    #region GetChildren
    internal class GetChildrenFuture : JsonRPCFuture
    {
        public GetChildrenFuture(string objectId)
            : base(objectId, "getChildren")
        {
        }
    }
    #endregion

    #region GetClasses
    internal class GetClassesFuture : JsonRPCFuture
    {
        public GetClassesFuture(string objectId)
            : base(objectId, "getClasses")
        {
        }
    }
    #endregion

    #region GetSubClasses
    internal class GetSubClassesFuture : JsonRPCFuture
    {
        public GetSubClassesFuture(string objectId)
            : base(objectId, "getSubClasses")
        {
        }
    }
    #endregion

    #region GetSuperClasses
    internal class GetSuperClassesFuture : JsonRPCFuture
    {
        public GetSuperClassesFuture(string objectId)
            : base(objectId, "getSuperClasses")
        {
        }
    }
    #endregion

    #region GetProperties
    internal class GetPropertiesFuture : JsonRPCFuture
    {
        public GetPropertiesFuture(string objectId)
            : base(objectId, "getProperties")
        {
        }
    }
    #endregion

    #region GetAggregatedProperties
    internal class GetAggregatedPropertiesFuture : JsonRPCFuture
    {
        public GetAggregatedPropertiesFuture(string objectId)
            : base(objectId, "getAggregatedProperties")
        {
        }
    }
    #endregion

    #region GetMethods
    internal class GetMethodsFuture : JsonRPCFuture
    {
        public GetMethodsFuture(string objectId)
            : base(objectId, "getMethods")
        {
        }
    }
    #endregion

    #region GetConnectedObjects
    internal class GetConnectedObjectsFuture : JsonRPCFuture
    {
        public GetConnectedObjectsFuture(string objectId)
            : base(objectId, "getConnectedObjects")
        {
        }
    }
    #endregion

    #region GetCreatableObjects
    internal class GetCreatableObjectsFuture : JsonRPCFuture
    {
        public GetCreatableObjectsFuture(string objectId)
            : base(objectId, "getCreatableObjects")
        {
        }
    }
    #endregion


    #region CreateObject
    internal class CreateObjectFuture : JsonRPCFuture
    {
        public Params @params;

        public CreateObjectFuture(string objectId, string type, string name)
            : base(objectId, "createObject")
        {
            this.@params = new Params(type, name);
        }

        internal class Params
        {
            public string type;
            public string name;

            public Params(
                string type,
                string name)
            {
                this.type = type;
                this.name = name;
            }
        };
    }
    #endregion

    #region DeleteObject
    internal class DeleteObjectFuture : JsonRPCFuture
    {
        public string @params;

        public DeleteObjectFuture(string objectId, string subObjectId)
            : base(objectId, "deleteObject")
        {
            this.@params = subObjectId;
        }
    }
    #endregion

    #region IsOfType
    internal class IsOfTypeFuture : JsonRPCFuture
    {
        public string @params;

        public IsOfTypeFuture(string objectId, string typeId)
            : base(objectId, "isOfType")
        {
            this.@params = typeId;
        }
    }
    #endregion

    #region Subscribe
    internal class SubscribeFuture : JsonRPCFuture
    {
        public Params @params;

        public SubscribeFuture(string objectId, string propertyName, string callbackMethod)
            : base(objectId, "subscribeToProperty")
        {
            this.@params = new Params(propertyName, callbackMethod);
        }

        internal class Params
        {
            public string propertyName;
            public string callbackMethod;

            public Params(
                string propertyName,
                string callbackMethod)
            {
                this.propertyName = propertyName;
                this.callbackMethod = callbackMethod;
            }
        };
    }
    #endregion

    #region Unsubscribe
    internal class UnsubscribeFuture : JsonRPCFuture
    {
        public long @params;

        public UnsubscribeFuture(string objectId, long subscriptionId)
            : base(objectId, "unsubscribe")
        {
            this.@params = subscriptionId;
        }
    }
    #endregion

    #region SelectFuture
    internal class SelectFuture : JsonRPCFuture
    {
        public Params @params;

        public SelectFuture(string objectId, dynamic returnClause, dynamic whereClause)
            : base(objectId, "select")
        {
            this.@params = new Params(returnClause, whereClause);
        }

        internal class Params
        {
            public ICollection<string> @return;
            public dynamic @where;

            public Params(dynamic returnClause, dynamic whereClause)
            {
                this.@return = returnClause;
                this.@where = whereClause;
            }
        };
    }
    #endregion
}
