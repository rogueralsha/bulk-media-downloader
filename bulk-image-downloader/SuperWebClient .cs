using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace BulkMediaDownloader {
    public class SuperWebClient : WebClient {
        private static readonly CookieContainer m_container = new CookieContainer();
        private WebRequest last_request = null;

        public bool SimpleHeaders = false;

        public SuperWebClient() {
        }

        public void SetCookies(List<CefSharp.Cookie> new_cookies) {
            foreach (CefSharp.Cookie cookie in new_cookies) {
                if (cookie.Name.StartsWith("_"))
                    continue;

                Cookie new_cookie = new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
                if (cookie.Expires.HasValue) {
                    new_cookie.Expires = cookie.Expires.Value;
                }
                new_cookie.HttpOnly = cookie.HttpOnly;

                m_container.Add(new_cookie);
            }
        }

        public static bool HasValidCookiesForDomain(Uri uri) {
            CookieCollection cc = m_container.GetCookies(uri);
            if (cc.Count > 0) {
                return true;
            } else {
                return false;
            }
        }

        public void SetReferer(Uri referer) {
            this.referer_override = referer.AbsoluteUri;
        }

        string referer_override = null;
        public string DownloadString(Uri url, Uri referer) {
            if (referer != null) {
                referer_override = referer.AbsoluteUri;
            }
            String output = DownloadString(url);
            return output;
        }

        protected override WebRequest GetWebRequest(Uri address) {
            try {
                WebRequest request = base.GetWebRequest(address);
                HttpWebRequest webRequest = request as HttpWebRequest;

                if(!SimpleHeaders) {

                // Mimicking the Chrome browser
                webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36";
                //webRequest.Headers.Add("Accept-Encoding", "gzip, deflate, sdch");
                webRequest.Headers.Add("Accept-Charset", "UTF-8");
                webRequest.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                webRequest.Headers.Add("Upgrade-Insecure-Requests", "1");

                }

                if (webRequest != null) {
                    webRequest.CookieContainer = m_container;
                }

                if (referer_override != null) {
                    webRequest.Referer = referer_override;
                }

                last_request = request;
                return request;
            } finally {
                referer_override = null;
            }

        }



        protected override WebResponse GetWebResponse(WebRequest request) {
            string headers = request.Headers.ToString();
            HttpWebRequest hwr = (HttpWebRequest)request;
            CookieCollection cc = hwr.CookieContainer.GetCookies(request.RequestUri);

            return base.GetWebResponse(request);
        }

        public WebHeaderCollection  GetHeaders(Uri address, Uri referrer = null) {
            HttpWebRequest req = (HttpWebRequest)this.GetWebRequest(address);

            req.Method = "HEAD";
            req.AllowAutoRedirect = false;

            if (referrer != null) {
                req.Referer = referrer.AbsoluteUri;
            }

            using (HttpWebResponse res = (HttpWebResponse)this.GetWebResponse(req)) {
                return res.Headers;
            }
        }

        public Uri GetRedirectURL(Uri address, Uri referrer = null) {
            HttpWebRequest req = (HttpWebRequest)this.GetWebRequest(address);
            req.AllowAutoRedirect = false;

            if (referrer != null) {
                req.Referer = referrer.AbsoluteUri;
            }

            string image_url;

            using (HttpWebResponse res = (HttpWebResponse)this.GetWebResponse(req)) {
                image_url = res.GetResponseHeader("Location");
                res.Close();
            }

            if (String.IsNullOrWhiteSpace(image_url)) {
                throw new Exception("No redirect returned from " + address.ToString());
            }

            return new Uri(image_url);
        }

        protected override void Dispose(bool disposing) {
            this.last_request = null;
            base.Dispose(disposing);
        }
    }

}
