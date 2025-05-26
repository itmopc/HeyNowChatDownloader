using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

namespace EmailDownloader
{
    public class DownloadPageAsyncResult
    {
        public string result { get; set; }
        public string reasonPhrase { get; set; }
        public HttpResponseHeaders headers { get; set; }
        public HttpStatusCode code { get; set; }
        public string errorMessage { get; set; }
    }
    public class HTTPRequestService
    {
        public static string WebApiPostMethod(string postData, string URL)
        {
            string responseFromServer = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent =
                              "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";
            request.Accept = "/";
            request.UseDefaultCredentials = true;
            request.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return responseFromServer;
        }

        public static string webGetMethod(string URL)
        {
            string jsonString = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "GET";
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";
            request.Accept = "/";
            request.UseDefaultCredentials = true;
            request.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            request.ContentType = "application/x-www-form-urlencoded";

            WebResponse response = request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            jsonString = sr.ReadToEnd();
            sr.Close();
            return jsonString;
        }

        public async Task<HttpResponseMessage> SendRequestAsync(string adaptiveUri, string json, string token, bool isPost)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                if (!string.IsNullOrEmpty(token))
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage responseMessage = null;
                try
                {
                    if (isPost)
                    {
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        responseMessage = await httpClient.PostAsync(adaptiveUri, content);
                    }
                    else
                        responseMessage = await httpClient.GetAsync(adaptiveUri);
                }
                catch (Exception ex)
                {
                    if (responseMessage == null)
                    {
                        responseMessage = new HttpResponseMessage();
                    }
                    responseMessage.StatusCode = HttpStatusCode.InternalServerError;

                    string errorMeassge = "";

                    if (ex.InnerException != null)
                    {
                        if ((ex.InnerException).Message != null)
                        {
                            errorMeassge += " -" + (ex.InnerException).Message;
                        }

                        if (((ex.InnerException).InnerException) != null)
                        {
                            errorMeassge += " -" + ((ex.InnerException).InnerException).Message;
                        }
                    }
                    responseMessage.ReasonPhrase = string.Format("RestHttpClient.SendRequest failed: {0}", errorMeassge);
                }

                return responseMessage;
            }
        }

        public async Task<DownloadPageAsyncResult> DownloadPageAsync(HttpResponseMessage response)
        {
            var result = new DownloadPageAsyncResult();
            try
            {
                using (HttpContent content = response.Content)
                {
                    // need these to return to Form for display
                    string resultString = await content.ReadAsStringAsync();
                    string reasonPhrase = response.ReasonPhrase;
                    HttpResponseHeaders headers = response.Headers;
                    HttpStatusCode code = response.StatusCode;

                    result.result = resultString;
                    result.reasonPhrase = reasonPhrase;
                    result.headers = headers;
                    result.code = code;
                }
            }
            catch (Exception ex)
            {
                // need to return ex.message for display.
                string eM = ex.Message;

                if (response != null)
                {
                    if (response.ReasonPhrase != null)
                    {
                        eM += " "+ response.ReasonPhrase; 
                    }
                }
                result.errorMessage = eM;


            }
            return result;
        }
    }
}
