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
using System.Web;
using System.Collections.Specialized;

namespace BulkMediaDownloader.MediaSources
{
    public class DropBoxMediaSource : AMediaSource {
        //https://www.dropbox.com/sh/5haik0wsev0xtf7/AADGQaYxLuTsX-WVBDVbAg7Ya?dl=0
        private static Regex address_regex = new Regex(@"https?:\/\/www\.dropbox\.com\/.+");

        private string album_name;

        public DropBoxMediaSource(Uri url)
            : base(url) {

            if (!ValidateUrl(url)) {
                throw new Exception("Dropbox URL not understood");
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
                    throw new Exception("Dropbox URL not understood");
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

            //https://www.dropbox.com/sh/5haik0wsev0xtf7/AADGQaYxLuTsX-WVBDVbAg7Ya?dl=0

            UriBuilder builder = new UriBuilder(page_url);
            NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
            query["dl"] = "1";
            builder.Query = query.ToString();
            Uri downloadUrl = builder.Uri;

            output.Add(new MediaSourceResult(downloadUrl, page_url, this.url, this, MediaResultType.Download));

            return output;
        }



    }
}
