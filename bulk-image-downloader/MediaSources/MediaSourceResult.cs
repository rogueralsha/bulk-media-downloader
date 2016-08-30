using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkMediaDownloader.MediaSources
{
    public enum MediaResultType {
        Download, DownloadSource
    }
    public class MediaSourceResult: IEqualityComparer<MediaSourceResult>
    {
        public String MediaSourceName {
            get {
                return MediaSource.GetType().Name;
            }
        }
        public readonly AMediaSource MediaSource;
        public readonly MediaResultType Type;
        public readonly String Stage;
        public readonly Uri URL;
        public readonly Uri Referrer;
        public readonly Uri Site;
        public bool SimpleHeaders = false;

        public MediaSourceResult(Uri url, Uri referrer, Uri site, AMediaSource media_source, MediaResultType type, String stage = null)
        {
            this.URL = url;
            this.Referrer = referrer;
            this.Site = site;
            this.Type = type;
            this.Stage = stage;
            this.MediaSource = media_source;
        }

        public override int GetHashCode() {
            return URL.GetHashCode();
        }

        public override bool Equals(object obj) {
            MediaSourceResult other = obj as MediaSourceResult;
            return this.URL.Equals(other.URL);
        }

        public bool Equals(MediaSourceResult x, MediaSourceResult y) {
            return x.URL.Equals(y.URL);
        }

        public int GetHashCode(MediaSourceResult obj) {
            return obj.GetHashCode();
        }
    }
}
