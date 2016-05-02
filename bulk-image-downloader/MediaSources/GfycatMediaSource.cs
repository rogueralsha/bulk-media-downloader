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
    public class GfycatMediaSource: ASitemapMediaSource {
        //http://www.hentai-foundry.com/pictures/user/GENSHI
        private static Regex address_regex = new Regex(@"(.+gfycat.com/.+)");

        private string album_name;
        int results_per_page = 500;

        public GfycatMediaSource(Uri url)
            : base(url) {

            if (!SupportsUrl(url)) {
                throw new Exception("Gfycat URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            album_name = address_matches[0].Groups[2].Value;

        }

        public static bool SupportsUrl(Uri url)
        {
            return address_regex.IsMatch(url.ToString());
        }

        public override string getFolderNameFromURL(Uri url)
        {
            if (!SupportsUrl(url))
            {
                    throw new Exception("Gfycat URL not understood");
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

        private XmlNode getChildNode(XmlNode parent, string child_name, string child_attribute_name, string child_attribute_value)
        {
            foreach(XmlNode child in parent.ChildNodes)
            {
                if (child.Name.ToLower() != child_name.ToLower())
                    continue;

                if (child.Attributes[child_attribute_name].Value.ToLower() != child_attribute_value.ToLower())
                    continue;

                return child;
            }
            return null;
        }

        protected override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url, String page_contents) {
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            XmlNodeList nodes = GetEntries(page_contents);
            foreach(XmlNode node in nodes)
            {
                if (node.Attributes["html"].Value != "html")
                    continue;

                XmlNode contentNode = getChildNode(node, "content", "type", "html");
                if (contentNode == null)
                    continue;

                XmlNode linkNode = getChildNode(node, "link", "rel", "htmself");
                if (contentNode == null)
                    continue;

                Uri self_url = new Uri(linkNode.Attributes["href"].Value);
                String entry = WebUtility.HtmlDecode(node.InnerText);

                foreach (Uri url in getImagesAndDirectLinkedMedia(self_url, entry))
                {
                    output.Add(new MediaSourceResult(url, self_url, this.url));
                }
            }
            return output;

        }



    }
}
