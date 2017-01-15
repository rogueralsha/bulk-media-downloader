using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace BulkMediaDownloader.MediaSources
{
    public class ImgurMediaSource: AMediaSource {
        //http://sexycyborg.imgur.com/
        private static Regex user_regex = new Regex(@"http[s]?:\/\/([^\.]+)\.imgur\.com\/.+");
        //http://imgur.com/a/4aAPS
        private static Regex album_regex = new Regex(@"http[s]?:\/\/imgur\.com\/a\/(.+)");
        //http://i.imgur.com/yuVxOjz.jpg
        private static Regex image_regex = new Regex(@"http[s]?:\/\/i\.imgur\.com/.+");

        private string album_name;
        int results_per_page = 500;

        public ImgurMediaSource(Uri url)
            : base(url) {

            if (!ValidateUrl(url))
            {
                throw new Exception("Imgur URL not understood");
            }
            album_name = getFolderNameFromURL(url);

        }


        public static bool ValidateUrl(Uri url) {
            return album_regex.IsMatch(url.ToString()) || user_regex.IsMatch(url.ToString()) || image_regex.IsMatch(url.ToString());
        }

        public override string getFolderNameFromURL(Uri url)
        {
            if (user_regex.IsMatch(url.ToString())) {
                MatchCollection address_matches = user_regex.Matches(url.ToString());
                string album_name = address_matches[0].Groups[1].Value;
                return album_name;
            } else if (album_regex.IsMatch(url.ToString())) {
                String contents = GetPageContents(url);
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(contents);
                //        <meta property="og:title" content="Pi Palette- Hacker&#039;s Cosmetic Case"/>
                HtmlNode typeNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                if (typeNode == null)
                    throw new Exception("Can't find post title node");

                MatchCollection address_matches = album_regex.Matches(url.ToString());
                string album_name = address_matches[0].Groups[1].Value + " - " + typeNode.Attributes["content"].Value;
                return album_name;
                //post-title
            } else if(image_regex.IsMatch(url.ToString())) {
                return string.Empty;
            } else             {
                    throw new Exception("Imgur URL not understood");
            }

        }

        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return DetermineAction(url, page_contents);
                case ALBUM_STAGE:
                    return GetAlbumImages(url, page_contents);
                case USER_STAGE:
                    return new MediaSourceResults();
                default:
                    throw new NotSupportedException(stage);
            }
        }

        private const String USER_STAGE = "user";
        private const String ALBUM_STAGE = "album";

        private MediaSourceResults DetermineAction(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();
            if (image_regex.IsMatch(page_url.ToString())) {
                output.Add(new MediaSourceResult(page_url, page_url, this.url, this, MediaResultType.Download));
            } else if (user_regex.IsMatch(page_url.ToString())) {
                output.Add(new MediaSourceResult(page_url, page_url, this.url, this, MediaResultType.DownloadSource,USER_STAGE));
            } else if (album_regex.IsMatch(page_url.ToString())) {
                output.Add(new MediaSourceResult(page_url, page_url, this.url, this, MediaResultType.DownloadSource, ALBUM_STAGE));
            } else {
                throw new Exception("URL not supported: " + page_url.ToString());
            }
            return output;
        }

        private MediaSourceResults GetAlbumImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            //http://imgur.com/ajaxalbums/getimages/4aAPS/hit.json

            MatchCollection address_matches = album_regex.Matches(url.ToString());
            string album_id = address_matches[0].Groups[1].Value;


            String json = GetPageContents(new Uri(@"http://imgur.com/ajaxalbums/getimages/" + album_id + "/hit.json"), page_url);

            Dictionary<string, dynamic> values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
            Object data = values["data"];
            Dictionary<dynamic,dynamic> dataList = JsonConvert.DeserializeObject<Dictionary<dynamic,dynamic>>(data.ToString());
            data = dataList["images"];
            List<dynamic> images = JsonConvert.DeserializeObject<List<dynamic>>(data.ToString());
            foreach (Object image in images) {
                values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(image.ToString());
                String hash = values["hash"];
                String extension = values["ext"];
                Uri imageUri = new Uri("http://i.imgur.com/" + hash + extension);
                MediaSourceResult msr = new MediaSourceResult(imageUri, page_url, this.url, this, MediaResultType.Download);
                msr.Subfolder = album_name;
                output.Add(msr);
            }

            return output;
        }

    }
}
