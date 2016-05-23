using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace BulkMediaDownloader.MediaSources
{
    public class ImageFapMediaSource : AMediaSource {
        //http://www.pixiv.net/member_illust.php?id=5548602
        private static Regex address_regex = new Regex(@"https?://www\.imagefap\.com/pictures/\d+/([^/]+)");
        private static Regex thumbnail_regex = new Regex(@"<img.+src=""(https?://x\d+.fap.to/images/thumb/[^""]+)"".*?>");
        private static Regex image_link_regex = new Regex(@"<a name=""(\d+)"" href=""[^""]+"">");
        private string album_name;

        public ImageFapMediaSource(Uri url)
            : base(url) {

            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("ImageFap not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            album_name = address_matches[0].Groups[1].Value;
        }

        public override string getFolderNameFromURL(Uri url)
        {
            if (!address_regex.IsMatch(url.ToString()))
            {
                throw new Exception("ImageFap not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            album_name = address_matches[0].Groups[1].Value;
            return album_name;
        }


        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents) {
            if(page_url.ToString().Contains("?")) {
                page_url = new Uri(page_url.ToString().Split('?')[0]);
            }

            HashSet<Uri> output = new HashSet<Uri>();

            int i = 0;
            Uri new_url = new Uri(page_url.ToString() + "?page=" + i);
            String contents = GetPageContents(new_url);
            while (!contents.Contains("<b>Could not find gallery</b>")&&thumbnail_regex.IsMatch(contents)) {
                output.Add(new_url);
                i++;
                new_url = new Uri(page_url.ToString() + "?page=" + i);
                contents = GetPageContents(new_url);
            }

            return output;

        }


        protected override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url, String page_contents) {
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            MatchCollection mc = image_link_regex.Matches(page_contents);

            foreach(Match m in mc) {
                String id = m.Groups[1].Value;
                Uri new_url = new Uri("http://www.imagefap.com/photo/" + id + "/");
                String contents = GetPageContents(new_url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(contents);

                //<span style='visibility: hidden;' itemprop="contentUrl">http://x.imagefapusercontent.com/u/31cicem/4712552/1233618992/Meg_Turney_-_Me_in_My_Place_2013_-_Set_Five.jpg</span>

                HtmlNodeCollection thumbnailNodes = 
                    doc.DocumentNode.SelectNodes("//span[@itemprop='contentUrl']");

                if (thumbnailNodes != null && thumbnailNodes.Count > 0) {
                    Uri image_url = new Uri(thumbnailNodes[0].InnerText);
                    output.Add(new MediaSourceResult(image_url, new_url, this.url));
                } else {
                    throw new System.Exception("Image not found on page " + new_url.ToString());
                }


            }

            if (output.Count == 0)
                throw new Exception("No media found on " + page_url.ToString());

            return output;

        }



    }
}
