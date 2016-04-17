using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BulkMediaDownloader.ImageSources {
    public class FlickrImageSource: AImageSource {
        private static Regex PagesRegex = new Regex(@"href=""(/photos/[^/]+/page(\d+/))""");
        private static Regex ImageRegex = new Regex(@"""url"":""(https:\\/\\/[^\.]+.staticflickr.com\\/\d+\\/+\d+\\/((\d+)_[a-z0-9]+_([a-z]).([a-z]+)))""");

        public FlickrImageSource(Uri url)
            : base(url) {
        }

        protected override List<Uri> GetImagesFromPage(Uri page_url, String page_contents) { return null; }
        protected override List<Uri> GetPages(Uri page_url, String page_contents) { return null; }

        private int GetHighestPageNumber(string page_contents) {
            int highest_number = 0;

            MatchCollection matches = PagesRegex.Matches(page_contents);
            foreach (Match match in matches) {
                int test = -1;

                string value = match.Groups[1].Value;
                if (Int32.TryParse(value, out test)) {
                    if (test > highest_number) {
                        highest_number = test;
                    }
                }
            }
            return highest_number;
        }

        protected void ProcessImages() {
            //if (!address_regex.IsMatch(url.ToString())) {
            //    throw new Exception("Shimmie URL not understood");
            //}



            //bool new_max_found = true;
            //int total_pages = 0;

            //string test_url = url.ToString();

            //string flickr_root = "https://www.flickr.com";

            //while (new_max_found) {
            //    IfPausedWaitUntilUnPaused();
            //    string primer_page = GetWebPageContents(new Uri(test_url));
            //    new_max_found = false;

            //    int test = GetHighestPageNumber(primer_page);
            //    if (test > total_pages) {
            //        total_pages = test;
            //        new_max_found = true;
            //        test_url = flickr_root + total_pages;
            //    }
            //}


            ////(.+)/post/list/([^/]+/)?(\d+)
            //for (int i = 1; i <= total_pages; i++) {
            //    IfPausedWaitUntilUnPaused();

            //    test_url = query_root + i.ToString();
            //    string page = DownloadManager.GetWebPageContents(new Uri(test_url));
            //    MatchCollection image_matches = images_regex.Matches(page);
            //    foreach (Match image_match in image_matches) {
            //        IfPausedWaitUntilUnPaused();

            //        GroupCollection groups = image_match.Groups;
            //        Group group = groups[0];

            //        string page_content = DownloadManager.GetWebPageContents(new Uri(address_root + image_match.Groups[1].Value));

            //        if (image_regex.IsMatch(page_content)) {
            //            DownloadManager.DownloadImage(new Uri(image_regex.Match(page_content).Value));
            //        }
            //    }
            //}

        }
    }
}
