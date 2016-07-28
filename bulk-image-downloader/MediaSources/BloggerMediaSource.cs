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
        //http://www.hentai-foundry.com/pictures/user/GENSHI
        private static Regex address_regex = new Regex(@"(http[s]?://([^\.]+).blogspot.com/atom.xml)");

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

        private XmlNodeList GetEntries(string page_contents)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page_contents);

            XmlNodeList nodes = doc.GetElementsByTagName("entry");
            return nodes;
        }

        private Uri constructPageUrl(Uri base_url, int offset)
        {
            UriBuilder output = new UriBuilder(base_url);
            // http://aaaninja.blogspot.com/atom.xml?redirect=false&start-index=1&max-results=500

            output.Query = "redirect=false&start-index=" + offset + "&max-results=" + results_per_page;

            return output.Uri;
        }

        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents) {
            HashSet<Uri> output = new HashSet<Uri>();

            int offset = 1;
            Uri current_url = constructPageUrl(page_url, offset);
            XmlNodeList nodes = GetEntries(page_contents);

            while(nodes.Count>0)
            {
                output.Add(current_url);
                offset += results_per_page;
                current_url = constructPageUrl(page_url, offset);
                page_contents = GetPageContents(current_url);
                nodes = GetEntries(page_contents);
            }

            return output;

        }

        public override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url) {
            String page_contents = this.GetPageContents(page_url);
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            XmlNodeList nodes = GetEntries(page_contents);
            foreach(XmlNode node in nodes)
            {
                XmlNode contentNode = getChildNode(node, "content", "type", "html");
                if (contentNode == null)
                    continue;

                XmlNode linkNode = getChildNode(node, "link", "rel", "self");
                if (linkNode == null)
                    continue;

                if (linkNode.Attributes["href"] == null)
                    continue;

                Uri self_url = new Uri(linkNode.Attributes["href"].Value);
                String entry = WebUtility.HtmlDecode(contentNode.InnerText);

                foreach (Uri url in getImagesAndDirectLinkedMedia(self_url, entry))
                {
                    output.Add(new MediaSourceResult(url, self_url,this.url));
                }
            }
            return output;

        }



    }
}
