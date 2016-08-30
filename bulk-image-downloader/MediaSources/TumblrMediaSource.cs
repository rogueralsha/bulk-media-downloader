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
    public class TumblrMediaSource : AMediaSource
    {
        private readonly static Regex root_name = new Regex(@"https?://(([^""/]+)\.com)/archive/");
        private readonly static Regex next_page_regex = new Regex(@"href=""(/archive/\?before_time=\d+)""");

        private readonly static Regex post_regex = new Regex(@"href=""(https?://[^""/]+\.com/post/[^""]+)""");
        //private readonly static Regex post_type_regex = new Regex(@"<meta property=""og:type"" content=""([^""]+)""");

        private readonly static Regex image_page_regex = new Regex(@"href=""(https?://[^""/]+\.com/image/[^\""]+)\""");

        private readonly static Regex video_iframe_regex = new Regex(@"<iframe src=[""']([^'""]+)[""'] style=[""'][^'""]+[""'] class='[^'""]+tumblr_video[^'""]+['""]");
        private readonly static Regex video_source_regex = new Regex(@"<source src=""(https?://[^""/]+\.com/video_file/[^\""]+)\""");

        private readonly static Regex instagram_embed_regex = new Regex(@"instagram\.com/[^/]+/[^/]+/embed/");

        private readonly static Regex redirect_regex = new Regex(@"https?://t\.umblr\.com/redirect\?z=(.+?)&amp;t=[^""]");

        //private readonly static Regex meta_og_image_regex = new Regex(@" meta property=""og:image"" content=""([^""]+)""");

        private readonly static Regex tumblr_image_src_regex =
            new Regex(@"data-src=""(https?://[^.]+\.media\.tumblr\.com/[^\""]+)\""");
        private readonly static Regex tumblr_image_regex =
                    new Regex(@"https?://[^.]+\.media\.tumblr\.com/[^/]+/tumblr_[^_]+_(\d+)\.");


        private readonly static Regex reblog_regex = new Regex(@"<div id=""info"">\s*reblogged");

        //http://40.media.tumblr.com/56243bb836a65a6a9526f17e2400b77a/tumblr_nrleothyHS1u6rxu8o1_1280.png
        private string address_root;
        private string album_name;


        public static bool ValidateUrl(Uri url) {
            return root_name.IsMatch(url.ToString());
        }

        public TumblrMediaSource(Uri url)
            : base(url)
        {

            if (!ValidateUrl(url))
            {
                throw new Exception("Tumblr URL not understood");
            }
            MatchCollection address_matches = root_name.Matches(url.ToString());
            address_root = address_matches[0].Groups[1].Value;
            album_name = address_matches[0].Groups[2].Value;
        }


        public override string getFolderNameFromURL(Uri url)
        {
            if (!root_name.IsMatch(url.ToString()))
            {
                throw new Exception("Tumblr URL not understood");
            }
            MatchCollection address_matches = root_name.Matches(url.ToString());
            string album_name = address_matches[0].Groups[2].Value;
            return album_name;
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


        private MediaSourceResults GetGalleryPages(Uri page_url, String page_contents)
        {
            MediaSourceResults output = new MediaSourceResults();
            Queue<Uri> to_check = new Queue<Uri>();

            string test_url = url.ToString();


            output.Add(new MediaSourceResult(url, null, this.url,this, MediaResultType.DownloadSource,"gallery"));
            MatchCollection mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
            while (true)
            {
                foreach (Match m in mc)
                {
                    String tmp = "http://" + address_root + m.Groups[1].Value;

                    Uri uri = new Uri(tmp);
                    output.Add(new MediaSourceResult(uri, null, this.url, this, MediaResultType.DownloadSource, "gallery"));
                    to_check.Enqueue(uri);
                }

                if (to_check.Count > 0)
                {
                    Uri next_page = to_check.Dequeue();
                    page_contents = GetPageContents(next_page);
                    mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
                }
                else
                {
                    break;
                }
            }

            already_checked = new List<string>();
            return output;

        }

        private List<String> already_checked = new List<string>();

        private MediaSourceResults GetImagePages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            MatchCollection mc = post_regex.Matches(page_contents);

            foreach (Match m in mc)
            {
                string post_url = m.Groups[1].Value;
                if (!post_url.Contains(address_root))
                {
                    continue;
                }
                if (already_checked.Contains(post_url))
                {
                    continue;
                }
                if (post_url.Contains("/rss"))
                    continue;

                already_checked.Add(post_url);

                output.Add(new MediaSourceResult(new Uri(post_url), page_url, this.url, this, MediaResultType.DownloadSource, "image"));
            }
            return output;

        }

        private MediaSourceResults GetImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();   

            if (reblog_regex.IsMatch(page_contents) ||
                page_contents.Contains("class=\"reblogged-from\"") ||
                page_contents.Contains("<a class=\"tumblr_blog\" href=\""))
                return output;

            List<Uri> image_urls = new List<Uri>();


            // Get all redirect links first before checking post type, since any post could have a link
            foreach (Match redirect_match in redirect_regex.Matches(page_contents)) {
                try {
                    String redirect_string = Uri.UnescapeDataString(redirect_match.Groups[1].Value);
                    Uri redirect_url = new Uri(redirect_string);
                    if (isMediaFile(redirect_url.ToString())) {
                        output.Add(new MediaSourceResult(redirect_url, null, this.url, this, MediaResultType.Download));
                        sendStatus("Redirect URL found: " + redirect_url.ToString());
                    } else {
                        output.AddRange(getHostedMedia(redirect_url));
                    }
                } catch (Exception e) {
                    Console.Out.WriteLine(e.Message);
                }
            }

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(page_contents);
            HtmlNode typeNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:type']");
            String postType;
            if (typeNode == null) {
                //throw new Exception("Cannot find post type for page: " + post_url);
                postType = "tumblr-feed:photo";
            } else {
                postType = typeNode.Attributes["content"].Value;
            }


            switch (postType) {
                case "tumblr-feed:entry":
                case "tumblr-feed:conversation":
                case "tumblr-feed:quote":
                    // Text post; may need to do manual post inspection
                    sendStatus("Text post, skipping: " + page_url);
                    return output;
                case "tumblr-feed:audio":
                    // Text post; may need to do manual post inspection
                    sendStatus("Audio post, skipping: " + page_url);
                    return output;
                case "tumblr-feed:link":
                    // Text post; may need to do manual post inspection
                    sendStatus("Link post, skipping: " + page_url);
                    return output;
                case "tumblr-feed:photo":
                case "tumblr-feed:photoset":
                case "tumblr-feed:panorama":
                case "article":
                    HtmlNodeCollection ogImageNodes = htmlDoc.DocumentNode.SelectNodes("//meta[@property='og:image']");
                    if (ogImageNodes != null) {
                        foreach (HtmlNode ogImageNode in ogImageNodes) {
                            string download_link = WebUtility.HtmlDecode(ogImageNode.Attributes["content"].Value);
                            if (download_link.Contains("avatar"))
                                continue;
                            image_urls.Add(new Uri(download_link));
                        }
                    }
                    break;
                case "tumblr-feed:video":
                    if (page_contents.Contains("youtube_iframe") ||
                        page_contents.Contains("www.youtube.com/embed/")) {
                        sendStatus("Youtube video, skipping: " + page_url);
                        return output;
                    }
                    if (instagram_embed_regex.IsMatch(page_contents)) {
                        sendStatus("Instagram video, skipping: " + page_url);
                        return output;
                    }
                    if (page_contents.Contains("player.vimeo.com")) {
                        sendStatus("Vimeo video, skipping: " + page_url);
                        return output;
                    }
                    if (!video_iframe_regex.IsMatch(page_contents)) {
                        sendStatus("Unable to find video frame, skipping: " + page_url);
                        return output;
                    }
                    string video_page_url = video_iframe_regex.Match(page_contents).Groups[1].Value;
                    string video_page_contents = GetPageContents(new Uri(video_page_url));
                    if (!video_source_regex.IsMatch(video_page_contents)) {
                        sendStatus("Unable to find video source, skipping: " + page_url);
                        return output;
                    }

                    foreach (Match vm in video_source_regex.Matches(video_page_contents)) {
                        string video_url = vm.Groups[1].Value;
                        image_urls.Add(new Uri(video_url));

                    }
                    break;
                default:
                    throw new NotSupportedException("Tumblr post type " + typeNode.Attributes["content"].Value + " is not supported");
            }



            //MatchCollection pmc = null;
            //if (image_page_regex.IsMatch(post_page_contents)) {
            //    pmc = image_page_regex.Matches(post_page_contents);
            //    foreach (Match pm in pmc) {
            //        string image_page_url = WebUtility.HtmlDecode(pm.Groups[1].Value);
            //        string image_page_contents = GetPageContents(new Uri(image_page_url));

            //        if (tumblr_image_src_regex.IsMatch(image_page_contents)) {
            //            MatchCollection ipmc = tumblr_image_src_regex.Matches(image_page_contents);
            //            foreach (Match ipm in ipmc) {
            //                string download_link = WebUtility.HtmlDecode(ipm.Groups[1].Value);
            //                image_urls.Add(new Uri(download_link));
            //            }
            //        }
            //    }
            //}
            //if (tumblr_image_src_regex.IsMatch(post_page_contents)) {
            //    pmc = tumblr_image_src_regex.Matches(post_page_contents);
            //    foreach (Match pm in pmc) {
            //        string download_link = WebUtility.HtmlDecode(pm.Groups[1].Value);
            //        image_urls.Add(new Uri(download_link));
            //    }
            //}



            if (image_urls.Count == 0) {
                string temp = Path.GetTempFileName();
                System.IO.File.WriteAllLines(temp, page_contents.Split('\n'));
                sendStatus("Media URL not found on " + page_url);
                return output;
            }

            foreach (Uri uri in image_urls) {
                if (tumblr_image_regex.IsMatch(uri.ToString())) {
                    Match image_m = tumblr_image_regex.Match(uri.ToString());
                    String res_string = image_m.Groups[1].Value;
                    int res = int.Parse(res_string);
                    if (res < 1280) {
                        StringBuilder new_uri_string = new StringBuilder(uri.ToString());
                        new_uri_string.Remove(image_m.Groups[1].Index, image_m.Groups[1].Length);
                        new_uri_string.Insert(image_m.Groups[1].Index, "1280");
                        Uri new_uri = new Uri(new_uri_string.ToString());
                        try {
                            this.GetHeaders(new_uri, page_url);
                            output.Add(new MediaSourceResult(new_uri, page_url, this.url, this, MediaResultType.Download));
                            continue;
                        } catch (WebException ex) {
                            Console.Out.WriteLine(ex.Message);
                        }
                    }
                }

                output.Add(new MediaSourceResult(uri, page_url, this.url, this, MediaResultType.Download));
            }

            return output;
        }
        

    }
}
