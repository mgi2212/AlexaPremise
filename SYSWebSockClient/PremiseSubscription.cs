using System.Diagnostics;
using System.Threading.Tasks;

namespace SYSWebSockClient
{
    public interface IPremiseSubscription
    {
        #region Methods

        Task UnsubscribeAsync();

        #endregion Methods
    }

    public sealed class PremiseSubscription : IPremiseSubscription
    {
        #region Fields

        public string clientSideSubscriptionId;
        private readonly string _objectId;
        private readonly long _subscriptionId;
        private readonly SYSClient _sysClient;

        #endregion Fields

        #region Constructors

        internal PremiseSubscription(SYSClient sysClient, string objectId, long subscriptionId, string clientSubscriptionId)
        {
            _sysClient = sysClient;
            _objectId = objectId;
            _subscriptionId = subscriptionId;
            clientSideSubscriptionId = clientSubscriptionId;
        }

        #endregion Constructors

        #region Methods

        Task IPremiseSubscription.UnsubscribeAsync()
        {
            var future = new UnsubscribeFuture(_objectId, _subscriptionId);

            _sysClient.Send(future, out Task task);
            if (!_sysClient.Subscriptions.TryRemove(clientSideSubscriptionId, out Subscription _))
            {
                Debug.WriteLine($"Subscription {clientSideSubscriptionId} Not Found");
            }
            return task;
        }

        #endregion Methods
    }
}