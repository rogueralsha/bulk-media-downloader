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

namespace BulkMediaDownloader.MediaSources
{
    public abstract class AMediaSource : INotifyPropertyChanged
    {
        protected Uri url;

        public BackgroundWorker worker;

        protected bool pause_work = false;

        public virtual bool RequiresLogin { get; }
        public string LoginURL { get; protected set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected static SuperWebClient TheWebClient = new SuperWebClient();

        public int WebRequestWaitTime { get; set; }
        public int WebRequestErrorAdditionalWaitTime { get; set; }
        public int WebRequestRetryCount { get; set; }

        public AMediaSource(Uri url)
        {
            this.WebRequestWaitTime = 100;
            this.WebRequestErrorAdditionalWaitTime = 1000;
            this.WebRequestRetryCount = 5;
            this.RequiresLogin = false;
            this.url = url;
            worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        public static void SetCookies(List<CefSharp.Cookie> new_cookies)
        {
            TheWebClient.SetCookies(new_cookies);
        }

        public virtual string getFolderNameFromURL(Uri url)
        {
            return "";
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            UniqueQueue<Uri> pages = new UniqueQueue<Uri>();
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            Uri starting_page = new Uri(this.url.ToString());

            if (!Properties.Settings.Default.DetectAdditionalPages)
            {
                pages.Enqueue(starting_page);
            }
            else
            {
                worker.ReportProgress(0, "Getting all pages from " + starting_page);
                pages = new UniqueQueue<Uri>();
                pages.EnqueueAll(GetPages(starting_page, GetPageContents(starting_page)));

                if (pages.Count == 0)
                    worker.ReportProgress(100, "No Pages founds");
            }

            double page_count = pages.Count, i = 0;
            while(pages.Count > 0) {
                Uri page = pages.Dequeue();

                double divided = i / page_count;
                int progress = (int)Math.Ceiling(divided * 100);

                worker.ReportProgress(progress, "Getting items from page " + page.ToString() + " (" + (i + 1) + "/" + page_count + ")");
                HashSet<MediaSourceResult> page_images = GetMediaFromPage(page, GetPageContents(page));
                worker.ReportProgress(progress, page_images.Count + " items found");
                foreach (MediaSourceResult media in page_images)
                {
                    output.Add(media);
                }
                i++;
            }

            worker.ReportProgress(100, "Done fetching items, total images " + output.Count);

            e.Result = output;
        }


        abstract protected HashSet<Uri> GetPages(Uri page_url, String page_contents);
        abstract protected HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url, String page_contents);

        public void Start()
        {
            if (worker.IsBusy)
            {
                pause_work = false;
            }
            else
            {
                worker.RunWorkerAsync();
            }
        }

        public void Pause()
        {
            pause_work = true;
        }

        public void Cancel()
        {
            worker.CancelAsync();
        }

        protected void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        protected void IfPausedWaitUntilUnPaused()
        {
            while (pause_work)
            {
                Thread.Sleep(500);
            }
        }

        protected virtual void SetCustomRequestHeaders(HttpWebRequest req)
        {
        }

        protected Uri GetRedirectURL(Uri url, Uri referer = null)
        {
            for (int i = 0; i < WebRequestRetryCount; i++)
            {
                try
                {
                    return TheWebClient.GetRedirectURL(url, referer);
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
                    return TheWebClient.GetResponseStatusCode(url, referer);
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
                    return TheWebClient.GetHeaders(url, referer);
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
                    String data = TheWebClient.DownloadString(url, referer);
                    Console.Out.WriteLine("Total characters in page data: " + data.Length);
                    return data;
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
        protected List<Uri> getImagesAndDirectLinkedMedia(Uri base_url, string page_contents)
        {
            List<Uri> output = new List<Uri>();
            HtmlAgilityPack.HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(page_contents);


            // Get all image tags
            foreach (HtmlNode imageNode in doc.DocumentNode.SelectNodes("//img"))
            {
                try
                {
                    if (imageNode.Attributes["src"] == null)
                        continue;
                    String image_src = imageNode.Attributes["src"].Value;
                    if (!String.IsNullOrWhiteSpace(image_src))
                    {
                        Uri image_url = GenerateFullUrl(base_url,image_src);
                        output.Add(image_url);
                    }

                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }
            }


            // Get all media links
            foreach (HtmlNode imageNode in doc.DocumentNode.SelectNodes("//a")) {
                String href;
                try
                {
                    if (imageNode.Attributes["href"] == null)
                        continue;
                    href = imageNode.Attributes["href"].Value;

                    if (!String.IsNullOrWhiteSpace(href))
                    {
                        Uri href_url = GenerateFullUrl(base_url, href);

                        // Check for media file extensions
                        if (isMediaFile(href_url.ToString()))
                        {
                            output.Add(href_url);
                            continue;
                        }

                        output.AddRange(getHostedMedia(href_url));

                    }

                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }
            }

            return output;
        }

        protected bool isMediaFile(String path)
        {
            foreach (String extension in Properties.Settings.Default.MediaExtensions)
            {
                if (path.Contains("." + extension))
                {
                    return true;
                }
            }
            return false;
        }

        private static Regex gfycat_regex = new Regex(@"(.+gfycat.com/.+)");
        protected List<Uri> getHostedMedia(Uri url)
        {
            List<Uri> output = new List<Uri>();

            // Check for known media hosting sites

            if (ImgurMediaSource.SupportsUrl(url))
            {
                throw new NotSupportedException();
            }

            if (gfycat_regex.IsMatch(url.ToString()))
            {
                String contents = GetPageContents(url);

                //<source id="webmSource" src="https://giant.gfycat.com/YawningBlaringGuanaco.webm" type="video/webm">
                //<source id="mp4Source" src="https://fat.gfycat.com/YawningBlaringGuanaco.mp4" type="video/mp4">
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(contents);
                HtmlNode node = doc.GetElementbyId("webmSource");
                try
                {
                    if (node != null && node.Attributes["src"] != null && !String.IsNullOrWhiteSpace(node.Attributes["src"].Value))
                        output.Add(new Uri(node.Attributes["src"].Value));
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }
                node = doc.GetElementbyId("mp4Source");
                try
                {
                    if (node != null && node.Attributes["src"] != null && !String.IsNullOrWhiteSpace(node.Attributes["src"].Value))
                        output.Add(new Uri(node.Attributes["src"].Value));
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }


            }


            return output;
        }
    }



}
