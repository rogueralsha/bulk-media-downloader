using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkMediaDownloader.MediaSources
{
    public class MediaSourceResult
    {
        public readonly Uri URL;
        public readonly Uri Referrer;
        public readonly Uri Site;
        public bool SimpleHeaders = false;

        public MediaSourceResult(Uri url, Uri referrer, Uri site)
        {
            this.URL = url;
            this.Referrer = referrer;
            this.Site = site;
        }

        public override int GetHashCode() {
            return URL.GetHashCode();
        }
    }
}
