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
        private readonly static Regex root_name = new Regex("http://(([^.]+)\\.tumblr\\.com)/archive/");
        private readonly static Regex next_page_regex = new Regex(@"href=""(/archive/\?before_time=\d+)""");

        private readonly static Regex post_regex = new Regex(@"href=""(http://[^\.]+\.tumblr\.com/post/[^""]+)""");
        //private readonly static Regex post_type_regex = new Regex(@"<meta property=""og:type"" content=""([^""]+)""");

        private readonly static Regex image_page_regex = new Regex(@"href=""(http://[^.]+\.tumblr\.com/image/[^\""]+)\""");

        private readonly static Regex video_iframe_regex = new Regex(@"<iframe src=[""']([^'""]+)[""'] style=[""'][^'""]+[""'] class='[^'""]+tumblr_video[^'""]+['""]");
        private readonly static Regex video_source_regex = new Regex(@"<source src=""(https?://[^.]+\.tumblr\.com/video_file/[^\""]+)\""");

        private readonly static Regex instagram_embed_regex = new Regex(@"instagram\.com/[^/]+/[^/]+/embed/");

        private readonly static Regex redirect_regex = new Regex(@"https?://t\.umblr\.com/redirect\?z=(.+?)&amp;t=[^""]");

        //private readonly static Regex meta_og_image_regex = new Regex(@" meta property=""og:image"" content=""([^""]+)""");

        private readonly static Regex tumblr_image_src_regex = new Regex(@"data-src=""(http://[^.]+\.media\.tumblr\.com/[^\""]+)\""");

        private readonly static Regex reblog_regex = new Regex(@"<div id=""info"">\s*reblogged");

        //http://40.media.tumblr.com/56243bb836a65a6a9526f17e2400b77a/tumblr_nrleothyHS1u6rxu8o1_1280.png
        private string address_root;
        private string album_name;

        public TumblrMediaSource(Uri url)
            : base(url)
        {

            if (!root_name.IsMatch(url.ToString()))
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
        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents)
        {
            HashSet<Uri> candidates = new HashSet<Uri>();
            Queue<Uri> to_check = new Queue<Uri>();

            string test_url = url.ToString();


            candidates.Add(url);
            MatchCollection mc = next_page_regex.Matches(WebUtility.HtmlDecode(page_contents));
            while (true)
            {
                foreach (Match m in mc)
                {
                    String tmp = "http://" + address_root + m.Groups[1].Value;

                    Uri uri = new Uri(tmp);
                    if (!candidates.Contains(uri))
                    {
                        candidates.Add(uri);
                        to_check.Enqueue(uri);
                    }
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
            return candidates;

        }


        private List<String> already_checked = new List<string>();

        protected override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url, String page_contents)
        {
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

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

                already_checked.Add(post_url);
                if (post_url.Contains("messing"))
                    Console.Out.WriteLine();


                String post_page_contents;
                if (post_url.Contains("/rss"))
                    continue;

                try
                {
                    post_page_contents = GetPageContents(new Uri(post_url));
                }
                catch (WebException ex)
                {
                    worker.ReportProgress(-1, ex);
                    continue;
                }

                if (reblog_regex.IsMatch(post_page_contents) ||
                    post_page_contents.Contains("class=\"reblogged-from\"") ||
                    post_page_contents.Contains("<a class=\"tumblr_blog\" href=\""))
                    continue;

                List<Uri> image_urls = new List<Uri>();


                // Get all redirect links first before checking post type, since any post could have a link
                foreach (Match redirect_match in redirect_regex.Matches(post_page_contents))
                {
                    try
                    {
                        String redirect_string = Uri.UnescapeDataString(redirect_match.Groups[1].Value);
                        Uri redirect_url = new Uri(redirect_string);
                        if (isMediaFile(redirect_url.ToString()))
                        {
                            output.Add(new MediaSourceResult(redirect_url, null, this.url));
                            worker.ReportProgress(-1, "Redirect URL found: " + redirect_url.ToString());
                        }
                        else
                        {
                            foreach (Uri url in getHostedMedia(redirect_url))
                            {
                                output.Add(new MediaSourceResult(url, null, this.url));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Out.WriteLine(e.Message);
                    }
                }

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(post_page_contents);
                HtmlNode typeNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:type']");
                if (typeNode == null)
                {
                    throw new Exception("Cannot find post type for page: " + post_url);
                }


                switch (typeNode.Attributes["content"].Value)
                {
                    case "tumblr-feed:entry":
                    case "tumblr-feed:conversation":
                    case "tumblr-feed:quote":
                        // Text post; may need to do manual post inspection
                        this.worker.ReportProgress(-1, "Text post, skipping: " + post_url);
                        continue;
                    case "tumblr-feed:audio":
                        // Text post; may need to do manual post inspection
                        this.worker.ReportProgress(-1, "Audio post, skipping: " + post_url);
                        continue;
                    case "tumblr-feed:link":
                        // Text post; may need to do manual post inspection
                        this.worker.ReportProgress(-1, "Link post, skipping: " + post_url);
                        continue;
                    case "tumblr-feed:photo":
                    case "tumblr-feed:photoset":
                    case "tumblr-feed:panorama":
                    case "article":
                        HtmlNodeCollection ogImageNodes = htmlDoc.DocumentNode.SelectNodes("//meta[@property='og:image']");
                        if (ogImageNodes!=null)
                        {
                            foreach (HtmlNode ogImageNode in ogImageNodes)
                            {
                                string download_link = WebUtility.HtmlDecode(ogImageNode.Attributes["content"].Value);
                                if (download_link.Contains("avatar"))
                                    continue;
                                image_urls.Add(new Uri(download_link));
                            }
                        }
                        break;
                    case "tumblr-feed:video":
                        if (post_page_contents.Contains("youtube_iframe") ||
                            post_page_contents.Contains("www.youtube.com/embed/"))
                        {
                            this.worker.ReportProgress(-1, "Youtube video, skipping: " + post_url);
                            continue;
                        }
                        if (instagram_embed_regex.IsMatch(post_page_contents))
                        {
                            this.worker.ReportProgress(-1, "Instagram video, skipping: " + post_url);
                            continue;
                        }
                        if (post_page_contents.Contains("player.vimeo.com"))
                        {
                            this.worker.ReportProgress(-1, "Vimeo video, skipping: " + post_url);
                            continue;
                        }
                        if (!video_iframe_regex.IsMatch(post_page_contents))
                        {
                            this.worker.ReportProgress(-1, "Unable to find video frame, skipping: " + post_url);
                            continue;
                        }
                        string video_page_url = video_iframe_regex.Match(post_page_contents).Groups[1].Value;
                        string video_page_contents = GetPageContents(new Uri(video_page_url));
                        if (!video_source_regex.IsMatch(video_page_contents))
                        {
                            this.worker.ReportProgress(-1, "Unable to find video source, skipping: " + post_url);
                            continue;
                        }

                        foreach (Match vm in video_source_regex.Matches(video_page_contents))
                        {
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



                if (image_urls.Count == 0)
                {
                    string temp = Path.GetTempFileName();
                    System.IO.File.WriteAllLines(temp, post_page_contents.Split('\n'));
                    this.worker.ReportProgress(-1, "Media URL not found on " + post_url);
                    continue;
                }

                foreach (Uri uri in image_urls)
                {
                    output.Add(new MediaSourceResult(uri, new Uri(post_url), this.url));
                }
            }


            return output;

        }


    }
}
