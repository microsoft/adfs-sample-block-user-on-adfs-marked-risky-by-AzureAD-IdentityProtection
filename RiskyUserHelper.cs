using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.IdentityServer.Public.ThreatDetectionFramework;
//include dlls System.Web.Extensions

namespace ThreatDetectionModule
{
    public enum RiskLevel
    {
        None,
        Low,
        Medium,
        High
    }

    internal static class RiskyUserHelper
    {
        //internal static string TenantName = "[Azure AD Tenant Name]";
        //internal static string ClientId = "[Application (client) Id]";
        //internal static string ClientSecret = "[Application (client) Secret]";
        internal static string TenantName = "fabtoso.com";
        internal static string ClientId = "65cd9494-0f9c-4798-a5cc-94fff2c7ce40";
        internal static string ClientSecret = "X8L138wSbbjM.iPScy.RI/lPwN@h1[U_";
        internal static string LoginURL = String.Format("https://login.microsoft.com/{0}/oauth2/token?api-version=1.0", TenantName);
        internal class RiskyList
        {
            public Dictionary<string, string>[] Value { get; set; }
        }

        /// <summary>
        /// Gets the risk of a user by lookoing at the IP data from AAD
        /// </summary>
        /// <param name="upn">UPN of the user</param>
        /// <returns></returns>
        public static RiskScore GetRiskScore(string upn)
        {
            
            Dictionary<string, string> accessTokenResponse = GetAccessToken();
            RiskyList list = GetRiskyUsers(accessTokenResponse["token_type"], accessTokenResponse["access_token"]);
            Dictionary<string, RiskScore> riskyUsers = BuildRiskyData(list);
            RiskScore level = RiskScore.NotEvaluated;
            if (null != riskyUsers)
            {
                if (riskyUsers.ContainsKey(upn))
                {
                    level = riskyUsers[upn];
                }
            }
            return level;
        }

        static Dictionary<string, RiskScore> BuildRiskyData(RiskyList list)
        {
            Dictionary<string, RiskScore> riskyUserData = new Dictionary<string, RiskScore>();
            foreach (Dictionary<string, string> user in list.Value)
            {
                RiskScore level = RiskScore.NotEvaluated;
                if (user.ContainsKey("userPrincipalName") && !String.IsNullOrEmpty(user["userPrincipalName"]))
                {
                    if (!String.IsNullOrEmpty(user["riskLevel"]))

                    {
                        if (String.Compare(user["riskLevel"], "high", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            level = RiskScore.High;
                        }
                        else if (String.Compare(user["riskLevel"], "low", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            level = RiskScore.Low;
                        }
                        else if (String.Compare(user["riskLevel"], "medium", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            level = RiskScore.Medium;
                        }
                        riskyUserData.Add(user["userPrincipalName"], level);
                    }
                }
            }

            return riskyUserData;
        }

        // Gets the access token which can be used to get the Risky user information
        static Dictionary<string, string> GetAccessToken()
        {
            string body = String.Format("resource=https://graph.microsoft.com&grant_type=client_credentials&client_id={0}&client_secret={1}", ClientId, ClientSecret);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            var req = HttpWebRequest.Create(LoginURL);

            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = bodyBytes.Length;
            req.Method = "POST";
            req.GetRequestStream().Write(bodyBytes, 0, bodyBytes.Length);

            var response = req.GetResponse();
            Dictionary<string, string> jsonResponse = new Dictionary<string, string>();
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    String responseString = reader.ReadToEnd();
                    JavaScriptSerializer json_serializer = new JavaScriptSerializer();
                    jsonResponse = json_serializer.Deserialize<Dictionary<string, string>>(responseString);
                }
            }

            return jsonResponse;
        }

        // Get the Risky user data from the graph
        static RiskyList GetRiskyUsers(string tokenType, string accessToken)
        {
            var riskyReq = HttpWebRequest.Create("https://graph.microsoft.com/beta/riskyUsers");
            riskyReq.Headers.Add("Authorization", String.Format("{0} {1}", tokenType, accessToken));

            var responseRisky = riskyReq.GetResponse();
            using (Stream stream = responseRisky.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    String responseString = reader.ReadToEnd();
                    JavaScriptSerializer json_serializer = new JavaScriptSerializer();
                    return json_serializer.Deserialize<RiskyList>(responseString);
                }
            }
        }
    }
}