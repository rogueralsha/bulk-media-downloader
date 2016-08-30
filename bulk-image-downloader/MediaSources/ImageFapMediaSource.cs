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

        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
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

        private const String GALLERY_STAGE = "gallery";
        private const String IMAGE_STAGE = "image";


        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return GetPages(url, page_contents);
                case GALLERY_STAGE:
                    return GetImagePages(url, page_contents);
                case IMAGE_STAGE:
                    return GetImages(url, page_contents);
                default:
                    throw new NotSupportedException(stage);
            }
        }

        private MediaSourceResults GetPages(Uri page_url, String page_contents) {
            if(page_url.ToString().Contains("?")) {
                page_url = new Uri(page_url.ToString().Split('?')[0]);
            }

            MediaSourceResults output = new MediaSourceResults();

            int i = 0;
            Uri new_url = new Uri(page_url.ToString() + "?page=" + i);
            String contents = GetPageContents(new_url);
            while (!contents.Contains("<b>Could not find gallery</b>")&&thumbnail_regex.IsMatch(contents)) {
                output.Add(new MediaSourceResult(new_url,null,this.url,this, MediaResultType.DownloadSource, GALLERY_STAGE));
                i++;
                new_url = new Uri(page_url.ToString() + "?page=" + i);
                contents = GetPageContents(new_url);
            }

            return output;

        }

        private MediaSourceResults GetImagePages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            MatchCollection mc = image_link_regex.Matches(page_contents);

            foreach (Match m in mc) {
                String id = m.Groups[1].Value;
                Uri new_url = new Uri("http://www.imagefap.com/photo/" + id + "/");

                output.Add(new MediaSourceResult(new_url, page_url, this.url, this, MediaResultType.DownloadSource, IMAGE_STAGE));
            }

            if (output.Count == 0)
                throw new Exception("No media found on " + page_url.ToString());

            return output;

        }

        private MediaSourceResults GetImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(page_contents);

                //<span style='visibility: hidden;' itemprop="contentUrl">http://x.imagefapusercontent.com/u/31cicem/4712552/1233618992/Meg_Turney_-_Me_in_My_Place_2013_-_Set_Five.jpg</span>

                HtmlNodeCollection thumbnailNodes = 
                    doc.DocumentNode.SelectNodes("//span[@itemprop='contentUrl']");

                if (thumbnailNodes != null && thumbnailNodes.Count > 0) {
                    Uri image_url = new Uri(thumbnailNodes[0].InnerText);
                    output.Add(new MediaSourceResult(image_url, page_url, this.url, this, MediaResultType.Download));
                } else {
                    throw new System.Exception("Image not found on page " + page_url.ToString());
                }


            if (output.Count == 0)
                throw new Exception("No media found on " + page_url.ToString());

            return output;

        }



    }
}
