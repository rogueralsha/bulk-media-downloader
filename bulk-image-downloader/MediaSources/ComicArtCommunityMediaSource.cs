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

            if (!ValidateUrl(url)) {
                throw new Exception("CAC url not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            cat_id = address_matches[0].Groups[1].Value;
            setAlbumName(url);
        }


        public static bool ValidateUrl(Uri url)
        {
            return address_regex.IsMatch(url.ToString());
        }

        public override string getFolderNameFromURL(Uri url) {
            setAlbumName(url);
            return album_name;
        }

        private void setAlbumName(Uri url) {
            if (!ValidateUrl(url)) {
                throw new Exception("CAC url not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string contents = GetPageContents(new Uri(address_matches[0].Value));
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(contents);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//title");
            if (node == null)
            {
                throw new Exception("Title node not found");
            }
            else
            {
                album_name = node.InnerText.Split('-')[0].Trim();
                return;
            }

            //album_name = address_matches[0].Groups[1].Value;
        }

        private String getCategoryUrl(String category) {
            //http://comicartcommunity.com/gallery/categories.php?cat_id=116
            return "http://comicartcommunity.com/gallery/categories.php?cat_id=" + category;

        }

        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage)
        {
            switch (stage)
            {
                case INITIAL_STAGE:
                    return GetGalleryPages(url, page_contents);
                case "gallery":
                    return GetImagePages(url, page_contents);
                case "image":
                    return GetImages(url, page_contents);
                default:
                    throw new NotSupportedException(stage);
            }
        }


        private MediaSourceResults GetGalleryPages(Uri page_url, String page_contents)
        {
            MediaSourceResults output = new MediaSourceResults();

            String baseUrl = getCategoryUrl(cat_id);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//a[text() = 'Last page &raquo;']");
            if (node == null) {
                output.Add(new MediaSourceResult(page_url, page_url, this.url, this, MediaResultType.DownloadSource, "gallery"));
                return output;
            }
                //throw new Exception("Could not find last page node");
                //<a href="categories.php?cat_id=116&amp;page=30" class="paging">Last page »</a>
            int index = node.Attributes["href"].Value.LastIndexOf("page=");
            String last_number_string = node.Attributes["href"].Value.Substring(index + 5);
            int last_page = int.Parse(last_number_string);

            for(int i = 1; i<= last_page; i++) {
                output.Add(new MediaSourceResult(new Uri(baseUrl + "&page=" + i), page_url, this.url, this, MediaResultType.DownloadSource, "gallery"));
            }

            return output;

        }

        private MediaSourceResults GetImagePages(Uri page_url, String page_contents)
        {
            MediaSourceResults output = new MediaSourceResults();
            sendStatus("Fetching image pages from " + page_url.ToString());

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//tr[@class='imagerow1' or @class='imagerow2']/td//a");

            if (nodes == null)
                throw new Exception("Could not find any image nodes");

            foreach (HtmlNode node in nodes)
            {
                if (!node.Attributes["href"].Value.Contains("./details.php?image_id="))
                    continue;
                //http://comicartcommunity.com/gallery/details.php?image_id=47490
                Uri imagePageUrl = new Uri("http://comicartcommunity.com/gallery" + node.Attributes["href"].Value.Substring(1));

                output.Add(new MediaSourceResult(imagePageUrl, page_url, this.url, this, MediaResultType.DownloadSource, "image"));

            }


            return output;
        }

        private MediaSourceResults GetImages(Uri page_url, String page_contents)
        {
            MediaSourceResults output = new MediaSourceResults();

            HtmlDocument imageDocument = new HtmlDocument();
            imageDocument.LoadHtml(page_contents);

            HtmlNodeCollection imageNodes = imageDocument.DocumentNode.SelectNodes("//div//img");
            if (imageNodes == null||imageNodes.Count==0)
                throw new Exception("Image node not found");

            foreach(HtmlNode imageNode in imageNodes)
            {
                String srcAttribute = imageNode.Attributes["src"].Value.Substring(1);
                if(srcAttribute.StartsWith("/data/media/")) {
                    //http://comicartcommunity.com/gallery/data/media/116/Barb_Wire_1_2.jpg
                    String newUrl = "http://comicartcommunity.com/gallery" + srcAttribute;
                    output.Add(new MediaSourceResult(new Uri(newUrl), page_url, this.url, this, MediaResultType.Download));
                }
            }

            return output;
        }

        }
    }
