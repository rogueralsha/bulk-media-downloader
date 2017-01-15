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
    public class GoogleDriveMediaSource : AMediaSource {
        // https://drive.google.com/file/d/FILE_ID/edit?usp=sharing
        // https://drive.google.com/open?id=0B-iLA-jxndDOMXRQRmJQUE5INWc}
        private static Regex address_regex = new Regex(@"https?:\/\/drive\.google\.com\/open\?id=([^\/]+)");

        private string album_name;

        public GoogleDriveMediaSource(Uri url)
            : base(url) {

            if (!ValidateUrl(url)) {
                throw new Exception("Google Drive URL not understood");
            }

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
                    throw new Exception("Google Drive URL not understood");
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
            if(address_regex.IsMatch(page_url.ToString())) {
                return GetMediaFromPage(page_url, page_contents);
            } else {
                throw new Exception("URL not supported: " + page_url.ToString());
            }
        }



        private MediaSourceResults GetMediaFromPage(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string file_id = address_matches[0].Groups[1].Value;

            //https://drive.google.com/uc?export=download&id=FILE_ID


            Uri uri = new Uri("https://drive.google.com/uc?export=download&id=" + file_id);
            output.Add(new MediaSourceResult(uri, page_url, this.url, this, MediaResultType.Download));

            return output;
        }



    }
}
