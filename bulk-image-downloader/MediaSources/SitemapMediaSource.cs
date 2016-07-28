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
        //http://www.hentai-foundry.com/pictures/user/GENSHI
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


        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents) {
            HashSet<Uri> output = new HashSet<Uri>();
            output.Add(page_url);
            return output;
        }



        public override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url) {
            String page_contents = this.GetPageContents(page_url);
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            XmlNodeList nodes = GetEntries(page_contents);
            foreach(XmlNode node in nodes)
            {

                XmlNode locNode = getChildNode(node, "loc");
                if (locNode == null)
                    continue;

                if (String.IsNullOrWhiteSpace(locNode.InnerText))
                    continue;

                Uri self_url = new Uri(locNode.InnerText);

                List<XmlNode> imageNodes = getChildNodes(node, "image:image");
                foreach(XmlNode imageNode in imageNodes)
                {
                    XmlNode imageLocNode = getChildNode(imageNode, "image:loc");
                    if(imageLocNode!=null&&!string.IsNullOrEmpty(imageLocNode.InnerText))
                    {
                        output.Add(new MediaSourceResult(new Uri(imageLocNode.InnerText), self_url, this.url));
                    }
                }

                String entry = GetPageContents(self_url);

                List<Uri> things = getImagesAndDirectLinkedMedia(self_url, entry);
                foreach(Uri thing in things)
                {
                    output.Add(new MediaSourceResult(thing, self_url, this.url));
                }

            }
            return output;

        }



    }
}
