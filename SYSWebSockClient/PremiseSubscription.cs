using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SYSWebSockClient
{

    public interface IPremiseSubscription
    {
        Task Unsubscribe();
    }

    class PremiseSubscription : IPremiseSubscription
    {
        private SYSClient SysClient;
        private string ObjectId; 
        private long SubscriptionId;

        internal PremiseSubscription(SYSClient sysClient, string objectId, long subscriptionId)
        {
            this.SysClient = sysClient;
            this.ObjectId = objectId;
            this.SubscriptionId = subscriptionId;
        }

        Task IPremiseSubscription.Unsubscribe()
        {
            var future = new UnsubscribeFuture(this.ObjectId, this.SubscriptionId);
            
            Task task;
            this.SysClient.Send(future, out task);
            return task;
        }
    }
}
