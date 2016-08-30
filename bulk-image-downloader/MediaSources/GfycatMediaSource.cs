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

namespace BulkMediaDownloader.MediaSources
{
    public class GfycatMediaSource: AMediaSource {
        //http://www.hentai-foundry.com/pictures/user/GENSHI
        private static Regex address_regex = new Regex(@"(.+gfycat.com/.+)");

        private static Regex media_address_regex = new Regex(@"(.+gfycat.com/[^/]+)");

        private string album_name;

        public GfycatMediaSource(Uri url)
            : base(url) {

            if (!ValidateUrl(url)) {
                throw new Exception("Gfycat URL not understood");
            }

            if (url.ToString().Contains("@"))
                throw new Exception("Gallery links not yet supported");
            //https://gfycat.com/@rogueralsha/carrie_keagan_as_power_girl

            MatchCollection address_matches = address_regex.Matches(url.ToString());
            album_name = address_matches[0].Groups[2].Value;

        }

        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
        }


        public override string getFolderNameFromURL(Uri url)
        {
            if (!ValidateUrl(url))
            {
                    throw new Exception("Gfycat URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string album_name = address_matches[0].Groups[2].Value;
            return album_name;
        }

        protected const String MEDIA_STAGE = "media";

        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return DetermineAction(url, page_contents);
                default:
                    throw new NotSupportedException(stage);
            }
        }

        private MediaSourceResults DetermineAction(Uri page_url, String page_contents) {
            if(media_address_regex.IsMatch(page_url.ToString())) {
                return GetMediaFromPage(page_url, page_contents);
            } else {
                throw new Exception("URL not supported: " + page_url.ToString());
            }
        }



        private MediaSourceResults GetMediaFromPage(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            //<source id="webmSource" src="https://giant.gfycat.com/YawningBlaringGuanaco.webm" type="video/webm">
            //<source id="mp4Source" src="https://fat.gfycat.com/YawningBlaringGuanaco.mp4" type="video/mp4">
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);
            HtmlNode node = doc.GetElementbyId("webmSource");
            try {
                if (node != null && node.Attributes["src"] != null && !String.IsNullOrWhiteSpace(node.Attributes["src"].Value))
                    output.Add(new MediaSourceResult(new Uri(node.Attributes["src"].Value), page_url,this.url, this, MediaResultType.Download));
            } catch (Exception e) {
                Console.Out.WriteLine(e.Message);
            }
            node = doc.GetElementbyId("mp4Source");
            try {
                if (node != null && node.Attributes["src"] != null && !String.IsNullOrWhiteSpace(node.Attributes["src"].Value))
                    output.Add(new MediaSourceResult(new Uri(node.Attributes["src"].Value), page_url, this.url, this, MediaResultType.Download));
            } catch (Exception e) {
                Console.Out.WriteLine(e.Message);
            }

            output.Add(new MediaSourceResult(url, null, url, MediaSourceManager.GetMediaSourceForUrl(url), MediaResultType.DownloadSource, INITIAL_STAGE));


            return output;
        }



    }
}
