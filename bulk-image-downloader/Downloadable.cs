using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Xml;
namespace BulkMediaDownloader {
    enum DownloadState {
        Pending,
        Paused,
        Downloading,
        Complete,
        Skipped,
        Error
    }
    enum DownloadType {
        Binary,
        Text
    }

    class Downloadable : INotifyPropertyChanged {
        private Thread download_thread;
        private SuperWebClient client;

        public int MaxAttempts { get; set; }

        public DownloadType Type = DownloadType.Binary;

        public string FileName {
            get {
                StringBuilder path =  new StringBuilder(Uri.UnescapeDataString(this.URL.ToString()));
                foreach(char c in System.IO.Path.GetInvalidPathChars())
                {
                    path.Replace(c, '_');
                }
                path.Replace(':', '_');
                string file = Path.GetFileName(path.ToString());
                if(file.Length > 255)
                {
                    int length = 255 - Path.GetExtension(file).Length;
                    file =  Path.GetFileNameWithoutExtension(file).Substring(0,length) + Path.GetExtension(file);
                }
                return file;
            }
        }
        public Uri URL { get; protected set; }

        //public Object Data { get; protected set; }


        private DateTime download_start_time;

        public String DownloadDir { get; protected set; }
        public String Source { get; set; }

        public int StartDelay = 1000;

        #region Properties
        private DownloadState _State = DownloadState.Pending;
        public DownloadState State {
            get {
                return _State;
            }

            protected set {
                _State = value;
                NotifyPropertyChanged("State");
                NotifyPropertyChanged("StateText");
                NotifyPropertyChanged("Speed");
                NotifyPropertyChanged("ProgressText");
                NotifyPropertyChanged("Progress");
            }
        }
        public string StateText {
            get {
                return State.ToString();
            }
        }

        private Exception _except = null;
        public Exception Exception {
            get {
                return _except;
            }
            protected set {
                _except = value;
                NotifyPropertyChanged("Exception");
                NotifyPropertyChanged("Error");
            }

        }
        public String Error {
            get {
                if (_except != null) {
                    return _except.Message;
                } else {
                    return "";
                }
            }
        }

        private long _length = -1;
        public long Length {
            get {
                return _length;
            }

            protected set {
                _length = value;
                NotifyPropertyChanged("Length");
                NotifyPropertyChanged("Progress");
                NotifyPropertyChanged("Speed");
                NotifyPropertyChanged("ProgressText");
            }
        }

        private long _downloaded_length = -1;
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



        public int Progress {
            get {
                if(State== DownloadState.Complete|| State== DownloadState.Skipped) {
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

        public string ProgressText {
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
        public Downloadable(Uri url, string download_dir) {
            this.URL = url;
            this.DownloadDir = download_dir;
            download_thread = new Thread(DownloadThread);
            MaxAttempts = 5;
        }

        public Downloadable(XmlElement ele) {
            try {
                LoadFromXML(ele);

                download_thread = new Thread(DownloadThread);
                MaxAttempts = 5;
            } catch {
                this.State = DownloadState.Error;
            }
        }
#endregion

        #region XML Read/write
        public XmlElement CreateElement(XmlDocument doc) {
            XmlElement ele = doc.CreateElement("downloadable");

            XmlElement field = doc.CreateElement("url");
            field.InnerText = URL.ToString();
            ele.AppendChild(field);

            field = doc.CreateElement("download_dir");
            field.InnerText = this.DownloadDir;
            ele.AppendChild(field);

            field = doc.CreateElement("state");
            field.InnerText = this.State.ToString();
            ele.AppendChild(field);

            field = doc.CreateElement("type");
            field.InnerText = this.Type.ToString();
            ele.AppendChild(field);

            return ele;
        }

        private void LoadFromXML(XmlElement ele) {
            if(ele.GetElementsByTagName("url").Count>0) {
                this.URL= new Uri(ele.GetElementsByTagName("url")[0].InnerText);
            } else {
                throw new Exception("No URL specified");
            }

            if (ele.GetElementsByTagName("download_dir").Count > 0) {
                this.DownloadDir = ele.GetElementsByTagName("download_dir")[0].InnerText;
            } else {
                throw new Exception("No download dir specified");
            }

            if (ele.GetElementsByTagName("state").Count > 0) {
                Enum.TryParse<DownloadState>(ele.GetElementsByTagName("state")[0].InnerText, out this._State);
            } else {
                this.State = DownloadState.Pending;
            }

            if (ele.GetElementsByTagName("type").Count > 0) {
                Enum.TryParse<DownloadType>(ele.GetElementsByTagName("type")[0].InnerText, out this.Type);
            } else {
                this.State = DownloadState.Pending;
            }

            if (this.State == DownloadState.Downloading) {
                this.State = DownloadState.Pending;
            }
        }


        #endregion

        #region Download controls
        public void Start() {
            this.State = DownloadState.Downloading;
            try {
                if(this.download_thread.ThreadState== ThreadState.Stopped)
                {
                    this.download_thread = new Thread(DownloadThread);
                }
                this.download_thread.Start();
            } catch (ThreadStartException ex)
            {
                this.download_thread = new Thread(DownloadThread);
                this.download_thread.Start();
            }
        }

        public void Reset() {
            this.State = DownloadState.Pending;
        }

        public void Pause() {
            this.State = DownloadState.Paused;
            if (this.client != null && this.client.IsBusy) {
                this.client.CancelAsync();
            }
        }

        #endregion

        #region Thread events
        private void DownloadThread() {
            try {
                if (client != null) {
                    if (client.IsBusy) {
                        throw new Exception("File is already downloading");
                    }
                }

                if (!DownloadManager.Overwrite && File.Exists(this.GetDownloadPath())) {
                    this.State = DownloadState.Skipped;
                    return;
                }

                System.Threading.Thread.Sleep(this.StartDelay);

                client = new SuperWebClient();
                client.DownloadProgressChanged += wc_DownloadProgressChanged;
                client.DownloadDataCompleted +=  client_DownloadCompleted;
                client.DownloadStringCompleted += client_DownloadCompleted;

                switch (this.Type) {
                    case DownloadType.Binary:
                        client.DownloadDataAsync(this.URL);
                        break;
                    case DownloadType.Text:
                        client.DownloadStringAsync(this.URL);
                        break;
                }
                this.State = DownloadState.Downloading;
                Attempts = 1;
                return;

            } catch (Exception e) {
                this.Exception = e;
                this.State = DownloadState.Error;
            }
        }

        void client_DownloadCompleted(object sender, AsyncCompletedEventArgs e) {
            if (e.Error != null) {
                if (Attempts < MaxAttempts) {
                    Thread.Sleep(5000);
                    switch (this.Type) {
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

                    if (!DownloadManager.Overwrite && File.Exists(this.GetDownloadPath())) {
                        this.State = DownloadState.Skipped;
                        return;
                    } else {
                        switch (this.Type) {
                            case DownloadType.Binary:
                                File.WriteAllBytes(this.GetDownloadPath(), ((DownloadDataCompletedEventArgs)e).Result);
                                break;
                            case DownloadType.Text:
                                File.WriteAllText(this.GetDownloadPath(), ((DownloadStringCompletedEventArgs)e).Result);
                                break;
                        }
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

        protected static WebClient PrepareClient() {
            WebClient wc = new WebClient();

            //if (!string.IsNullOrEmpty(Properties.Settings.Default.SOCKS_Proxy_Host))
            //{
            //    wc.Proxy = new WebProxy(Properties.Settings.Default.SOCKS_Proxy_Host, Properties.Settings.Default.SOCKS_Proxy_Port);
            //}

            return wc;

        }
        #endregion

        #region INotify Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "") {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
