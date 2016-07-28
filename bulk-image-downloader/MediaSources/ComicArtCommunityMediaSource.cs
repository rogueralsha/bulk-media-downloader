using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;

namespace BulkMediaDownloader.MediaSources
{
    public class ComicArtCommunityMediaSource : AMediaSource
    {
        //http://comicartcommunity.com/gallery/categories.php?cat_id=116&page=2
        private static Regex address_regex = new Regex(@"https?://(?:www\.)?comicartcommunity\.com/gallery/categories.php\?cat_id=(\d+)");
        private string album_name;
        private string cat_id;

        public ComicArtCommunityMediaSource(Uri url)
            : base(url) {

            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("CAC url not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            cat_id = address_matches[0].Groups[1].Value;
            setAlbumName(url);
        }

        public override string getFolderNameFromURL(Uri url) {
            setAlbumName(url);
            return album_name;
        }

        private void setAlbumName(Uri url) {
            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("CAC url not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string contents = GetPageContents(new Uri(address_matches[0].Value));
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(contents);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//span[@class='clickstream']");
            if (node == null)
                throw new Exception("Clickstream node not found");
            for(int i = 1; i < node.ChildNodes.Count; i++) {
                if (node.ChildNodes[i].InnerText.Length > 1) {
                    album_name = node.ChildNodes[i].InnerText.Trim('/');
                    return;
                }
            }
            album_name = address_matches[0].Groups[1].Value;
        }

        private String getCategoryUrl(String category) {
            //http://comicartcommunity.com/gallery/categories.php?cat_id=116
            return "http://comicartcommunity.com/gallery/categories.php?cat_id=" + category;

        }


        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents) {
            HashSet<Uri> output = new HashSet<Uri>();

            String baseUrl = getCategoryUrl(cat_id);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//a[text() = 'Last page &raquo;']");
            if (node == null) {
                output.Add(page_url);
                return output;
            }
                //throw new Exception("Could not find last page node");
                //<a href="categories.php?cat_id=116&amp;page=30" class="paging">Last page »</a>
            int index = node.Attributes["href"].Value.LastIndexOf("page=");
            String last_number_string = node.Attributes["href"].Value.Substring(index + 5);
            int last_page = int.Parse(last_number_string);

            for(int i = 1; i<= last_page; i++) {
                output.Add(new Uri(baseUrl  + "&page=" + i));

            }

            return output;

        }


        public override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url) {
            String page_contents = this.GetPageContents(page_url);
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//tr[@class='imagerow1' or @class='imagerow2']/td/a");
            
            if (nodes == null)
                throw new Exception("Could not find any image nodes");

            foreach(HtmlNode node in nodes) {
                if (!node.Attributes["href"].Value.Contains("./details.php?image_id="))
                  continue;
                //http://comicartcommunity.com/gallery/details.php?image_id=47490
                Uri imagePageUrl = new Uri("http://comicartcommunity.com/gallery" + node.Attributes["href"].Value.Substring(1));
                String imagePageContents = GetPageContents(imagePageUrl);
                HtmlDocument imageDocument = new HtmlDocument();
                imageDocument.LoadHtml(imagePageContents);

                HtmlNode imageNode = imageDocument.DocumentNode.SelectSingleNode("//div/img");
                if (imageNode == null)
                    throw new Exception("Image node not found");
                //http://comicartcommunity.com/gallery/data/media/116/Barb_Wire_1_2.jpg
                String newUrl = "http://comicartcommunity.com/gallery" + imageNode.Attributes["src"].Value.Substring(1);
                output.Add(new MediaSourceResult(new Uri(newUrl), imagePageUrl, this.url));
            }

            return output;

        }


    }
}
