using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;

namespace BulkMediaDownloader.ImageSources {
    public abstract class AImageSource : INotifyPropertyChanged {
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

        public AImageSource(Uri url) {
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

        public static void SetCookies(List<CefSharp.Cookie> new_cookies) {
            TheWebClient.SetCookies(new_cookies);
        }

        public virtual string getFolderNameFromURL(Uri url) {
            return "";
        }

        void worker_DoWork(object sender, DoWorkEventArgs e) {
            List<Uri> pages = new List<Uri>();
            Dictionary<Uri, List<Uri>> images = new Dictionary<Uri, List<Uri>>();
            Uri starting_page = new Uri(this.url.ToString());

            if (!Properties.Settings.Default.DetectAdditionalPages) {
                pages.Add(starting_page);
            } else {
                worker.ReportProgress(0, "Getting all pages from " + starting_page);
                pages = GetPages(starting_page, GetPageContents(starting_page));
                if (pages.Count == 0) {
                    worker.ReportProgress(0, "No additional pages found, using starting page");
                    pages.Add(starting_page);
                }
            }


            for (int i = 0; i < pages.Count; i++) {
                Uri page = pages[i];
                images.Add(page, new List<Uri>());
                double divided = ((double)i) / ((double)pages.Count);
                int progress = (int)Math.Ceiling(divided * 100);

                worker.ReportProgress(progress, "Getting items from page " + page.ToString() + " (" + (i + 1) + "/" + pages.Count + ")");
                List<Uri> page_images = GetImagesFromPage(page, GetPageContents(page));
                worker.ReportProgress(progress, page_images.Count + " items found");
                foreach (Uri image in page_images) {
                    images[page].Add(image);
                }
            }

            worker.ReportProgress(100, "Done fetching items, total " + images.Count);

            e.Result = images;
        }


        abstract protected List<Uri> GetPages(Uri page_url, String page_contents);
        abstract protected List<Uri> GetImagesFromPage(Uri page_url, String page_contents);

        public void Start() {
            if (worker.IsBusy) {
                pause_work = false;
            } else {
                worker.RunWorkerAsync();
            }
        }

        public void Pause() {
            pause_work = true;
        }

        public void Cancel() {
            worker.CancelAsync();
        }

        protected void NotifyPropertyChanged(String propertyName = "") {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        protected void IfPausedWaitUntilUnPaused() {
            while (pause_work) {
                Thread.Sleep(500);
            }
        }

        protected virtual void SetCustomRequestHeaders(HttpWebRequest req) {
        }

        protected Uri GetRedirectURL(Uri url, Uri referer = null) {
            for (int i = 0; i < WebRequestRetryCount; i++) {
                try {
                    return TheWebClient.GetRedirectURL(url, referer);
                } catch (Exception ex) {
                    if (i == WebRequestRetryCount - 1) {
                        throw new WebException("Error while attempting to get redirect for: " + url.ToString(), ex);
                    }
                    System.Threading.Thread.Sleep(WebRequestErrorAdditionalWaitTime);
                } finally {
                    System.Threading.Thread.Sleep(WebRequestWaitTime);
                }
            }
            return null;
        }

        protected string GetPageContents(Uri url, Uri referer = null) {
            for (int i = 0; i < WebRequestRetryCount; i++) {
                try {
                    String data = TheWebClient.DownloadString(url, referer);
                    Console.Out.WriteLine("Total characters in page data: " + data.Length);
                    return data;
                } catch (Exception ex) {
                    if (i == WebRequestRetryCount -1) {
                        throw new WebException("Error while attempting to get page contents for: " + url.ToString(), ex);
                    }
                    System.Threading.Thread.Sleep(WebRequestErrorAdditionalWaitTime);
                } finally {
                    System.Threading.Thread.Sleep(WebRequestWaitTime);
                }
            }
            return "";
        }



    }



}
