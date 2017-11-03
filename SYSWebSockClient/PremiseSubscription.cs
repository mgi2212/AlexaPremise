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
            SysClient = sysClient;
            ObjectId = objectId;
            SubscriptionId = subscriptionId;
            clientSideSubscriptionId = clientSubscriptionId;
        }

        #endregion Constructors

        #region Methods

        Task IPremiseSubscription.Unsubscribe()
        {
            var future = new UnsubscribeFuture(ObjectId, SubscriptionId);

            SysClient.Send(future, out Task task);
            if (!SysClient.Subscriptions.TryRemove(clientSideSubscriptionId, out Subscription _))
            {
                Debug.WriteLine($"Subscription {clientSideSubscriptionId} Not Found");
            }
            return task;
        }

        #endregion Methods
    }
}