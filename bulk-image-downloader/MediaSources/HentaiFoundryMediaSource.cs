using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace BulkMediaDownloader.MediaSources
{
    public class HentaiFoundryMediaSource : AMediaSource {
        //http://www.hentai-foundry.com/pictures/user/GENSHI
        private static Regex address_regex = new Regex(@"((.+)/pictures/user/([^/]+))");
        private static Regex page_nav_regex = new Regex(@"/pictures/user/[^/]+/page/(\d+)");

        private string address_root;
        private string query_root;
        private string album_name;

        public override bool RequiresLogin
        {
            get
            {
                if (SuperWebClient.HasValidCookiesForDomain(new Uri("http://www.hentai-foundry.com/")))
                {
                    return false;
                }
                return true;
            }
        }

        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
        }

        public HentaiFoundryMediaSource(Uri url)
            : base(url) {
            this.WebRequestWaitTime = 200;
            this.LoginURL = @"http://www.hentai-foundry.com/";

            if (!ValidateUrl(url)) {
                throw new Exception("Hentai Foundry URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            address_root = address_matches[0].Groups[2].Value;
            query_root = address_matches[0].Groups[1].Value;
            album_name = address_matches[0].Groups[3].Value;

        }

        public override string getFolderNameFromURL(Uri url)
        {
            if (!address_regex.IsMatch(url.ToString()))
            {
                throw new Exception("Hentai Foundry URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string album_name = address_matches[0].Groups[3].Value;
            return album_name;
        }


        private static int GetHighestPageNumber(string page_contents) {
            int total_pages = -1;
            MatchCollection matches = page_nav_regex.Matches(page_contents);
            foreach (Match match in matches) {
                int test = -1;

                string value = match.Groups[1].Value;
                if (Int32.TryParse(value, out test)) {
                    if (test > total_pages) {
                        total_pages = test;
                    }
                }
            }
            if(total_pages<1)
            {
                return 1;
            }
            return total_pages;
        }

        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
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

        private MediaSourceResults GetGalleryPages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();
            bool new_max_found = true;
            int total_pages = 0;

            string test_url = url.ToString();

            while (new_max_found) {
                new_max_found = false;

                int test = GetHighestPageNumber(page_contents);
                if (test > total_pages) {
                    total_pages = test;
                    new_max_found = true;
                    test_url = query_root + @"/page/" + total_pages;
                    page_contents = GetPageContents(new Uri(test_url));
                }
            }


            //(.+)/post/list/([^/]+/)?(\d+)
            for (int i = 1; i <= total_pages; i++) {

                test_url = query_root + @"/page/" + i.ToString();

                output.Add(new MediaSourceResult(new Uri(test_url),null, this.url, this, MediaResultType.DownloadSource, "gallery"));
            }
            return output;

        }


        private MediaSourceResults GetImagePages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);


            HtmlNodeCollection imagePageNodes = doc.DocumentNode.SelectNodes("//img[@class='thumb']");

            foreach (HtmlNode node in imagePageNodes) {
                HtmlNode linkNode = node.ParentNode;
                if (linkNode.Name.ToLower() != "a") {
                    throw new Exception("Found a thumbnail without a link");
                }

                if (String.IsNullOrWhiteSpace(linkNode.Attributes["href"].Value))
                    throw new Exception("No href found");

                string image_page = linkNode.Attributes["href"].Value;

                if (!image_page.Contains(album_name))
                    continue;

                Uri link = new Uri(address_root + image_page);

                output.Add(new MediaSourceResult(link, page_url, this.url, this, MediaResultType.DownloadSource, "image"));
            }

            return output;
        }

        private MediaSourceResults GetImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();
            HtmlDocument imageDoc = new HtmlDocument();

                imageDoc.LoadHtml(page_contents);

            HtmlNode imageNode = imageDoc.DocumentNode.SelectSingleNode("//div[@class='container']//div[@class='boxbody']//img");
            HtmlNode embedNode = imageDoc.DocumentNode.SelectSingleNode("//embed");


            if (imageNode!=null)
                {
                    String value = imageNode.Attributes["src"].Value;
                    if(value.StartsWith(@"//"))
                    {
                        value = value.Replace(@"//", "http://");
                    }
                    output.Add(new MediaSourceResult(new Uri(value), page_url, this.url, this, MediaResultType.Download));
                } else {
                    throw new Exception("Image node not found");
                }

            if(embedNode != null) {
                String value = embedNode.Attributes["src"].Value;
                if (value.StartsWith(@"//")) {
                    value = value.Replace(@"//", "http://");
                }
                output.Add(new MediaSourceResult(new Uri(value), page_url, this.url, this, MediaResultType.Download));

            }
            return output;
        }



    }
}
