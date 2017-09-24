using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SYSWebSockClient
{

    public interface IPremiseSubscription
    {
        Task Unsubscribe();
    }

    public class PremiseSubscription : IPremiseSubscription
    {
        private SYSClient SysClient;
        private string ObjectId; 
        private long SubscriptionId;
        public string clientSideSubscriptionId;

        internal PremiseSubscription(SYSClient sysClient, string objectId, long subscriptionId, string clientSubscriptionId)
        {
            this.SysClient = sysClient;
            this.ObjectId = objectId;
            this.SubscriptionId = subscriptionId;
            this.clientSideSubscriptionId = clientSubscriptionId;
        }

        Task IPremiseSubscription.Unsubscribe()
        {
            var future = new UnsubscribeFuture(this.ObjectId, this.SubscriptionId);

            this.SysClient.Send(future, out Task task);
            if (!this.SysClient.Subscriptions.TryRemove(this.clientSideSubscriptionId, out Subscription subscription))
            {
                Debug.WriteLine(string.Format("Subscription {0} Not Found", this.clientSideSubscriptionId));
            }
            return task;
        }
    }
}
