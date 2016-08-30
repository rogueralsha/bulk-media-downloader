using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace BulkMediaDownloader.MediaSources
{
    public class BloggerMediaSource: ASitemapMediaSource {
        private static Regex address_regex = new Regex(@"(http[s]?://([^\.]+).blogspot.com/sitemap.xml)");

        private string album_name;
        int results_per_page = 500;

        public BloggerMediaSource(Uri url)
            : base(url) {

            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("Blogger URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            album_name = address_matches[0].Groups[2].Value;

        }


        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
        }


        public override string getFolderNameFromURL(Uri url)
        {
            if (!address_regex.IsMatch(url.ToString()))
            {
                throw new Exception("Blogger URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string album_name = address_matches[0].Groups[2].Value;
            return album_name;
        }



    }
}
