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

namespace BulkMediaDownloader.ImageSources
{
    public abstract class AImageSource : INotifyPropertyChanged
    {
        protected Uri url;

        public BackgroundWorker worker;

        protected bool pause_work = false;

        public virtual bool RequiresLogin { get; }
        public string LoginURL { get; protected set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected static SuperWebClient TheWebClient = new SuperWebClient();

        public AImageSource(Uri url)
        {
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
            List<Uri> pages = new List<Uri>();
            Dictionary<Uri, List<Uri>> images = new Dictionary<Uri, List<Uri>>();
            Uri starting_page = new Uri(this.url.ToString());

            if (!Properties.Settings.Default.DetectAdditionalPages)
            {
                pages.Add(starting_page);
            }
            else {
                worker.ReportProgress(0, "Getting all pages from " + starting_page);
                pages = GetPages(GetPageContents(starting_page));
                if (pages.Count == 0)
                {
                    worker.ReportProgress(0, "No additional pages found, using starting page");
                    pages.Add(starting_page);
                }
            }


            for(int i = 0; i < pages.Count; i++) {
                Uri page = pages[i];
                images.Add(page, new List<Uri>());
                System.Threading.Thread.Sleep(100);
                double divided = ((double)i) / ((double)pages.Count);
                int progress = (int)Math.Ceiling(divided * 100);

                worker.ReportProgress(progress,"Getting items from page " + page.ToString() + " (" + (i+1) + "/" + pages.Count + ")");
                List<Uri> page_images = GetImagesFromPage(GetPageContents(page));
                worker.ReportProgress(progress, page_images.Count + " items found");
                foreach (Uri image in page_images)
                {
                    images[page].Add(image);
                }
            }

            worker.ReportProgress(100, "Done fetching items, total " + images.Count);

            e.Result = images;
        }


        abstract protected List<Uri> GetPages(String page_contents);
        abstract protected List<Uri> GetImagesFromPage(String page_contents);

        public void Start()
        {
            if (worker.IsBusy)
            {
                pause_work = false;
            }
            else {
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

        protected string GetPageContents(Uri url)
        {
            //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            //req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            //req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36";
            //req.Headers.Add("Accept-Encoding", "gzip, deflate");
            //req.Headers.Add("Accept-Charset", "UTF-8");
            //req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
            //req.Headers.Add("Upgrade-Insecure-Requests", "1");
            //SetCustomRequestHeaders(req);
            //for (int i = 0; i < 5; i++)
            //{
            //    try
            //    {
            //        using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
            //        {
            //            if (response.StatusCode == HttpStatusCode.OK)
            //            {
            //                using (Stream receiveStream = response.GetResponseStream())
            //                {
            //                    using (MemoryStream memStream = new MemoryStream())
            //                    {
            //                        receiveStream.CopyTo(memStream);
            //                        //if (response.CharacterSet == null)
            //                        //{
            //                        //    readStream = new StreamReader(receiveStream);
            //                        //}
            //                        //else
            //                        //{
            //                        //    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
            //                        //}

            //                        memStream.Seek(0, SeekOrigin.Begin);
            //                        byte[] bytes = memStream.ToArray();

            //                        string temp_file = System.IO.Path.GetTempFileName();

            //                        File.WriteAllBytes(temp_file, memStream.ToArray());
            //                        StringBuilder output = new StringBuilder();
            //                        string test = "";
            //                        foreach(String line in File.ReadAllLines(temp_file))
            //                        {
            //                            // Memory nightmare? Yes, but a necessary one. 
            //                            // This tests for line appending errors, something enountered with data from Deviant Art's server.
            //                            int starting_length = test.Length;
            //                            test += line;
            //                            if((test.Length-starting_length) == line.Length)
            //                            {
            //                                output.AppendLine(line.Replace("\r", "").Replace("\n", "").Replace("\0", ""));
            //                            } else
            //                            {
            //                                Console.Out.WriteLine("Discarding line:");
            //                                Console.Out.WriteLine(line);
            //                            }
            //                        }
            //                        String data = output.ToString();
            //                        File.Delete(temp_file);
            //                        Console.Out.WriteLine(data.Length);
            //                        return test;
            //                    }
            //                }

            //            }
            //            else
            //            {
            //                throw new Exception("Status code " + response.StatusCode.ToString());
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        if (i == 4)
            //        {
            //            throw new Exception("Error while attempting to get page contents for: " + url.ToString(), ex);
            //        }
            //    }
            //}

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    String data = TheWebClient.DownloadString(url);
                    Console.Out.WriteLine("Total characters in page data: " + data.Length);
                    return data;
                }
                catch (Exception ex)
                {
                    if (i == 4)
                    {
                        throw new Exception("Error while attempting to get page contents for: " + url.ToString(), ex);
                    }
                }
            }
            return "";
        }



    }



}
