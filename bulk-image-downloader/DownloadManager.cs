using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace BulkMediaDownloader {

    public class DownloadManager : ObservableCollection<Downloadable>, INotifyPropertyChanged {

        private Thread supervisor_thread;

        //public static string DownloadDir {
        //    get {
        //        return Properties.Settings.Default.DownloadDir;
        //    }
        //    set {
        //        Properties.Settings.Default.DownloadDir = value;
        //        Properties.Settings.Default.Save();
        //    }
        //}
        public static bool Overwrite = false;

        private static bool StopTheMadness = false;

        private static List<string> locker = new List<string>();

        [XmlIgnore]
        public int Progress {
            get {
                double remaining = CompletedDownloads;
                double total = this.Count;
                double percent = remaining / total;
                return (int)Math.Ceiling(percent*100);
            }
        }
        [XmlIgnore]
        public double ProgressDouble {
            get {
                double remaining = CompletedDownloads;
                double total = this.Count;
                double percent = remaining / total;
                return percent;
            }
        }
        [XmlIgnore]
        public string ProgressText {
            get {
                return CompletedDownloads.ToString() + "/" + this.Count.ToString();
            }
        }


        [XmlIgnore]
        public static int MaxConcurrentDownloads {
            get {
                return Properties.Settings.Default.MaxConcurrentDownloads;
            }
            set {
                Properties.Settings.Default.MaxConcurrentDownloads = value;
                Properties.Settings.Default.Save();
            }
        }
        [XmlIgnore]
        public int RemainingDownloads
        {
            get
            {
                int output = 0;
                foreach(Downloadable dl in this)
                {
                    if (dl.State != DownloadState.Complete && dl.State != DownloadState.Skipped)
                        output++;
                }
                return output;
            }
        }
        [XmlIgnore]
        public int CompletedDownloads {
            get {
                int output = 0;
                foreach (Downloadable dl in this) {
                    if (dl.State == DownloadState.Complete || dl.State == DownloadState.Skipped)
                        output++;
                }
                return output;
            }
        }
        public DownloadManager() {


            supervisor_thread = new Thread(Supervise);
            this.CollectionChanged += DownloadManager_CollectionChanged;
        }

        private void DownloadManager_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace) {
                foreach (Downloadable d in e.NewItems) {
                    d.PropertyChanged += this.Down_PropertyChanged;
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace) {
                foreach (Downloadable d in e.OldItems) {
                    try {
                        d.PropertyChanged -= this.Down_PropertyChanged;
                    } catch (Exception ex) {
                        Console.Out.Write(ex.Message);
                    }
                }
            }
        }

        private void notifyProgressProperties() {
            NotifyPropertyChanged("Progress");
            NotifyPropertyChanged("ProgressDouble");
            NotifyPropertyChanged("ProgressText");
            NotifyPropertyChanged("RemainingDownloads");
        }

        public void Start() {
            StopTheMadness = false;
            supervisor_thread.Start();
        }

        public void Stop() {
            StopTheMadness = true;
        }

        private void Supervise() {
            while (!StopTheMadness) {
                lock (this) {
                    int downloading_count = 0;
                    for (int i = 0; i < this.Count; i++) {
                        if (this[i].State == DownloadState.Downloading) {
                            downloading_count++;
                        }
                    }
                    for (int i = 0; i < this.Count; i++) {
                        if ((downloading_count < MaxConcurrentDownloads || this[i].Type == DownloadType.Text) && this[i].State == DownloadState.Pending) {
                            this[i].Start();
                            downloading_count++;
                        }

                    }
                }
                Thread.Sleep(50);
            }
        }

        public void DownloadMedia(MediaSources.MediaSourceResult media, string download_dir) {
            Downloadable down = new Downloadable(media.URL, media.Referrer, download_dir);
            down.Type = DownloadType.Binary;
            down.Site = media.Site;
            down.SimpleHeaders = media.SimpleHeaders;
            down.PropertyChanged += this.Down_PropertyChanged;
            App.Current.Dispatcher.Invoke((Action)(() => {
                lock (this)
                {
                    this.Add(down);
                }
            }));
        }

        //public Downloadable AddDownloadable(Uri url, string download_dir, string source, DownloadType type) {
        //    Downloadable down = new Downloadable(url, download_dir);
        //    down.Type = type;
        //    down.Source = source;
        //    down.PropertyChanged += this.Down_PropertyChanged;
        //    App.Current.Dispatcher.Invoke((Action)(() => {
        //        lock (this) {
        //            this.Add(down);
        //        }
        //    }));

        //    return down;
        //}

        private void Down_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName=="State")
            {
                notifyProgressProperties();
                
            }
        }

        #region INotify Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public void ClearAllDownloads() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    try {
                        this[i].Pause();
                    } catch { }
                }
                this.Clear();
            }
            SaveAll();
        }

        public void ClearCompleted() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Complete || this[i].State == DownloadState.Skipped) {
                        this.RemoveAt(i);
                        i--;
                    }
                }
            }
            SaveAll();
            notifyProgressProperties();
        }

        public  void SaveAll() {
            XmlSerializer x = new XmlSerializer(this.GetType());
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb)) {
                x.Serialize(sw, this);
            }

            Properties.Settings.Default.Save();
        }

        public void RestartFailed() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Error) {
                        this[i].Reset();
                    }
                }
            }
            SaveAll();
        }

        public void RestartAll() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Error||
                        this[i].State == DownloadState.Paused) {
                        this[i].Reset();
                    }
                }
            }
            SaveAll();
        }

        public void PauseAll() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Pending||
                        this[i].State == DownloadState.Downloading) {
                        this[i].Pause();
                    }
                }
            }
            SaveAll();
        }


    }
}
