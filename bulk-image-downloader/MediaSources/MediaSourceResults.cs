using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkMediaDownloader.MediaSources {
    public class MediaSourceResults: HashSet<MediaSourceResult> {

        public void AddRange(IList<MediaSourceResult> list) {
            foreach(MediaSourceResult msr in list) {
                this.Add(msr);
            }
        }
    }
}
