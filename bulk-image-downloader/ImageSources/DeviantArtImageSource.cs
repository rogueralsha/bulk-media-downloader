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

namespace bulk_image_downloader.ImageSources {
    public class DeviantArtImageSource : AImageSource {
        private readonly static Regex root_name = new Regex("http://(([^.]+)\\.deviantart\\.com)/gallery/");
        private readonly static Regex next_page_regex = new Regex("href=\"(/gallery/?.*?offset=(\\d+)[^\"]*)\"");
        private readonly static Regex image_link_regex = new Regex("http://[^/]+/art/([^\"#]+)");


        // This matches against the target of the download button. This is the preffered image.
        private static Regex download_url_regex = new Regex("data-download_url=\"([^\"]+)\"");
        // This matches against the data that powers the "full image" when you click on an image
        private readonly static Regex full_image_regex = new Regex("<img.+?src=\"(http://(orig|img)[^\"]+)\"[\\s\\S]+class=\"dev-content-full[ ]?\">", RegexOptions.Singleline);

        private readonly static Regex journal_regex = new Regex(@"class=""journal journal-green journalcontrol free-literature""");

        // This matches against the og:image meta tag, not used since hte above options provide better quality matches        
        private readonly static Regex original_image_regex = new Regex("<meta property=\"og: image\" content=\"([^\"]+)\" > ");
        // This matches against the img element that contains the full image, the above data link is better
        //private static Regex full_image_regex = new Regex("<img.+src=\"([^\"]+)\" + (.||\\s) +class=\"dev-art-full\\s?\">");


        private string address_root;
        private string album_name;

        public override bool RequiresLogin
        {
            get
            {
                if (SuperWebClient.HasValidCookiesForDomain(new Uri("http://deviantart.com")))
                {
                    return false;
                }
                return true;
            }
        }


        public DeviantArtImageSource(Uri url)
            : base(url) {

            this.LoginURL = @"http://www.deviantart.com/";
            if (!root_name.IsMatch(url.ToString())) {
                throw new Exception("DeviantArt URL not understood");
            }
            MatchCollection address_matches = root_name.Matches(url.ToString());
            address_root = address_matches[0].Groups[1].Value;
            album_name = address_matches[0].Groups[2].Value;
        }


        public override string getFolderNameFromURL(Uri url)
        {
            if (!root_name.IsMatch(url.ToString()))
            {
                throw new Exception("DeviantArt URL not understood");
            }
            MatchCollection address_matches = root_name.Matches(url.ToString());
            string album_name = address_matches[0].Groups[2].Value;
            return album_name;
        }
        protected override List<Uri> GetPages(String page_contents) {
            SortedDictionary<int, Uri> candidates = new SortedDictionary<int, Uri>();
            Queue<Uri> to_check = new Queue<Uri>();

            string test_url = url.ToString();

            MatchCollection mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
            while(true)
            {
                foreach (Match m in mc)
                {
                    String tmp = "http://" + address_root + m.Groups[1].Value;
                    if (!tmp.Contains("catpath")) {
                        continue;
                    }
                    Uri uri = new Uri(tmp);
                    int offset = int.Parse(m.Groups[2].Value);
                    if (!candidates.ContainsValue(uri)&&!candidates.ContainsKey(offset))
                    {
                        candidates.Add(offset, uri);
                        to_check.Enqueue(uri);
                    }
                }

                if(to_check.Count > 0)
                {
                    System.Threading.Thread.Sleep(100);
                    Uri next_page = to_check.Dequeue();
                    page_contents = GetPageContents(next_page);
                    mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
                } else
                {
                    break;
                }
            }



            already_checked = new List<string>();
            return candidates.Values.ToList<Uri>();

        }


        private List<String> already_checked = new List<string>();

        protected override List<Uri> GetImagesFromPage(String page_contents) {
            List<Uri> output = new List<Uri>();

            MatchCollection mc = image_link_regex.Matches(page_contents);
            foreach(Match m in mc)
            {
                if(!m.Value.Contains(address_root))
                {
                    continue;
                }
                if(already_checked.Contains(m.Value))
                {
                    continue;
                }
                already_checked.Add(m.Value);
                System.Threading.Thread.Sleep(100);
                String image_page_contents = GetPageContents(new Uri(m.Value));

                Match im = null;
                String image_url = null;
                if (download_url_regex.IsMatch(image_page_contents))
                {
                    im = download_url_regex.Match(image_page_contents);
                    string download_link = WebUtility.HtmlDecode(im.Groups[1].Value);
                    image_url = TheWebClient.GetRedirectURL(new Uri(download_link), m.Value).ToString();
                }
                else if (full_image_regex.IsMatch(image_page_contents))
                {
                    im = full_image_regex.Match(image_page_contents);
                    image_url = im.Groups[1].Value;
                } else if(journal_regex.IsMatch(image_page_contents))
                {
                    // This page is a journal entry, no image to download!
                    continue;
                } else
                {
                    string temp = Path.GetTempFileName();
                    System.IO.File.WriteAllLines(temp, image_page_contents.Split('\n'));
                    throw new Exception("Image URL not found on " + m.Value);
                }

                Uri uri = new Uri(image_url);
                if(!output.Contains(uri))
                {
                    output.Add(uri);
                }

            }


            return output;
            
        }


    }
}
