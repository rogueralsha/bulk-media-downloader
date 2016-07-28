using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkMediaDownloader.Download {
    public interface IGetCredentials {

        bool getCredentials(BulkMediaDownloader.MediaSources.AMediaSource source);
    }
}
