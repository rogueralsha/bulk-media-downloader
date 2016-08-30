using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BulkMediaDownloader.Download {



    [Serializable]
    public class Downloadable : ADownloadable, INotifyPropertyChanged {
        private SuperWebClient client;

        [XmlIgnore]
        [NotMapped]
        public int MaxAttempts { get; set; }

        private string _OverrideFileName = null;

        private string OriginalFileName = null;

        [XmlIgnore]
        [NotMapped]
        public string FileName {
            get {
                if (!String.IsNullOrWhiteSpace(_OverrideFileName))
                    return _OverrideFileName;

                UriBuilder builder = new UriBuilder(this.URL);
                builder.Query = "";

                StringBuilder path = new StringBuilder(Uri.UnescapeDataString(builder.Uri.ToString()));
                foreach (char c in System.IO.Path.GetInvalidPathChars()) {
                    path.Replace(c, '_');
                }
                path.Replace(':', '_');
                string file = Path.GetFileName(path.ToString());
                if (file.Length > 255) {
                    int length = 255 - Path.GetExtension(file).Length;
                    file = Path.GetFileNameWithoutExtension(file).Substring(0, length) + Path.GetExtension(file);
                }
                return file;
            }
        }

        [XmlIgnore]
        [NotMapped]
        public override bool RequiresLogin {
            get {
                return false;
            }
        }


        [XmlIgnore]
        [NotMapped]
        public Uri RefererURL { get; protected set; }
        public String RefererURLString {
            get {
                if (RefererURL == null)
                    return null;
                else
                    return RefererURL.ToString();
            }
            set {
                if (value == null)
                    RefererURL = null;
                else
                RefererURL = new Uri(value);
            }
        }
        //public Object Data { get; protected set; }

        public bool SimpleHeaders { get; set; }


        private DateTime download_start_time;

        [XmlIgnore]
        [NotMapped]
        public Uri Site { get; set; }
        public String SiteString {
            get {
                return Site.ToString();
            }
            set {
                Site = new Uri(value);
            }
        }

        public String Source {
            get {
                return Site.ToString();
            }
            set {
                Site = new Uri(value);
            }
        }


        [XmlIgnore]
        [NotMapped]
        public int StartDelay = 1000;

        #region Properties




        private long _length = -1;
        public long Length {
            get {
                return _length;
            }

            set {
                _length = value;
                NotifyPropertyChanged("Length");
                NotifyPropertyChanged("Progress");
                NotifyPropertyChanged("Speed");
                NotifyPropertyChanged("ProgressText");
            }
        }

        private long _downloaded_length = -1;
        [XmlIgnore]
        [NotMapped]
        public long DownloadedLength {
            get {
                return _downloaded_length;
            }
            protected set {
                _downloaded_length = value;
                NotifyPropertyChanged("DownloadedLength");
                NotifyPropertyChanged("Progress");
                NotifyPropertyChanged("Speed");
                NotifyPropertyChanged("ProgressText");
            }
        }


        [XmlIgnore]
        [NotMapped]
        public override int Progress {
            get {
                if (State == DownloadState.Complete || State == DownloadState.Skipped) {
                    return 100;
                }

                if (Length <= 0) {
                    return 0;
                }
                double output = 0;
                output += DownloadedLength;
                output /= Length;
                output *= 100;
                return Convert.ToInt32(output);
            }
        }

        private int _attempts = 0;
        [XmlIgnore]
        [NotMapped]
        public int Attempts {
            get {
                return _attempts;
            }
            protected set {
                _attempts = value;
                NotifyPropertyChanged("Attempts");
                NotifyPropertyChanged("ProgressText");
                NotifyPropertyChanged("Progress");

            }
        }

        #endregion

        #region "Download status"

        [XmlIgnore]
        [NotMapped]
        public string Speed {
            get {
                if (download_start_time == null || this.State != DownloadState.Downloading) {
                    return "";
                }
                TimeSpan time_since_start = DateTime.Now - download_start_time;
                if (time_since_start.Seconds == 0) {
                    return "";
                } else {
                    long per_sec = DownloadedLength / time_since_start.Seconds;
                    return FormatSize(per_sec) + "/sec";
                }

            }
        }
        [XmlIgnore]
        [NotMapped]
        public override string ProgressText {
            get {
                StringBuilder output = new StringBuilder();
                if (DownloadedLength < 0) {

                } else {
                    output.Append(FormatSize(DownloadedLength));
                    if (Length > 0) {
                        output.Append("/");
                    }
                }
                if (Length > 0) {
                    output.Append(FormatSize(Length));
                }

                if (Attempts > 1) {
                    output.Append(" (Attempt ");
                    output.Append(Attempts);
                    output.Append(")");
                }
                return output.ToString();
            }
        }
        #endregion

        #region Constructors
        public Downloadable() {
            SimpleHeaders = false;
        }
        public Downloadable(Uri url, Uri referer, string download_dir) : this(url, download_dir) {
            this.RefererURL = referer;
        }
        public Downloadable(Uri url, string download_dir) : this() {
            this.URL = url;
            this.DownloadDir = download_dir;
            MaxAttempts = 5;
        }


        #endregion


        #region Download controls


        public override void Reset() {
            this.State = DownloadState.Pending;
            if (this.client != null && this.client.IsBusy) {
                try {
                    this.client.CancelAsync();
                } finally {
                    try {
                        this.client.Dispose();
                    } catch (Exception e) {
                    } finally {
                        this.client = null;
                    }
                }
            }
        }

        public override void Pause() {
            this.State = DownloadState.Paused;
            if (this.client != null && this.client.IsBusy) {
                try {
                    this.client.CancelAsync();
                } finally {
                    try {
                        this.client.Dispose();
                    } catch (Exception e) {
                    } finally {
                        this.client = null;
                    }
                }
            }
        }

        #endregion

        private int FileNameIncrement = 0;

        private void IncrementFileName() {
            string filename = Path.GetFileNameWithoutExtension(this.FileName);
            string ext = Path.GetExtension(this.FileName);
            FileNameIncrement++;

            string post_file_portion = " (" + FileNameIncrement.ToString() + ")" + ext;
            if (post_file_portion.Length + filename.Length > 255) {
                this._OverrideFileName = filename.Substring(0, 255 - post_file_portion.Length) + post_file_portion;
            } else {
                this._OverrideFileName = filename + post_file_portion;
            }
        }

        private void setUpClient() {
            client = new SuperWebClient();
            client.SimpleHeaders = this.SimpleHeaders;

            if (this.RefererURL != null) {
                client.SetReferer(this.RefererURL);
            }

            client.DownloadProgressChanged += wc_DownloadProgressChanged;
            client.DownloadDataCompleted += client_DownloadCompleted;
            client.DownloadStringCompleted += client_DownloadCompleted;


        }

        #region Thread events
        protected override object DownloadThread() {
            if (client != null) {
                if (client.IsBusy) {
                    throw new Exception("File is already downloading");
                }
            }

            FileInfo fi = new FileInfo(this.GetDownloadPath());

            if (!fi.Directory.Exists) {
                fi.Directory.Create();
            }

            while (fi.Exists) {
                long length = 0;

                using (SuperWebClient swc = new SuperWebClient()) {
                    WebHeaderCollection whc = swc.GetHeaders(this.URL, this.RefererURL);
                    String length_string = whc[HttpResponseHeader.ContentLength];
                    try {
                        length = long.Parse(length_string);
                    } catch (Exception e) {
                        Console.Out.WriteLine(e.Message);
                    }

                }

                if (length == fi.Length) {
                    this.State = DownloadState.Skipped;
                    return null;
                }
               IncrementFileName();
                fi = new FileInfo(this.GetDownloadPath());
            }

            System.Threading.Thread.Sleep(this.StartDelay);

            setUpClient();

            switch (this.DataType) {
                case DownloadType.Binary:
                    client.DownloadDataAsync(this.URL);
                    break;
                case DownloadType.Text:
                    client.DownloadStringAsync(this.URL);
                    break;
            }
            this.State = DownloadState.Downloading;
            Attempts = 1;
            return null;
        }

        void client_DownloadCompleted(object sender, AsyncCompletedEventArgs e) {
            if (e.Error != null) {
                if (Attempts < MaxAttempts) {
                    Thread.Sleep(5000);
                    if (client == null) {
                        setUpClient();
                    }
                    switch (this.DataType) {
                        case DownloadType.Binary:
                            client.DownloadDataAsync(this.URL);
                            break;
                        case DownloadType.Text:
                            client.DownloadStringAsync(this.URL);
                            break;
                    }
                    Attempts = Attempts + 1;
                } else {
                    this.Exception = e.Error;
                    this.State = DownloadState.Error;
                    try {
                        client.Dispose();
                    } catch (Exception) { }
                    client = null;
                }
                return;
            }

            try {
                if (e.Cancelled) {
                    this.State = DownloadState.Paused;
                    return;
                }
                try {
                    //switch (this.Type) {
                    //    case DownloadType.Text:
                    //        this.Data = ((DownloadStringCompletedEventArgs)e).Result;
                    //        break;
                    //    case DownloadType.Binary:
                    //        this.Data = ((DownloadDataCompletedEventArgs)e).Result;
                    //        break;
                    //    default:
                    //        throw new NotSupportedException();
                    //}

                    FileInfo fi = new FileInfo(this.GetDownloadPath());

                    if (!fi.Directory.Exists) {
                        fi.Directory.Create();
                    }

                    while (fi.Exists) {
                        long length = 0;
                        switch (this.DataType) {
                            case DownloadType.Binary:
                                length = ((DownloadDataCompletedEventArgs)e).Result.LongLength;
                                break;
                            case DownloadType.Text:
                                length = ((DownloadStringCompletedEventArgs)e).Result.Length;
                                break;
                        }
                        if (length == fi.Length) {
                            this.State = DownloadState.Skipped;
                            return;
                        }
                        IncrementFileName();
                        fi = new FileInfo(this.GetDownloadPath());
                    }
                    switch (this.DataType) {
                        case DownloadType.Binary:
                            File.WriteAllBytes(this.GetDownloadPath(), ((DownloadDataCompletedEventArgs)e).Result);
                            break;
                        case DownloadType.Text:
                            File.WriteAllText(this.GetDownloadPath(), ((DownloadStringCompletedEventArgs)e).Result);
                            break;
                    }
                    this.State = DownloadState.Complete;
                } catch (Exception ex) {
                    this.Exception = ex;
                    this.State = DownloadState.Error;
                }
            } finally {
                try {
                    client.Dispose();
                } catch (Exception) { }
                client = null;
            }
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            this.Length = e.TotalBytesToReceive;
            this.DownloadedLength = e.BytesReceived;
        }

        #endregion

        #region Helper functions
        private string GetDownloadPath() {
            string filename = this.FileName;
            string ext = Path.GetExtension(filename);
            if (filename.Length > 248) {
                filename = filename.Substring(0, 248 - ext.Length) + ext;
            }

            if (filename.Length + this.DownloadDir.Length + 1 > 260) {
                if (260 - this.DownloadDir.Length - ext.Length - 2 <= 0) {
                    throw new Exception("The destination folder's name is too long!");
                }
                filename = filename.Substring(0, 260 - this.DownloadDir.Length - ext.Length - 2) + ext;
            }

            string output = Path.Combine(this.DownloadDir, filename);
            return output;
        }

        private string FormatSize(long len) {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length) {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", len, sizes[order]);
            return result;
        }

        private void FetchHeaderInfo() {
            System.Net.WebRequest req = System.Net.HttpWebRequest.Create(this.URL);
            req.Method = "HEAD";
            using (System.Net.WebResponse resp = req.GetResponse()) {
                int ContentLength;
                if (int.TryParse(resp.Headers.Get("Content-Length"), out ContentLength)) {
                    //Do something useful with ContentLength here 
                }
            }
        }

        public override void Dispose() {
            this.client.Dispose();
        }


        #endregion
    }
}
