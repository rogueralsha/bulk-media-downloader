using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Threading;
//using System.Xml;
using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using System.Diagnostics;

namespace BulkMediaDownloader.MediaSources
{
    public class MediaSourceEventArgs: EventArgs {
        public String Message;
    }

    public abstract class AMediaSource
    {

        public const String INITIAL_STAGE = "initial";


        protected Uri url;

        protected bool pause_work = false;

        public virtual bool RequiresLogin { get; }
        public string LoginURL { get; protected set; }

        public event PropertyChangedEventHandler PropertyChanged;

        //protected static SuperWebClient TheWebClient = new SuperWebClient();

        public event EventHandler<MediaSourceEventArgs> StatusChanged;

        private int _WebRequestWaitTime;
        public int WebRequestWaitTime {
            get {
                return _WebRequestWaitTime + RandomWaitTime;
            }
            set {
                _WebRequestWaitTime = value;
            }
        }
        public int WebRequestErrorAdditionalWaitTime { get; set; }
        public int WebRequestRetryCount { get; set; }

        public readonly bool Disabled = false;

        public AMediaSource(Uri url)
        {
            this.WebRequestWaitTime = 100;
            this.WebRequestErrorAdditionalWaitTime = 1000;
            this.WebRequestRetryCount = 5;
            this.RequiresLogin = false;
            this.url = url;
        }

        private int _RandomWaitTime = 10;
        /// <summary>
        /// Generates a random additional wait time between 0 and 500 milliseconds
        /// </summary>
        protected int RandomWaitTime {
            get {
                return new Random().Next(0, _RandomWaitTime);
            }
            set {
                _RandomWaitTime = value;
            }
        }

        protected void sendStatus(String message) {
            if (StatusChanged != null) {
                MediaSourceEventArgs args = new MediaSourceEventArgs();
                args.Message = message;
                this.StatusChanged(this, args);
            }
        }

        protected void sendStatus(Exception e) {
            if (StatusChanged != null) {
                StringBuilder message = new StringBuilder();
                while(e!=null) {
                    message.AppendLine(e.Message);
                    message.AppendLine(e.StackTrace);
                    e = e.InnerException;
                }

                MediaSourceEventArgs args = new MediaSourceEventArgs();
                args.Message = message.ToString();
                this.StatusChanged(this, args);
            }
        }


        public virtual string getFolderNameFromURL(Uri url)
        {
            return "";
        }

        public MediaSourceResult GetInitialStage()
        {
            MediaSourceResult output = 
                new MediaSourceResult(this.url,null,this.url, this, MediaResultType.DownloadSource, INITIAL_STAGE);
            return output;
        }

        public MediaSourceResults ProcessDownloadSource(Uri url, Uri referrer, string stage) {
            string page_contents = GetPageContents(url, referrer);
            return ProcessDownloadSourceInternal(url, page_contents, stage);
        }

        abstract protected MediaSourceResults ProcessDownloadSourceInternal(Uri url, String page_contents, String stage);

        protected virtual void SetCustomRequestHeaders(HttpWebRequest req) {}

        protected Uri GetRedirectURL(Uri url, Uri referer = null)
        {
            for (int i = 0; i < WebRequestRetryCount; i++)
            {
                try
                {
                    using (SuperWebClient client = new SuperWebClient()) {
                        return client.GetRedirectURL(url, referer);
                    }
                }
                catch (Exception ex)
                {
                    if (i == WebRequestRetryCount - 1)
                    {
                        throw new WebException("Error while attempting to get redirect for: " + url.ToString(), ex);
                    }
                    System.Threading.Thread.Sleep(WebRequestErrorAdditionalWaitTime);
                }
                finally
                {
                    System.Threading.Thread.Sleep(WebRequestWaitTime);
                }
            }
            return null;
        }

        protected HttpStatusCode GetResponseStatusCode(Uri url, Uri referer = null) {
            for (int i = 0; i < WebRequestRetryCount; i++) {
                try {
                    using (SuperWebClient client = new SuperWebClient()) {
                        return client.GetResponseStatusCode(url, referer);
                    }
                } catch (Exception ex) {
                    if (i == WebRequestRetryCount - 1) {
                        throw new WebException("Error while attempting to get header for: " + url.ToString(), ex);
                    }
                    System.Threading.Thread.Sleep(WebRequestErrorAdditionalWaitTime);
                } finally {
                    System.Threading.Thread.Sleep(WebRequestWaitTime);
                }
            }
            return  HttpStatusCode.InternalServerError; // Shouldn't ever actually hit this line
        }

        protected WebHeaderCollection GetHeaders(Uri url, Uri referer = null) {
            for (int i = 0; i < WebRequestRetryCount; i++) {
                try {
                    using (SuperWebClient client = new SuperWebClient()) {
                        return client.GetHeaders(url, referer);
                    }
                } catch (Exception ex) {
                    if (i == WebRequestRetryCount - 1) {
                        throw new WebException("Error while attempting to get header for: " + url.ToString(), ex);
                    }
                    System.Threading.Thread.Sleep(WebRequestErrorAdditionalWaitTime);
                } finally {
                    System.Threading.Thread.Sleep(WebRequestWaitTime);
                }
            }
            return null;
        }

        protected string GetPageContents(Uri url, Uri referer = null)
        {
            for (int i = 0; i < WebRequestRetryCount; i++)
            {
                try
                {
                    using (SuperWebClient client = new SuperWebClient()) {
                        String data = client.DownloadString(url, referer);
                        Console.Out.WriteLine("Total characters in page data: " + data.Length);
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    if (i == WebRequestRetryCount - 1)
                    {
                        throw new WebException("Error while attempting to get page contents for: " + url.ToString(), ex);
                    }
                    System.Threading.Thread.Sleep(WebRequestErrorAdditionalWaitTime);
                }
                finally
                {
                    System.Threading.Thread.Sleep(WebRequestWaitTime);
                }
            }
            return "";
        }


        protected Uri GenerateFullUrl(Uri root_url, String possible_relative_url) {
            if (possible_relative_url.Contains('#'))
                possible_relative_url = possible_relative_url.Split('#')[0];

            if(Uri.IsWellFormedUriString(possible_relative_url, UriKind.Absolute)) {
                return new Uri(possible_relative_url);
            }
            if(Uri.IsWellFormedUriString(possible_relative_url, UriKind.Relative)) {
                return new Uri(root_url, possible_relative_url);
            }
            if(possible_relative_url.StartsWith("/")) {
                if (Uri.IsWellFormedUriString(possible_relative_url.TrimStart('/'), UriKind.Relative)) {
                    return new Uri(new Uri(root_url.Scheme + "://" +root_url.Host), possible_relative_url);
                }
            }

            throw new Exception("Cannot generate a URL from " + possible_relative_url);
        }

        /// <summary>
        /// General helper function to extract all possible media links from an HTML block. Kind of brute-forcey.
        /// </summary>
        /// <param name="page_contents"></param>
        /// <returns></returns>
        protected List<MediaSourceResult> getImagesAndDirectLinkedMedia(Uri base_url, string page_contents) {
            List<MediaSourceResult> output = new List<MediaSourceResult>();
            HtmlAgilityPack.HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(page_contents);


            // Get all image tags
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//img");
            if (nodes != null) {
                foreach (HtmlNode imageNode in nodes) {
                    try {
                        if (imageNode.Attributes["src"] == null)
                            continue;
                        String image_src = imageNode.Attributes["src"].Value;
                        if (!String.IsNullOrWhiteSpace(image_src)) {
                            Uri image_url = GenerateFullUrl(base_url, image_src);
                            output.Add(new MediaSourceResult(image_url, base_url, this.url, this, MediaResultType.Download));
                        }

                    } catch (Exception e) {
                        Console.Out.WriteLine(e.Message);
                    }
                }
            }

            // Get all media links
            nodes = doc.DocumentNode.SelectNodes("//a");
            if (nodes != null) {
                foreach (HtmlNode imageNode in nodes) {
                    String href;
                    try {
                        if (imageNode.Attributes["href"] == null)
                            continue;
                        href = imageNode.Attributes["href"].Value;

                        if (!String.IsNullOrWhiteSpace(href)) {
                            Uri href_url = GenerateFullUrl(base_url, href);

                            // Check for media file extensions
                            if (isMediaFile(href_url.ToString())) {
                                // Check if the file is actually a nested page
                                WebHeaderCollection headers = GetHeaders(href_url, base_url);
                                if (headers[HttpResponseHeader.ContentType].ToLower().Contains("text/html")) {
                                    // This means that a particular image is actually a nesting page of some sort. Gotta extract it!
                                    output.AddRange(getImagesAndDirectLinkedMedia(href_url, GetPageContents(href_url, base_url)));
                                } else {
                                    output.Add(new MediaSourceResult(href_url, base_url, this.url, this, MediaResultType.Download));

                                }
                                continue;
                            }

                            output.AddRange(getHostedMedia(href_url));

                        }

                    } catch (Exception e) {
                        Console.Out.WriteLine(e.Message);
                    }
                }
            }

            // Get all sources
            //<video class="center-block" style="vertical-align: middle; width: 100%; max-width:1280px;" preload="auto" controls="controls" autoplay loop>
            //< source src = "//s1.webmshare.com/nGMdr.webm" type = "video/webm" />
            nodes = doc.DocumentNode.SelectNodes("//source");
            if (nodes != null) {
                foreach (HtmlNode imageNode in nodes) {
                    String src;
                    try {
                        if (imageNode.Attributes["src"] == null)
                            continue;
                        src = imageNode.Attributes["src"].Value;

                        if (!String.IsNullOrWhiteSpace(src)) {
                            Uri href_url = GenerateFullUrl(base_url, src);

                            // Check for media file extensions
                            output.Add(new MediaSourceResult(href_url, base_url, this.url, this, MediaResultType.Download));
                        }

                    } catch (Exception e) {
                        Console.Out.WriteLine(e.Message);
                    }
                }
            }


            return output;
        }

        protected bool isMediaFile(String path)
        {
            Uri testUri = new Uri(path);
            path = testUri.AbsolutePath;


            foreach (String extension in Properties.Settings.Default.MediaExtensions)
            {
                if (path.ToLower().EndsWith("." + extension.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        protected List<MediaSourceResult> getHostedMedia(Uri url)
        {
            List<MediaSourceResult> output = new List<MediaSourceResult>();

            try {
                AMediaSource source = MediaSourceManager.GetMediaSourceForUrl(url, true);
                output.Add(new MediaSourceResult(url, null, url, source, MediaResultType.DownloadSource, INITIAL_STAGE));
            } catch (UrlNotRecognizedException ex) {
                Debug.WriteLine("Unsupported url: " + url.ToString());
            }

            return output;
        }

    }



}
