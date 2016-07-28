﻿using System;
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

namespace BulkMediaDownloader.MediaSources {
    public class DeviantArtMediaSource : AMediaSource {
        private readonly static Regex root_name = new Regex("http://(([^.]+)\\.deviantart\\.com)/gallery/");
        private readonly static Regex next_page_regex = new Regex("href=\"(/gallery/?.*?offset=(\\d+)[^\"]*)\"");
        private readonly static Regex image_link_regex = new Regex("http://[^/]+/art/([^\"#]+)");


        // This matches against the data that powers the "full image" when you click on an image
        private readonly static Regex full_image_regex = new Regex("<img.+?src=\"(http://(orig|img)[^\"]+)\"[\\s\\S]+class=\"dev-content-full[ ]?\">", RegexOptions.Singleline);

        private readonly static Regex journal_regex = new Regex(@"class=""journal journal-green journalcontrol free-literature""");
        private readonly static Regex flash_reged = new Regex(@"<iframe class=""flashtime"" src=""([^""]+)""");
        // This matches against the og:image meta tag, not used since hte above options provide better quality matches        
        private readonly static Regex original_image_regex = new Regex("<meta property=\"og: image\" content=\"([^\"]+)\" > ");
        // This matches against the img element that contains the full image, the above data link is better
        //private static Regex full_image_regex = new Regex("<img.+src=\"([^\"]+)\" + (.||\\s) +class=\"dev-art-full\\s?\">");


        private string address_root;
        private string album_name;

        public override bool RequiresLogin {
            get {
                if (SuperWebClient.HasValidCookiesForDomain(new Uri("http://deviantart.com"))) {
                    return false;
                }
                return true;
            }
        }


        public DeviantArtMediaSource(Uri url)
            : base(url) {
            // Deviantart is difficult. We retry a lot, and we wait a lot.
            this.WebRequestWaitTime = 2000;
            this.WebRequestErrorAdditionalWaitTime = 20000; //Seriously, sometimes this isn't even enough
            this.WebRequestRetryCount = 10;

            this.LoginURL = @"http://www.deviantart.com/";
            if (!root_name.IsMatch(url.ToString())) {
                throw new Exception("DeviantArt URL not understood");
            }
            MatchCollection address_matches = root_name.Matches(url.ToString());
            address_root = address_matches[0].Groups[1].Value;
            album_name = address_matches[0].Groups[2].Value;
        }


        public override string getFolderNameFromURL(Uri url) {
            if (!root_name.IsMatch(url.ToString())) {
                throw new Exception("DeviantArt URL not understood");
            }
            MatchCollection address_matches = root_name.Matches(url.ToString());
            string album_name = address_matches[0].Groups[2].Value;
            return album_name;
        }

        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents) {
            SortedDictionary<int, Uri> candidates = new SortedDictionary<int, Uri>();
            Queue<Uri> to_check = new Queue<Uri>();


            MatchCollection mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
            while (true) {
                foreach (Match m in mc) {
                    String tmp = "http://" + address_root + m.Groups[1].Value;
                    if (!tmp.Contains("catpath")) {
                        continue;
                    }
                    Uri uri = new Uri(tmp);
                    int offset = int.Parse(m.Groups[2].Value);
                    if (!candidates.ContainsValue(uri) && !candidates.ContainsKey(offset)) {
                        worker.ReportProgress(-1, "Found page: " + uri.ToString());
                        candidates.Add(offset, uri);
                        to_check.Enqueue(uri);
                    }
                }

                if (to_check.Count > 0) {
                    Uri next_page = to_check.Dequeue();
                    page_contents = GetPageContents(next_page, page_url);
                    mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
                } else {
                    break;
                }
            }



            already_checked = new List<string>();

            if (candidates.Count == 0)
                candidates.Add(0, page_url);

            HashSet<Uri> output = new HashSet<Uri>();

            foreach (Uri uri in candidates.Values) {
                worker.ReportProgress(-1, "Fetching image pages from " +uri.ToString());
                String imagePageContents = this.GetPageContents(uri);
                MatchCollection imc = image_link_regex.Matches(imagePageContents);
                foreach (Match m in imc) {
                    if (!m.Value.Contains(address_root)) {
                        continue;
                    }
                    if (already_checked.Contains(m.Value)) {
                        continue;
                    }
                    already_checked.Add(m.Value);
                    Uri image_page_url = new Uri(m.Value);
                    if (!output.Contains(image_page_url))
                        output.Add(image_page_url);
                }

            }
            return output;
        }


        private List<String> already_checked = new List<string>();

        public override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url) {
            String page_contents = this.GetPageContents(page_url);
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();
            String image_page_contents = GetPageContents(page_url, page_url);

            String image_url = null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(image_page_contents);
            //data-gmiclass="DownloadButton"
            HtmlNode downloadLinkNode =
                doc.DocumentNode.SelectSingleNode("//a[@data-gmiclass='DownloadButton']");

            if (downloadLinkNode != null) {

                string download_link = WebUtility.HtmlDecode(downloadLinkNode.Attributes["href"].Value);
                try {
                    image_url = this.GetRedirectURL(new Uri(download_link), page_url).ToString();
                } catch (WebException ex) {
                    if ((int)ex.Status >= 400 && (int)ex.Status < 500) {
                        throw ex;
                    } else {
                        this.worker.ReportProgress(-1, "Error while attempting to get download link");
                        this.worker.ReportProgress(-1, ex);
                        return output;
                    }
                }
            } else if (full_image_regex.IsMatch(image_page_contents)) {
                image_url = full_image_regex.Match(image_page_contents).Groups[1].Value;
            } else if (flash_reged.IsMatch(image_page_contents)) {
                image_url = flash_reged.Match(image_page_contents).Groups[1].Value;
            } else if (journal_regex.IsMatch(image_page_contents)) {
                // This page is a journal entry, no image to download!
                return output;
            } else {
                //string temp = Path.GetTempFileName();
                //System.IO.File.WriteAllLines(temp, image_page_contents.Split('\n'));
                this.worker.ReportProgress(-1, "Image URL not found on " + page_url.ToString());
                return output;
            }

            Uri uri = new Uri(image_url);
            worker.ReportProgress(-1, "Found image: " + uri.ToString());
            output.Add(new MediaSourceResult(uri, page_url, this.url));


            return output;

        }



    }
}
