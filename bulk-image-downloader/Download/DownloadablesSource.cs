using BulkMediaDownloader.MediaSources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BulkMediaDownloader.Download {
    [Serializable]
    public class DownloadablesSource : ADownloadable {

        public String MediaSourceName { get; set; }

        public String SourceStage { get; set; }

        private AMediaSource _MediaSource;

        [XmlIgnore]
        [NotMapped]
        public AMediaSource MediaSource {
            get {
                if (_MediaSource == null) {
                    _MediaSource = MediaSourceManager.GetMediaSourceForUrl(this.SourceURL);
                }
                return _MediaSource;
            }
        }

        [XmlIgnore]
        [NotMapped]
        public override bool RequiresLogin {
            get {
                return MediaSource.RequiresLogin;
            }
        }


        [XmlIgnore]
        [NotMapped]
        public string FileName {
            get {
                return this.URLString;
            }
        }

        [XmlIgnore]
        [NotMapped]
        public Uri RefererURL { get; protected set; }
        public String RefererURLString {
            get {
                if (RefererURL == null)
                    return String.Empty;
                return RefererURL.ToString();
            }
            set {
                if (String.IsNullOrEmpty(value))
                    RefererURL = null;
                else
                    RefererURL = new Uri(value);
            }
        }
        [XmlIgnore]
        [NotMapped]
        public Uri SourceURL { get; protected set; }
        public String SourceURLString {
            get {
                return SourceURL.ToString();
            }
            set {
                SourceURL = new Uri(value);
            }
        }

        [XmlIgnore]
        [NotMapped]
        public override int Progress {
            get {
                if (State == DownloadState.Complete || State == DownloadState.Skipped) {
                    return 100;
                }
                    return 0;
            }
        }



        public DownloadablesSource() {
            this.DataType = DownloadType.Source;
        }

        public DownloadablesSource(AMediaSource MediaSource, String stage, Uri source_uri, Uri uri, Uri referer_uri) : 
            this(MediaSource.GetType().Name, stage, source_uri, uri, referer_uri) {
            this._MediaSource = MediaSource;
        }



        public DownloadablesSource(String MediaSourceName, String stage, Uri source_uri, Uri uri, Uri referer_uri) : this() {
            this.MediaSourceName = MediaSourceName;
            this.URL = uri;
            this.SourceStage = stage;
            this.SourceURL = source_uri;
            this.RefererURL = referer_uri;
        }

        protected override object DownloadThread() {
            AMediaSource source = MediaSource;

            MediaSourceResults page_images = source.ProcessDownloadSource(this.URL, this.RefererURL, this.SourceStage);

            this.State = DownloadState.Complete;
            return page_images;
        }

        public override void Pause() {
            this.State = DownloadState.Paused;
            //throw new NotImplementedException();
        }

        public override void Reset() {
            this.State = DownloadState.Pending;
        }

        public override void Dispose() {
        }

        public override bool Equals(object obj) {
            bool result = base.Equals(obj);

            if (obj is DownloadablesSource && result) {
                DownloadablesSource other = obj as DownloadablesSource;
                return this.SourceStage.Equals(other.SourceStage);
            }

            return result;
        }
    }
}
