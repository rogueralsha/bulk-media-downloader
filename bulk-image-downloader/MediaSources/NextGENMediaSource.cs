using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BulkMediaDownloader.MediaSources
{
    public class NextGENMediaSource: AMediaSource {
        public NextGENMediaSource(Uri url)
            : base(url) {

        }

        private static Regex page_nav_regex = new Regex(@"(http.+?/gallery/page/)(\d+)");

        private static Regex images_regex = new Regex(@"['""](http.+?/wp-content/gallery/[^/]+/[^'""/]+)['""]");

        public override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url) { return null; }
        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents) { return null; }

        protected void ProcessImages() {
            string page_contents = GetPageContents(url);

            if (!Properties.Settings.Default.DetectAdditionalPages || !page_nav_regex.IsMatch(page_contents)) {
                DetectImages(new Uri(this.url.ToString()));
            } else {
                MatchCollection matches = page_nav_regex.Matches(page_contents);

                string query_root = matches[0].Groups[1].Value;

                bool new_max_found = true;
                int total_pages = 0;

                string test_url = url.ToString();

                while (new_max_found) {
                    IfPausedWaitUntilUnPaused();
                    string primer_page = GetPageContents(new Uri(test_url));
                    new_max_found = false;

                    int test = GetHighestPageNumber(primer_page);
                    if (test > total_pages) {
                        total_pages = test;
                        new_max_found = true;
                        test_url = query_root + total_pages;
                    }
                }


                //(.+)/post/list/([^/]+/)?(\d+)
                for (int i = 1; i <= total_pages; i++) {
                    IfPausedWaitUntilUnPaused();

                    test_url = query_root + i.ToString();

                    DetectImages(new Uri(test_url));
                }
            }


        }

        private void DetectImages(Uri page_url) {
            string page_contents = GetPageContents(page_url);
            MatchCollection image_matches = images_regex.Matches(page_contents);
            List<string> found_urls = new List<string>();

            foreach (Match image_match in image_matches) {
                IfPausedWaitUntilUnPaused();

                GroupCollection groups = image_match.Groups;
                Group group = groups[0];

                if (!found_urls.Contains(image_match.Groups[1].Value)) {
                    found_urls.Add(image_match.Groups[1].Value);
                    //DownloadManager.DownloadImage(new Uri(image_match.Groups[1].Value), this.download_dir, page_url.ToString());
                }

            }
            found_urls.Clear();
        }


        private static int GetHighestPageNumber(string page_contents) {
            int total_pages = -1;
            MatchCollection matches = page_nav_regex.Matches(page_contents);
            foreach (Match match in matches) {
                int test = -1;

                string value = match.Groups[2].Value;
                if (Int32.TryParse(value, out test)) {
                    if (test > total_pages) {
                        total_pages = test;
                    }
                }
            }
            return total_pages;
        }
    }
}
