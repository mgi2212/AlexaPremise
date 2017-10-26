using System.Diagnostics;
using System.Threading.Tasks;

namespace SYSWebSockClient
{
    public interface IPremiseSubscription
    {
        #region Methods

        Task Unsubscribe();

        #endregion Methods
    }

    public class PremiseSubscription : IPremiseSubscription
    {
        #region Fields

        public string clientSideSubscriptionId;
        private string ObjectId;
        private long SubscriptionId;
        private SYSClient SysClient;

        #endregion Fields

        #region Constructors

        internal PremiseSubscription(SYSClient sysClient, string objectId, long subscriptionId, string clientSubscriptionId)
        {
            this.SysClient = sysClient;
            this.ObjectId = objectId;
            this.SubscriptionId = subscriptionId;
            this.clientSideSubscriptionId = clientSubscriptionId;
        }

        #endregion Constructors

        #region Methods

        Task IPremiseSubscription.Unsubscribe()
        {
            var future = new UnsubscribeFuture(this.ObjectId, this.SubscriptionId);

            this.SysClient.Send(future, out Task task);
            if (!this.SysClient.Subscriptions.TryRemove(this.clientSideSubscriptionId, out Subscription _))
            {
                Debug.WriteLine($"Subscription {this.clientSideSubscriptionId} Not Found");
            }
            return task;
        }

        #endregion Methods
    }
}