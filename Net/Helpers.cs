using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LumiSoft.Net
{
    public static class Helpers
    {
        // for now simply return UTF8
        public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
        // probably the better choice for reproducible behavior
        public static readonly StringComparison DEFAULT_IGNORE_CASE_COMPARISON = StringComparison.OrdinalIgnoreCase;

        public static Encoding GetDefaultEncoding()
        {
#if NETSTANDARD
            return DEFAULT_ENCODING;
#else
            return Encoding.Default;
#endif
        }

        public static StringComparison GetDefaultIgnoreCaseComparison()
        {
#if NETSTANDARD
            return DEFAULT_IGNORE_CASE_COMPARISON;
#else
            return StringComparison.InvariantCultureIgnoreCase;
#endif
        }

        private static HttpWebRequest SetupHttpWebRequest(String url, String method, String contentType = null, 
            NetworkCredential networkCredential = null,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders = null,
            Action<HttpWebRequest> modifyRequestFunc = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;

            if (contentType != null)
            {
                request.ContentType = contentType;
            }

            if (networkCredential != null)
            {
                request.Credentials = networkCredential;
            }

            if (additionalHeaders != null)
            {
                foreach (var pair in additionalHeaders)
                {
#if NETSTANDARD
                    request.Headers[pair.Key] = pair.Value;
#else
                    request.Headers.Add(pair.Key, pair.Value);
#endif
                }
            }

            if (modifyRequestFunc != null)
            {
                modifyRequestFunc(request);
            }

            return request;
        }

        public static void SendHttpRequest(String url, String method, String contentType = null, 
            NetworkCredential networkCredential = null,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders = null, 
            String contentString = null,
            Action<HttpWebRequest> modifyRequestFunc = null,
            Action<WebResponse> handleResponseFunc = null)
        {
            byte[] contentBytes = null;
            if (contentString != null)
            {
                contentBytes = GetDefaultEncoding().GetBytes(contentString);
            }

            SendHttpRequest(url, method, contentType, networkCredential, additionalHeaders,
                contentBytes,
                modifyRequestFunc, handleResponseFunc);
        }

        
        public static void SendHttpRequest(String url, String method, String contentType = null, 
            NetworkCredential networkCredential = null,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders = null,
            byte[] contentBytes = null,
            Action<HttpWebRequest> modifyRequestFunc = null,
            Action<WebResponse> handleResponseFunc = null)
        {
            HttpWebRequest request = SetupHttpWebRequest(url, method, contentType, networkCredential, additionalHeaders, modifyRequestFunc);

            if (contentBytes != null)
            {
                var requestStreamTask = Task.Run(async () =>
                    {
                        using (var requestStream = await request.GetRequestStreamAsync())
                        {
                            requestStream.Write(contentBytes, 0, contentBytes.Length);
                            using (var response = await request.GetResponseAsync())
                            {
                                if (handleResponseFunc != null)
                                {
                                    handleResponseFunc(response);
                                }
                            }
                        }
                    });
                requestStreamTask.GetAwaiter().GetResult();
            }
            else
            {
                var responseTask = Task.Run(() => request.GetResponseAsync());
                using (var response = responseTask.GetAwaiter().GetResult())
                {
                    if (handleResponseFunc != null)
                    {
                        handleResponseFunc(response);
                    }
                }
            }
        }

        public static void SendHttpRequest(String url, String method, String contentType = null, 
            NetworkCredential networkCredential = null,
            IEnumerable<KeyValuePair<String, String>> additionalHeaders = null,
            Stream contentStream = null,
            Action<HttpWebRequest> modifyRequestFunc = null,
            Action<WebResponse> handleResponseFunc = null)
        {
            HttpWebRequest request = SetupHttpWebRequest(url, method, contentType, networkCredential, additionalHeaders, modifyRequestFunc);

            if (contentStream != null)
            {
                var requestStreamTask = Task.Run(async () =>
                    {
                        using (var requestStream = await request.GetRequestStreamAsync())
                        {
                            Net_Utils.StreamCopy(contentStream, requestStream, 32000);
                            using (var response = await request.GetResponseAsync())
                            {
                                if (handleResponseFunc != null)
                                {
                                    handleResponseFunc(response);
                                }
                            }
                        }
                    });
                requestStreamTask.GetAwaiter().GetResult();
            }
            else
            {
                var responseTask = Task.Run(() => request.GetResponseAsync());
                using (var response = responseTask.GetAwaiter().GetResult())
                {
                    if (handleResponseFunc != null)
                    {
                        handleResponseFunc(response);
                    }
                }
            }
        }
        
        public static IPAddress[] GetHostAddresses(String host)
        {
#if NETSTANDARD
            var dnsTask = Task.Run(() => Dns.GetHostAddressesAsync(host));
            IPAddress[] hosts = dnsTask.GetAwaiter().GetResult();
#else
            IPAddress[] hosts = System.Net.Dns.GetHostAddresses(host);
#endif
            return hosts;
        }

        public static IPHostEntry GetHostEntry(String host)
        {
#if NETSTANDARD
            var dnsTask = Task.Run(() => Dns.GetHostEntryAsync(host));
            IPHostEntry hostEntry = dnsTask.GetAwaiter().GetResult();
#else
            IPHostEntry hostEntry = System.Net.Dns.GetHostEntry(host);
#endif
            return hostEntry;
        }


        #region CloseOrDispose Methods

        public static void CloseOrDispose(this Socket socket)
        {
            if (socket == null)
            {
                return;
            }

#if NETSTANDARD
            socket.Dispose();
#else
            socket.Close();
#endif
        }

        public static void CloseOrDispose(this Stream stream)
        {
            if (stream == null)
            {
                return;
            }

#if NETSTANDARD
            stream.Dispose();
#else
            stream.Close();
#endif
        }

        public static void CloseOrDispose(this WaitHandle handle)
        {
            if (handle == null)
            {
                return;
            }

#if NETSTANDARD
            handle.Dispose();
#else
            handle.Close();
#endif
        }

        #endregion
    }
}
