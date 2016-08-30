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
    public class SitemapMediaSource: ASitemapMediaSource {
        private static Regex address_regex = new Regex(@"(https?://([^/]+)/.*sitemap.*)");

        private string album_name;

        public SitemapMediaSource(Uri url)
            : base(url) {

            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("Sitemap URL not understood");
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
                throw new Exception("Sitemap URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string album_name = address_matches[0].Groups[2].Value;
            return album_name;
        }

        private XmlNodeList GetEntries(string page_contents)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page_contents);

            XmlNodeList nodes = doc.GetElementsByTagName("url");
            return nodes;
        }

    }
}
