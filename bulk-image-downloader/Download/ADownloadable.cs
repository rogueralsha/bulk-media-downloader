using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using BulkMediaDownloader.Model;

namespace BulkMediaDownloader.Download {
    public enum DownloadState {
        Pending,
        Paused,
        Downloading,
        Complete,
        Skipped,
        Error
    }
    public enum DownloadType {
        Binary,
        Text,
        Source
    }

    [Serializable]
    [XmlInclude(typeof(Downloadable))]
    [XmlInclude(typeof(DownloadablesSource))]
    public abstract class ADownloadable: INotifyPropertyChanged, IDisposable {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public DownloadType DataType = DownloadType.Binary;

        public String DownloadDir { get; set; }

        private DownloadState _State = DownloadState.Pending;
        public DownloadState State {
            get {
                return _State;
            }

            set {
                _State = value;
                NotifyPropertyChanged("State");
                NotifyPropertyChanged("StateText");
                NotifyPropertyChanged("Speed");
                NotifyPropertyChanged("ProgressText");
                NotifyPropertyChanged("Progress");
            }
        }

        [XmlIgnore]
        [NotMapped]
        public Uri URL { get; protected set; }
        public String URLString {
            get {
                return URL.ToString();
            }
            set {
                URL = new Uri(value);
            }
        }

        [XmlIgnore]
        [NotMapped]
        public string StateText {
            get {
                return State.ToString();
            }
        }

        [XmlIgnore]
        [NotMapped]
        public string ExtraInfo {
            get {
                return this.URL.ToString() + Environment.NewLine + this.Error;
            }
        }


        private Exception _except = null;
        [XmlIgnore]
        [NotMapped]
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
        [XmlIgnore]
        [NotMapped]
        public abstract int Progress { get; }

        [XmlIgnore]
        [NotMapped]
        public virtual string ProgressText {
            get {
                return this.State.ToString();
            }
        }

        [XmlIgnore]
        [NotMapped]
        public String Error {
            get {
                if (_except != null) {
                    return _except.Message;
                } else {
                    return "";
                }
            }
        }

        [XmlIgnore]
        [NotMapped]
        public abstract bool RequiresLogin { get; }

        BackgroundWorker worker = new BackgroundWorker();

        protected ADownloadable() {
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += ADownloadable_RunWorkerCompleted;
        }

        private void ADownloadable_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if(e.Error!=null) {
                this.State = DownloadState.Error;
                this._except = e.Error;
            } else {
                if(WorkComplete!=null)
                WorkComplete(this, e.Result);
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) {
            this.State = DownloadState.Downloading;
            e.Result = DownloadThread();
        }

        public void Start() {
            if(!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        public abstract void Pause();
        public abstract void Reset();

        public delegate void WorkCompleteEventHandler(ADownloadable sender, object result);

        public event WorkCompleteEventHandler WorkComplete;

        protected abstract object DownloadThread();

        #region INotify Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName = "") {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public abstract void Dispose();

        public override int GetHashCode() {
            return URL.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            if (!(obj is ADownloadable))
                return false;

            ADownloadable other = obj as ADownloadable;
            return this.URL.Equals(other.URL);
        }

        public void Save() {
            using (DatabaseContext db = new DatabaseContext()) {
                if (this.Id == Guid.Empty)
                    db.Add(this);
                else
                    db.Update(this);
            }
        }

        public void Delete() {
            using (DatabaseContext db = new DatabaseContext()) {
                db.Remove(this);
            }
        }
    }
}
