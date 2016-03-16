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
    public class DeviantArtImageSource : AImageSource {
        private static Regex root_name = new Regex("http://([^/]+)/gallery/");
        private static Regex next_page_regex = new Regex("href=\"(/gallery/?.*?offset=(\\d+)[^\"]*)\"");
        private static Regex image_link_regex = new Regex("http://[^/]+/art/([^\"#]+)");


        // This matches against the target of the download button. This is the preffered image.
        private static Regex download_url_regex = new Regex("data-download_url=\"([^\"]+)\"");
        // This matches against the data that powers the "full image" when you click on an image
        private static Regex full_image_regex = new Regex("<img.+src=\"([^\"]+)\".+class=\"dev-content-full[ ]?\"", RegexOptions.Singleline);

        // This matches against the og:image meta tag, not used since hte above options provide better quality matches        
        private static Regex original_image_regex = new Regex("<meta property=\"og: image\" content=\"([^\"]+)\" > ");
        // This matches against the img element that contains the full image, the above data link is better
        //private static Regex full_image_regex = new Regex("<img.+src=\"([^\"]+)\" + (.||\\s) +class=\"dev-art-full\\s?\">");


        private string address_root;
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
                if (cached_cookies != null && !cached_cookies.Expired)
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
                    System.Threading.Thread.Sleep(1000);
                    Uri next_page = to_check.Dequeue();
                    page_contents = GetPageContents(next_page);
                    mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
                } else
                {
                    break;
                }
            }

            

            return candidates.Values.ToList<Uri>();

        }


        protected override List<Uri> GetImagesFromPage(String page_contents) {
            List<Uri> output = new List<Uri>();
            List<String> already_checked = new List<String>();

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
                System.Threading.Thread.Sleep(1000);
                String image_page_contents = GetPageContents(new Uri(m.Value));

                Match im = null;
                String image_url = null;
                if (download_url_regex.IsMatch(image_page_contents))
                {
                    im = download_url_regex.Match(image_page_contents);
                    // The download links point to a page that redirects to another page. We're just going to go ahead and get that redirect.
                    HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(im.Groups[1].Value);
                    req.MaximumAutomaticRedirections = 5;
                    req.Method = "HEAD";
                    req.Accept = "*/*";
                    req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36";
                    req.Headers.Add("Accept-Encoding", "gzip, deflate, sdch");
                    req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                    req.Headers.Add("Upgrade-Insecure-Requests", "1");
                    req.Referer = m.Value;
                    if (!String.IsNullOrWhiteSpace(this.lastSetCookie))
                    {
                        CookieContainer cc = new CookieContainer();
                        string[] cookie_sections = this.lastSetCookie.Split(';');
                        Dictionary<String, String> cookie_arge = new Dictionary<string, string>();
                        foreach(String cookie_section in cookie_sections)
                        {
                            cookie_arge.Add(cookie_section.Split('=')[0].Trim(), cookie_section.Split('=')[1].Trim());
                        }
                        cc.Add(new Uri("http://deviantart.com"), new Cookie("userinfo",cookie_arge["userinfo"]));
                    }

                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                    image_url = res.ResponseUri.AbsoluteUri;
                    res.Close();
                }
                else  if (full_image_regex.IsMatch(image_page_contents))
                {
                    im = full_image_regex.Match(image_page_contents);
                    image_url = im.Groups[1].Value;
                } else
                {
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
