using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PremiseAlexaBridgeService
{

    public static class InputExtensions
    {
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

        public static void AddUnique<List>(this IList<List> self, IEnumerable<List> items)
        {
            foreach (var item in items)
                if (!self.Contains(item))
                    self.Add(item);
        }
    }

    public class PreWarmCache : System.Web.Hosting.IProcessHostPreloadClient
    {
        public void Preload(string[] parameters)
        {
            Task t = Task.Run(() => PremiseServer.WarmUpCache());

        }
    }

    /// <summary>
    /// Base Class
    /// </summary>

    public class PremiseAlexaBase
    {
        #region Utility

        public static async Task InformLastContact(string command)
        {
            await PremiseServer.HomeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString());
            await PremiseServer.HomeObject.SetValue("LastHeardCommand", command);
        }

        public static async Task<bool> CheckAccessToken(string token)
        {
            var accessToken = await PremiseServer.HomeObject.GetValue<string>("AccessToken");
            List<string> tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

        public static string NormalizeDisplayName(string displayName)
        {
            displayName = displayName.Trim();

            if ((!string.IsNullOrEmpty(displayName)) && (displayName.IndexOf("(Occupied)") != -1))
            {
                return displayName.Replace("(Occupied)", "").Trim();
            }
            return displayName;
        }

        #endregion
    }
}
