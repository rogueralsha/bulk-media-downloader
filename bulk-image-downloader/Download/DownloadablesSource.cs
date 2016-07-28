using BulkMediaDownloader.MediaSources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BulkMediaDownloader.Download {
    [Serializable]
    public class DownloadablesSource : ADownloadable {

        public String MediaSourceName;


        private AMediaSource _MediaSource;
        [XmlIgnore]
        public AMediaSource MediaSource {
            get {
                if (_MediaSource == null) {
                    switch (MediaSourceName) {
                        case "ShimmieMediaSource":
                            _MediaSource = new ShimmieMediaSource(this.SourceURL);
                            break;
                        case "DeviantArtMediaSource":
                            _MediaSource = new DeviantArtMediaSource(this.SourceURL);
                            break;
                        case "TumblrMediaSource":
                            _MediaSource = new TumblrMediaSource(this.SourceURL);
                            break;
                        case "HentaiFoundryMediaSource":
                            _MediaSource = new HentaiFoundryMediaSource(this.SourceURL);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
                return _MediaSource;
            }
        }

        [XmlIgnore]
        public override bool RequiresLogin {
            get {
                return MediaSource.RequiresLogin;
            }
        }


        [XmlIgnore]
        public string FileName {
            get {
                return this.URLString;
            }
        }

        [XmlIgnore]
        public Uri SourceURL { get; protected set; }
        public String SourceURLString {
            get {
                return SourceURL.ToString();
            }
            set {
                SourceURL = new Uri(value);
            }
        }

        public DownloadablesSource() {
            this.Type = DownloadType.Source;
        }

        public DownloadablesSource(String MediaSourceName, Uri source_uri, Uri uri): this() {
            this.MediaSourceName = MediaSourceName;
            this.URL = uri;
            this.SourceURL = source_uri;
        }

        protected override object DownloadThread() {
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            AMediaSource source = MediaSource;

            HashSet<MediaSourceResult> page_images =
                source.GetMediaFromPage(this.URL);

            foreach (MediaSourceResult media in page_images) {
                output.Add(media);
            }
            this.State = DownloadState.Complete;
            return output;
        }

        public override void Pause() {
            this.State = DownloadState.Paused;
            //throw new NotImplementedException();
        }

        public override void Reset() {
            this.State = DownloadState.Pending;
        }
    }
}
