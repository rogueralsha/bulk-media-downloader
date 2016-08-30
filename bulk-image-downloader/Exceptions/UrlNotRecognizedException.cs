using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkMediaDownloader {
    public class UrlNotRecognizedException: Exception {
        public UrlNotRecognizedException(String message) : base(message) { }
    }
}
