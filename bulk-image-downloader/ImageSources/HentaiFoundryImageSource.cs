﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace bulk_image_downloader.ImageSources {
    public class HentaiFoundryImageSource : AImageSource {
        //http://www.hentai-foundry.com/pictures/user/GENSHI
        private static Regex address_regex = new Regex(@"((.+)/pictures/user/([^/]+))");
        private static Regex page_nav_regex = new Regex(@"/pictures/user/[^/]+/page/(\d+)");
        private static Regex image_link_regex = new Regex(@"/pictures/user/[^/]+/(\d+)/[^""]+");
        private static Regex image_download_regex = new Regex(@"//pictures.hentai-foundry.com/[^/]+/[^/]+/(\d+)/[^""]+");

        private string address_root;
        private string query_root;
        private string album_name;

        private static CachedCookies cached_cookies = null;
        public override CachedCookies StarterCookies
        {
            get
            {
                return cached_cookies;
            }

             set
            {
                cached_cookies = value;
            }
        }


        public override bool RequiresLogin
        {
            get
            {
                if(cached_cookies!=null&&!cached_cookies.Expired)
                {
                    return false;
                }
                return true;
            }
        }

        public HentaiFoundryImageSource(Uri url)
            : base(url) {
            this.LoginURL = @"http://www.hentai-foundry.com/";

            if (!address_regex.IsMatch(url.ToString())) {
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
            album_name = address_matches[0].Groups[3].Value;
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
                    test_url = query_root + @"/page/" + total_pages;
                    System.Threading.Thread.Sleep(1000);
                    page_contents = GetPageContents(new Uri(test_url));
                }
            }


            //(.+)/post/list/([^/]+/)?(\d+)
            for (int i = 1; i <= total_pages; i++) {
                IfPausedWaitUntilUnPaused();

                test_url = query_root + @"/page/" + i.ToString();

                output.Add(new Uri(test_url));
            }
            return output;

        }


        protected override List<Uri> GetImagesFromPage(String page_contents) {
            List<Uri> output = new List<Uri>();

            List<Uri> image_pages = new List<Uri>();

            MatchCollection image_matches = image_link_regex.Matches(page_contents);
            foreach (Match image_match in image_matches)
            {
                IfPausedWaitUntilUnPaused();

                GroupCollection groups = image_match.Groups;
                Group group = groups[0];

                string image_page = image_match.Value;

                if (!image_page.Contains(album_name))
                    continue;

                Uri link = new Uri(address_root + image_page);
                if (!image_pages.Contains(link))
                {

                    image_pages.Add(link);
                }
            }

            foreach(Uri image_page in image_pages)
            {
                IfPausedWaitUntilUnPaused();

                System.Threading.Thread.Sleep(200);

                string page_content = GetPageContents(image_page);

                if (image_download_regex.IsMatch(page_content))
                {
                    String value = image_download_regex.Match(page_content).Value;
                    if(value.StartsWith(@"//"))
                    {
                        value = value.Replace(@"//", "http://");
                    }
                    output.Add(new Uri(value));
                }

            }
            return output;

        }



    }
}