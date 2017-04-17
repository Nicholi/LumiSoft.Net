using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;

namespace LumiSoft.Net.WebDav.Client
{
    /// <summary>
    /// Implements WebDav client. Defined in RFC 4918.
    /// </summary>
    public class WebDav_Client
    {
        private NetworkCredential m_pCredentials = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public WebDav_Client()
        {
        }


        #region method PropFind

        /// <summary>
        /// Executes PROPFIND method.
        /// </summary>
        /// <param name="requestUri">Request URI.</param>
        /// <param name="propertyNames">Properties to get. Value null means property names listing.</param>
        /// <param name="depth">Maximum depth inside collections to get.</param>
        /// <returns>Returns server returned responses.</returns>
        public WebDav_MultiStatus PropFind(string requestUri,string[] propertyNames,int depth)
        {
            if(requestUri == null){
                throw new ArgumentNullException("requestUri");
            }

            StringBuilder requestContentString = new StringBuilder();
            requestContentString.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n");
            requestContentString.Append("<propfind xmlns=\"DAV:\">\r\n");
            requestContentString.Append("<prop>\r\n");
            if(propertyNames == null || propertyNames.Length == 0){
                requestContentString.Append("   <propname/>\r\n");
            }
            else{
                foreach(string propertyName in propertyNames){
                    requestContentString.Append("<" + propertyName + "/>");
                }
            }            
            requestContentString.Append("</prop>\r\n");
            requestContentString.Append("</propfind>\r\n");

            byte[] requestContent = Encoding.UTF8.GetBytes(requestContentString.ToString());

            Dictionary<String, String> headers = null;
            if (depth > -1)
            {
                headers = new Dictionary<String, String>()
                    {
                        { "Depth", depth.ToString() },
                    };
            }

            WebDav_MultiStatus status = null;
            Helpers.SendHttpRequest(requestUri, "PROPFIND", "application/xml", m_pCredentials, headers, 
                contentBytes: requestContent,
                modifyRequestFunc: (HttpWebRequest request) =>
                    {
#if !NETSTANDARD
                        request.ContentLength = requestContent.Length;
#endif
                    },
                handleResponseFunc: (WebResponse response) =>
                    {
                        status = WebDav_MultiStatus.Parse(response.GetResponseStream());
                    });

            return status;
        }

        #endregion

        #region method PropPatch

        // public void PropPatch()
        // {
        // }

        #endregion

        #region method MkCol

        /// <summary>
        /// Creates new collection to the specified path.
        /// </summary>
        /// <param name="uri">Target collection URI.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> null reference.</exception>
        public void MkCol(string uri)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            Helpers.SendHttpRequest(uri, "MKCOL", null, m_pCredentials, contentString: null);
        }

        #endregion

        #region method Get
        
        /// <summary>
        /// Gets the specified resource stream.
        /// </summary>
        /// <param name="uri">Target resource URI.</param>
        /// <param name="contentSize">Returns resource size in bytes.</param>
        /// <returns>Retruns resource stream.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> is null reference.</exception>
        public Stream Get(string uri,out long contentSize)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            long innerContentSize = 0;
            Stream sr = null;
            Helpers.SendHttpRequest(uri, "GET", null, m_pCredentials, 
                contentString: null,
                handleResponseFunc: (WebResponse response) =>
                    {
                        using(var responseStream = response.GetResponseStream())
                        {
                            sr = new MemoryStream();
                            responseStream.CopyTo(sr);
                            innerContentSize = response.ContentLength;
                        }
                    });

            contentSize = innerContentSize;
            return sr;
        }

        #endregion

        #region method Head

        // public void Head()
        // {
        // }

        #endregion

        #region method Post

        // public void Post()
        // {
        // }

        #endregion

        #region method Delete

        /// <summary>
        /// Deletes specified resource.
        /// </summary>
        /// <param name="uri">Target URI. For example: htt://server/test.txt .</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> is null reference.</exception>
        public void Delete(string uri)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            Helpers.SendHttpRequest(uri, "DELETE", null, m_pCredentials, contentString: null);
        }

        #endregion

        #region method Put

        /// <summary>
        /// Creates specified resource to the specified location.
        /// </summary>
        /// <param name="targetUri">Target URI. For example: htt://server/test.txt .</param>
        /// <param name="stream">Stream which data to upload.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>targetUri</b> or <b>stream</b> is null reference.</exception>
        public void Put(string targetUri,Stream stream)
        {
            if(targetUri == null){
                throw new ArgumentNullException("targetUri");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            // Work around, to casuse authentication, otherwise we may not use AllowWriteStreamBuffering = false later.
            // All this because ms is so lazy, tries to write all data to memory, instead switching to temp file if bigger 
            // data sent.
            try{
                Helpers.SendHttpRequest(targetUri, "HEAD", null, m_pCredentials, contentString: null);
            }
            catch{
            }

            Helpers.SendHttpRequest(targetUri, "PUT", "application/octet-stream", m_pCredentials, 
                contentStream: stream,
                modifyRequestFunc: (HttpWebRequest request) =>
                    {
#if !NETSTANDARD
                        request.PreAuthenticate = true;
                        request.AllowWriteStreamBuffering = false;
                        if (stream.CanSeek)
                        {                
                            request.ContentLength = (stream.Length - stream.Position);
                        }
#endif
                    });
        }

        #endregion

        #region method Copy

        /// <summary>
        /// Copies source URI resource to the target URI.
        /// </summary>
        /// <param name="sourceUri">Source URI.</param>
        /// <param name="targetUri">Target URI.</param>
        /// <param name="depth">If source is collection, then depth specified how many nested levels will be copied.</param>
        /// <param name="overwrite">If true and target resource already exists, it will be over written. 
        /// If false and target resource exists, exception is thrown.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sourceUri</b> or <b>targetUri</b> is null reference.</exception>
        public void Copy(string sourceUri,string targetUri,int depth,bool overwrite)
        {
            if(sourceUri == null){
                throw new ArgumentNullException(sourceUri);
            }
            if(targetUri == null){
                throw new ArgumentNullException(targetUri);
            }

            var headers = new Dictionary<String, String>()
                {
                    { "Destination", targetUri },
                    { "Overwrite", (overwrite ? "T" : "F") },
                };
            if (depth > -1)
            {
                headers.Add("Depth", depth.ToString());
            }
            Helpers.SendHttpRequest(sourceUri, "COPY", null, m_pCredentials, headers, contentString: null);
        }

        #endregion

        #region method Move

        /// <summary>
        /// Moves source URI resource to the target URI.
        /// </summary>
        /// <param name="sourceUri">Source URI.</param>
        /// <param name="targetUri">Target URI.</param>
        /// <param name="depth">If source is collection, then depth specified how many nested levels will be copied.</param>
        /// <param name="overwrite">If true and target resource already exists, it will be over written. 
        /// If false and target resource exists, exception is thrown.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sourceUri</b> or <b>targetUri</b> is null reference.</exception>
        public void Move(string sourceUri,string targetUri,int depth,bool overwrite)
        {
            if(sourceUri == null){
                throw new ArgumentNullException(sourceUri);
            }
            if(targetUri == null){
                throw new ArgumentNullException(targetUri);
            }

            var headers = new Dictionary<String, String>()
                {
                    { "Destination", targetUri },
                    { "Overwrite", (overwrite ? "T" : "F") },
                };
            if (depth > -1)
            {
                headers.Add("Depth", depth.ToString());
            }
            Helpers.SendHttpRequest(sourceUri, "MOVE", null, m_pCredentials, headers, contentString: null);
        }

        #endregion

        #region method Lock

        // public void Lock()
        // {
        // }

        #endregion

        #region method Unlock

        // public void Unlock()
        // {
        // }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets credentials.
        /// </summary>
        public NetworkCredential Credentials
        {
            get{ return m_pCredentials; }

            set{ m_pCredentials = value; }
        }

        #endregion

    }
}
