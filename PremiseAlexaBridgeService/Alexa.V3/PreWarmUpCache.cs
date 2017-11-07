using System.Threading.Tasks;
using System.Web.Hosting;

namespace PremiseAlexaBridgeService
{
    public class PreWarmCache : IProcessHostPreloadClient
    {
        #region Methods

        public void Preload(string[] parameters)
        {
            Task.Run(() => PremiseServer.WarmUpCache());
        }

        #endregion Methods
    }
}