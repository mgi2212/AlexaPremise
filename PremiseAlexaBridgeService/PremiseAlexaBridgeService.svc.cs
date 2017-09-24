﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SYSWebSockClient;
using System.ServiceModel.Web;


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

    }

    public class PreWarmCache : System.Web.Hosting.IProcessHostPreloadClient
    {
        PremiseServer ServiceInstance;

        public void Preload(string[] parameters)
        {
            ServiceInstance = PremiseServer.Instance;
        }
    }

    /// <summary>
    /// Base Class
    /// </summary>

    public class PremiseAlexaBase
    {
        protected PremiseServer ServiceInstance = PremiseServer.Instance;

        #region Utility

        protected static async Task InformLastContact(string command)
        {
            await PremiseServer.HomeObject.SetValue("LastHeardFromAlexa", DateTime.Now.ToString());
            await PremiseServer.HomeObject.SetValue("LastHeardCommand", command);
        }

        protected static async Task<bool> CheckAccessToken(string token)
        {
            var accessToken = await PremiseServer.HomeObject.GetValue<string>("AccessToken");
            List<string> tokens = new List<string>(accessToken.Split(','));
            return (-1 != tokens.IndexOf(token));
        }

        protected static string NormalizeDisplayName(string displayName)
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
