//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2021 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Ict.Common.IO
{
    /// <summary>
    /// a few simple functions to access content from the web
    /// </summary>
    public class THTTPUtils
    {
        /// <summary>
        /// this message is transfered via 404 code to the client
        /// </summary>
        public const String SESSION_ALREADY_CLOSED = "SESSION_ALREADY_CLOSED";

        private class WebClientWithSession : WebClient
        {
            public WebClientWithSession()
                : this(new CookieContainer())
            {
            }

            public WebClientWithSession(CookieContainer c)
            {
                this.CookieContainer = c;

                // see http://stackoverflow.com/questions/566437/http-post-returns-the-error-417-expectation-failed-c
                System.Net.ServicePointManager.Expect100Continue = false;

                if (TAppSettingsManager.GetValue("IgnoreServerCertificateValidation", "false", false) == "true")
                {
                    // when checking the validity of a SSL certificate, always pass
                    // this only makes sense in a testing environment, with self signed certificates
                    ServicePointManager.ServerCertificateValidationCallback =
                        new RemoteCertificateValidationCallback(
                            delegate
                            { return true; }
                            );
                }
            }

            public CookieContainer CookieContainer {
                get; set;
            }

            public byte[] Get(string AUrl, NameValueCollection AParameters = null)
            {
                byte[] result = null;

                if (AParameters == null)
                {
                    result = DownloadData(AUrl);
                }
                else
                {
                    result = UploadValues(AUrl, AParameters);
                }

                return result;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                TLogging.LogAtLevel(1, "GetWebRequest: got called for URI: " + address.ToString());
                HttpWebRequest request = (HttpWebRequest) base.GetWebRequest(address);

                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/6.0; OpenPetraFatClient)";

                request.Timeout = Convert.ToInt32(
                    TimeSpan.FromMinutes(TAppSettingsManager.GetInt32("WebRequestTimeOutInMinutes", 15)).
                    TotalMilliseconds);

                // TODO Set HttpWebRequest.KeepAlive property to false to avoid  'The request was aborted: The request was canceled.' when multiple Threads run on one TCP Session ?????
                // see http://www.jaxidian.org/update/2007/05/05/8/

                var castRequest = request as HttpWebRequest;

                if (castRequest != null)
                {
                    castRequest.CookieContainer = this.CookieContainer;

                    foreach (Cookie IndivCookie in castRequest.CookieContainer.GetCookies(address))
                    {
                        TLogging.LogAtLevel(1, "GetWebRequest: castRequest.CookieContainer cookie\r\n" +
                            "Name: " + IndivCookie.Name + "\r\n" +
                            "Value: " + IndivCookie.Value + "\r\n" +
                            "Path: " + IndivCookie.Path + "\r\n" +
                            "Domain: " + IndivCookie.Domain);
                    }
                }

                return request;
            }
        }

        /// <summary>
        /// Storage of the single cookie that stores the Session ID.
        /// </summary>
        /// <remarks>
        /// Set in <see cref="StoreSessionCookie" /> if we don't have a session cookie yet,
        /// and set to null only in <see cref="ResetSession" />. Other than that this
        /// static Field is only read!
        /// </remarks>
        [ThreadStatic]
        public static Cookie OverallCookie = null;

        /// reset the static variables for each Web Request call.
        public static void ResetStaticVariables()
        {
            OverallCookie = null;
        }

        /// <summary>
        /// Throws away the previous client session cookies!
        /// </summary>
        public static void ResetSession()
        {
            OverallCookie = null;
        }

        /// <summary>
        /// read from a website;
        /// </summary>
        public static string ReadWebsite(string url)
        {
            return ReadWebsiteAsync(url).Result;
        }

        /// <summary>
        /// read from a website;
        /// </summary>
        private static async Task<string> ReadWebsiteAsync(string url)
        {
            string urlToLog = url;

            // hide password and other parameters from the log
            if (urlToLog.Contains("?"))
            {
                urlToLog = urlToLog.Substring(0, urlToLog.IndexOf("?")) + "?...";
            }

            TLogging.LogAtLevel(1, "ReadWebsite: " + urlToLog);
            string ReturnValue = String.Empty;

            Uri fullUri = new Uri(url);
            string baseUrl = fullUri.GetComponents(
                UriComponents.SchemeAndServer | UriComponents.UserInfo, UriFormat.Unescaped);
            Uri baseAddress = new Uri(baseUrl);
            string pageUrl = url.Substring(baseUrl.Length);
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                {
                    var result = client.GetAsync(pageUrl).Result;
                    if (result.IsSuccessStatusCode)
                    {
                        byte[] buf = await result.Content.ReadAsByteArrayAsync();

                        if ((buf != null) && (buf.Length > 0))
                        {
                            ReturnValue = Encoding.UTF8.GetString(buf, 0, buf.Length);
                        }
                        else
                        {
                            TLogging.Log("server did not return anything? timeout?");
                        }

                        StoreSessionCookie(cookieContainer, baseAddress.ToString());
                    }
                    else
                    {
                        var error = await result.Content.ReadAsStringAsync();
                        TLogging.Log("Trying to download: " + urlToLog + Environment.NewLine +
                            result.StatusCode.ToString() + " " + error, TLoggingType.ToLogfile);
                        throw new Exception("THTTPUtils::ReadWebsite: " + urlToLog + " " + result.StatusCode.ToString() + " " + error);
                    }
                }
            }

            return ReturnValue;
        }

        private static void LogRequest(string url, NameValueCollection parameters)
        {
            TLogging.Log(url);

            foreach (string k in parameters.Keys)
            {
                if (k.ToLower().Contains("password"))
                {
                    TLogging.Log(" " + k + " = *****");
                }
                else
                {
                    if (parameters[k].Length < 2000)
                    {
                        TLogging.Log(" " + k + " = " + parameters[k]);
                    }
                    else
                    {
                        TLogging.Log(" " + k + " = " + parameters[k].Substring(0, 1000) + " [..]");
                    }
                }
            }
        }

        private static WebClientWithSession GetNewWebClient(string url)
        {
            CookieContainer CookieCont = new CookieContainer();

            TLogging.LogAtLevel(1, "SetWebClient: url argument is: " + url);

            if (url.StartsWith("Login"))
            {
                ResetSession();
            }

            if ((OverallCookie == null)
                && url.StartsWith(TAppSettingsManager.GetValue("OpenPetra.HTTPServer"))
                && !(url.Contains("Login") || (url.Contains("/client"))
                     || (url.Contains("serverMServerAdmin.asmx"))))
            {
                TLogging.Log("GetNewWebClient: Cannot connect to the server without a cookie!  url=" + url);
                throw new Exception("Cannot connect to the server without a cookie!  url=" + url);
            }

            if (OverallCookie != null)
            {
                TLogging.LogAtLevel(1, "SetWebClient: OverallCookie exists - copying!");
                CookieCont = new CookieContainer();
                CookieCont.Add(new Cookie(OverallCookie.Name, OverallCookie.Value, OverallCookie.Path, OverallCookie.Domain));
                TLogging.LogAtLevel(1, "GetNewWebClient: copying cookie\r\n" +
                    "Name: " + OverallCookie.Name + "\r\n" +
                    "Value: " + OverallCookie.Value + "\r\n" +
                    "Path: " + OverallCookie.Path + "\r\n" +
                    "Domain: " + OverallCookie.Domain);
            }

            return new WebClientWithSession(CookieCont);
        }

        private static string WebClientUploadValues(string url, NameValueCollection parameters, int ANumberOfAttempts = 0)
        {
            byte[] buf;

            WebClientWithSession WClient = GetNewWebClient(url);

            try
            {
                buf = WClient.Get(url, parameters);
            }
            catch (System.Net.WebException ex)
            {
                TLogging.LogAtLevel(0, "WebClientUploadValues WebException: " + ex.ToString());

                HttpWebResponse httpWebResponse = (HttpWebResponse)ex.Response;

                if (httpWebResponse != null)
                {
                    if (httpWebResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception(SESSION_ALREADY_CLOSED);
                    }

                    if (httpWebResponse.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        // do not retry if code 500 returns
                        throw;
                    }
                }

                if (ANumberOfAttempts > 0)
                {
                    // sleep for half a second
                    System.Threading.Thread.Sleep(500);
                    return WebClientUploadValues(url, parameters, ANumberOfAttempts - 1);
                }

                throw;
            }

            StoreSessionCookie(WClient.CookieContainer, url);

            if ((buf != null) && (buf.Length > 0))
            {
                return Encoding.ASCII.GetString(buf, 0, buf.Length);
            }

            return String.Empty;
        }

        private static void StoreSessionCookie(CookieContainer AContainer, string AUrl)
        {
            // store the session cookie only if we don't have one
            if (OverallCookie == null)
            {
                foreach (Cookie c in AContainer.GetCookies(new Uri(AUrl)))
                {
                    if (c.Name == "OpenPetraSessionID")
                    {
                        TLogging.LogAtLevel(1, "returned cookie\r\n" +
                            "Name: " + c.Name + "\r\n" +
                            "Value: " + c.Value + "\r\n" +
                            "Path: " + c.Path + "\r\n" +
                            "Domain: " + c.Domain);

                        OverallCookie = new Cookie(c.Name, c.Value, c.Path, c.Domain);
                    }
                }
            }
        }

        /// <summary>
        /// post a request to a website. used for Connectors
        /// </summary>
        public static string PostRequest(string url, NameValueCollection parameters)
        {
            if (TLogging.DebugLevel > 0)
            {
                LogRequest(url, parameters);
            }

            try
            {
                // config parameter value for how many times a connection should be attempted until the web call fails
                return WebClientUploadValues(url, parameters, TAppSettingsManager.GetInt32("HTTPUtils.PostRequests", 10));
            }
            catch (System.Net.WebException e)
            {
                TLogging.Log("Trying to download: ");
                LogRequest(url, parameters);
                TLogging.Log(e.Message);
                TLogging.Log("Error message from server:");
                TLogging.Log(e.ToString());

                if (e.Response != null)
                {
                    StreamReader sr = new StreamReader(e.Response.GetResponseStream());
                    TLogging.Log(sr.ReadToEnd());
                    sr.Close();
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// download a patch or other file from a website;
        /// used for patching the program
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Boolean DownloadFile(string url, string filename)
        {
            WebClientWithSession WClient = GetNewWebClient(url);

            try
            {
                WClient.DownloadFile(url, filename);
                StoreSessionCookie(WClient.CookieContainer, url);
                return true;
            }
            catch (Exception e)
            {
                TLogging.Log(e.Message + " url: " + url + " filename: " + filename);
            }

            return false;
        }
    }
}
