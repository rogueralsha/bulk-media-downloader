using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace bulk_image_downloader.ImageSources {
    public class ShimmieImageSource : AImageSource {

        private static Regex address_regex = new Regex(@"((.+)/post/list/([^/]+)/?)(\d+)");
        private static Regex page_nav_regex = new Regex(@"/post/list/[^/]+/(\d+)");
        private static Regex images_regex = new Regex("class='[^'\"]+' href='(/post/view/[\\d]+)'");
        private static Regex image_regex = new Regex("http://.+/_images/[^'\"]+");

        private static Regex image_only_regex = new Regex("<a href=['\"](.+_images.+)['\"]>Image Only");

        private string address_root;
        private string query_root;
        private string album_name;

        public ShimmieImageSource(Uri url)
            : base(url) {


            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("Shimmie URL not understood");
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
            return total_pages;
        }

        protected override List<Uri> GetPages(String page_contents) {
            List<Uri> output = new List<Uri>();
            bool new_max_found = true;
            int total_pages = 0;

            string test_url = url.ToString();

            while (new_max_found) {
                IfPausedWaitUntilUnPaused();
                new_max_found = false;

                int test = GetHighestPageNumber(page_contents);
                if (test > total_pages) {
                    total_pages = test;
                    new_max_found = true;
                    test_url = query_root + total_pages;
                    System.Threading.Thread.Sleep(100);
                    page_contents = GetPageContents(new Uri(test_url));
                }
            }


            //(.+)/post/list/([^/]+/)?(\d+)
            for (int i = 1; i <= total_pages; i++) {
                IfPausedWaitUntilUnPaused();

                test_url = query_root + i.ToString();

                output.Add(new Uri(test_url));
            }
            return output;

        }


        protected override List<Uri> GetImagesFromPage(String page_contents) {
            List<Uri> output = new List<Uri>();

            MatchCollection image_matches = image_only_regex.Matches(page_contents);
            foreach (Match image_match in image_matches)
            {
                IfPausedWaitUntilUnPaused();

                GroupCollection groups = image_match.Groups;
                Group group = groups[0];

                string image = image_match.Groups[1].Value;

                if (image_regex.IsMatch(image))
                {
                    output.Add(new Uri(image));
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
