using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkMediaDownloader
{
    class UrlToProcess
    {
        public readonly Uri url;
        public readonly string image_source_name;


        public UrlToProcess(Uri url, string image_source)
        {
            this.url = url;
            this.image_source_name = image_source;
        }
    }
}
