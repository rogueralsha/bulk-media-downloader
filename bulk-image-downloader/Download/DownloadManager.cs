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
using Microsoft.EntityFrameworkCore;
using BulkMediaDownloader.MediaSources;
using BulkMediaDownloader.Model;
using System.ComponentModel.DataAnnotations.Schema;


namespace BulkMediaDownloader.Download {

    public class DownloadManager : ObservableCollection<ADownloadable>, INotifyPropertyChanged {

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
        //public static bool Overwrite = false;

        private static bool StopTheMadness = false;

        private static List<string> locker = new List<string>();

        public static IGetCredentials CredentialsProvider;

        [XmlIgnore]
        public int Progress {
            get {
                double remaining = CompletedDownloads;
                double total = this.Count;
                double percent = remaining / total;
                return (int)Math.Ceiling(percent * 100);
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
        public int RemainingDownloads {
            get {
                int output = 0;
                foreach (ADownloadable dl in this) {
                    if (dl.State == DownloadState.Pending)
                        output++;
                }
                return output;
            }
        }
        [XmlIgnore]
        public int CompletedDownloads {
            get {
                int output = 0;
                foreach (ADownloadable dl in this) {
                    if (dl.State == DownloadState.Complete)
                        output++;
                }
                return output;
            }
        }

        public DownloadManager() {
            using(BulkMediaDownloader.Model.DatabaseContext db = new Model.DatabaseContext()) {
                foreach (DownloadablesSource d in db.DownloadableSources.ToList<DownloadablesSource>()) {
                    d.WorkComplete += Source_WorkComplete;
                    this.Add(d);                    
                }
                foreach (Downloadable d in db.Downloadables.ToList<Downloadable>()) {
                    d.WorkComplete += Source_WorkComplete;
                    this.Add(d);
                }
            }

            supervisor_thread = new Thread(Supervise);
            this.CollectionChanged += DownloadManager_CollectionChanged;
        }

        private void DownloadManager_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace) {
                foreach (ADownloadable d in e.NewItems) {
                    d.PropertyChanged += this.Down_PropertyChanged;
                    d.WorkComplete += this.Source_WorkComplete;
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace) {
                foreach (ADownloadable d in e.OldItems) {
                    try {
                        d.PropertyChanged -= this.Down_PropertyChanged;
                        d.WorkComplete -= this.Source_WorkComplete;
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
                    bool source_downloading = false;
                    for (int i = 0; i < this.Count; i++) {
                        if (this[i].State == DownloadState.Downloading) {
                            switch (this[i].DataType) {
                                case DownloadType.Source:
                                    source_downloading = true;
                                    break;
                                default:
                                    downloading_count++;
                                    break;
                            }
                        }

                    }
                    for (int i = 0; i < this.Count; i++) {
                        if (this[i].State == DownloadState.Pending) {
                            switch (this[i].DataType) {
                                case DownloadType.Source:
                                    if (!source_downloading)
                                        startDownload(this[i]);
                                    source_downloading = true;
                                    break;
                                default:
                                    if (downloading_count < MaxConcurrentDownloads) {
                                        startDownload(this[i]);
                                        downloading_count++;
                                    }
                                    break;
                            }
                        }
                    }
                }
                Thread.Sleep(50);
            }
            lock(this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Downloading) {
                        this[i].Pause();
                    }
                }
            }

        }

        private void startDownload(ADownloadable downlodable) {
            App.Current.Dispatcher.Invoke((Action)(() => {
                if(downlodable is DownloadablesSource) {
                    DownloadablesSource ds = downlodable as DownloadablesSource;
                    if (downlodable.RequiresLogin && CredentialsProvider != null) {
                        if (!CredentialsProvider.getCredentials(ds.MediaSource))
                            downlodable.Pause();
                            return;
                    }
                }
                downlodable.Start();
            }));
        }

        private void Source_WorkComplete(ADownloadable sender, object results) {
            if (results != null) {
                if (results is HashSet<MediaSources.MediaSourceResult>) {
                    foreach (MediaSources.MediaSourceResult result in (HashSet<MediaSources.MediaSourceResult>)results) {
                        AddMediaSourceResult(result, sender.DownloadDir);
                    }
                }
            }
        }

        public void AddMediaSourceResult(MediaSources.MediaSourceResult media, string download_dir) {
            ADownloadable down;
            switch (media.Type) {
                case MediaSources.MediaResultType.Download:
                    Downloadable downloadable = new Downloadable(media.URL, media.Referrer, download_dir);
                    downloadable.DataType = DownloadType.Binary;
                    downloadable.Site = media.Site;
                    downloadable.SimpleHeaders = media.SimpleHeaders;
                    downloadable.PropertyChanged += this.Down_PropertyChanged;
                    down = downloadable;
                    break;
                case MediaSources.MediaResultType.DownloadSource:
                    DownloadablesSource ds = new DownloadablesSource(media.MediaSource, media.Stage, media.Site, media.URL, media.Referrer);
                    ds.DownloadDir = download_dir;
                    ds.PropertyChanged += this.Down_PropertyChanged;
                    ds.WorkComplete += Source_WorkComplete;
                    down = ds;
                    break;
                default:
                    throw new NotSupportedException(media.Type.ToString());
            }

            App.Current.Dispatcher.Invoke((Action)(() => {
                if (!this.Contains(down)) {
                    this.Add(down);
                    down.Save();
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

        private void Down_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "State") {
                notifyProgressProperties();

            }
        }

        #region INotify Implementation

        public new event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "") {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public void ClearAllDownloads() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    try {
                        this[i].Pause();
                        this[i].Delete();
                    } catch { }
                }
                this.Clear();
            }
        }

        public void ClearCompleted() {
                using (DatabaseContext db = new DatabaseContext()) {

                    for (int i = 0; i < this.Count; i++) {
                        if (this[i].State == DownloadState.Complete || this[i].State == DownloadState.Skipped) {
                            db.Remove(this[i], false);
                            this.RemoveAt(i);
                            i--;
                        }
                    }
                    db.SaveChangesAsync();
                }
            notifyProgressProperties();
        }

        public void RestartFailed() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Error) {
                        this[i].Reset();
                        this[i].Save();
                    }
                }
            }
        }

        public void RestartAll() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Error ||
                        this[i].State == DownloadState.Paused) {
                        this[i].Reset();
                        this[i].Save();
                    }
                }
            }
        }

        public void PauseAll() {
            lock (this) {
                for (int i = 0; i < this.Count; i++) {
                    if (this[i].State == DownloadState.Pending ||
                        this[i].State == DownloadState.Downloading) {
                        this[i].Pause();
                        this[i].Save();
                    }
                }
            }
        }
        

    }
}
