using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace BulkMediaDownloader.MediaSources
{
    public class ShimmieMediaSource : AMediaSource {

        private static Regex address_regex = new Regex(@"((.+)/post/list/([^/]+)/?)(\d+)");
        private static Regex page_nav_regex = new Regex(@"/post/list/[^/]+/(\d+)");
        private static Regex images_regex = new Regex("class='[^'\"]+' href='(/post/view/[\\d]+)'");
        private static Regex image_regex = new Regex("https?://.+/_images/[^'\"]+");

        private static Regex image_only_regex = new Regex("<a href=['\"](.+_images.+)['\"]>Image Only");

        private string address_root;
        private string query_root;
        private string album_name;

        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
        }

        public ShimmieMediaSource(Uri url)
            : base(url) {


            if (!ValidateUrl(url)) {
                throw new UrlNotRecognizedException("Shimmie URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            address_root = address_matches[0].Groups[2].Value;
            query_root = address_matches[0].Groups[1].Value;
            album_name= address_matches[0].Groups[3].Value;

        }


        public override string getFolderNameFromURL(Uri url)
        {
            if (!address_regex.IsMatch(url.ToString()))
            {
                throw new UrlNotRecognizedException("Shimmie URL not understood");
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
            return total_pages;
        }


        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return GetGalleryPages(url, page_contents);
                case "gallery":
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
                    test_url = query_root + total_pages;
                    page_contents = GetPageContents(new Uri(test_url));
                }
            }


            //(.+)/post/list/([^/]+/)?(\d+)
            for (int i = 1; i <= total_pages; i++) {
                test_url = query_root + i.ToString();

                output.Add(new MediaSourceResult(new Uri(test_url),null, this.url, this, MediaResultType.DownloadSource, "gallery"));
            }
            return output;

        }


        private MediaSourceResults GetImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            MatchCollection image_matches = image_only_regex.Matches(page_contents);
            foreach (Match image_match in image_matches)
            {
                GroupCollection groups = image_match.Groups;
                Group group = groups[0];

                string image = image_match.Groups[1].Value;

                if (image_regex.IsMatch(image))
                {
                    output.Add(new MediaSourceResult(new Uri(image), page_url, this.url, this, MediaResultType.Download));
                }
            }
            return output;

            //image_matches = images_regex.Matches(page_contents);
            //foreach (Match image_match in image_matches) {
            //    IfPausedWaitUntilUnPaused();

            //    GroupCollection groups = image_match.Groups;
            //    Group group = groups[0];

            //    System.Threading.Thread.Sleep(1000);

            //    string page_content = GetPageContents(new Uri(address_root + image_match.Groups[1].Value));

            //    if (image_regex.IsMatch(page_content)) {
            //        String value = image_regex.Match(page_content).Value;
            //        output.Add(new Uri(value));
            //    }
            //}
            //return output;
        }



    }
}
