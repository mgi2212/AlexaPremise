using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace PremiseAlexaBridgeService
{
    public static class InputExtensions
    {
        #region Methods

        public static void AddUnique<TList>(this IList<TList> self, IEnumerable<TList> items)
        {
            foreach (var item in items)
                if (!self.Contains(item))
                    self.Add(item);
        }

        public static double LimitToRange(
                    this double value, double inclusiveMinimum, double inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }

        public static int LimitToRange(
            this int value, int inclusiveMinimum, int inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }

        #endregion Methods
    }

    public class PremiseAlexaBase
    {
        #region Utility

        public static async Task<bool> CheckAccessToken(string token)
        {
            var accessToken = await PremiseServer.HomeObject.GetValue<string>("AccessToken");
            List<string> tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

        public static async Task InformLastContact(string command)
        {
            await PremiseServer.HomeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            await PremiseServer.HomeObject.SetValue("LastHeardCommand", command);
        }

        #endregion Utility
    }

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