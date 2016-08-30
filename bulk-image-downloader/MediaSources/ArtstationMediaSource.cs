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

namespace BulkMediaDownloader.MediaSources
{
    public class ArtstationMediaSource : AMediaSource {
        //https://www.artstation.com/artist/brentknight
        //https://www.artstation.com/users/brentknight/projects.json?page=1
        private static Regex address_regex = new Regex(@"(.+artstation.com/artist/([^/]+))");

        private string album_name;

        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
        }

        public ArtstationMediaSource(Uri url)
            : base(url) {

            if (!ValidateUrl(url)) {
                throw new Exception("Artstation URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            album_name = address_matches[0].Groups[2].Value;

        }

        public override string getFolderNameFromURL(Uri url)
        {
            if (!address_regex.IsMatch(url.ToString()))
            {
                throw new Exception("Artstation URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string album_name = address_matches[0].Groups[2].Value;
            return album_name;
        }


        private Uri preparePageUrl(int page)
        {
            return new Uri("https://www.artstation.com/users/" + album_name + "/projects.json?page="  + page);
        }


        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return GetGalleryPages(url, page_contents);
                case "json":
                    return GetImages(url, page_contents);
                default:
                    throw new NotSupportedException(stage);
            }
        }

        private MediaSourceResults GetGalleryPages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();


            int page = 1;
            Uri new_url = preparePageUrl(page);
            String json = GetPageContents(new_url);
            while (json.Length > 30)
            {
                Dictionary<string, dynamic> values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                Object data = values["data"];
                List<dynamic> dataList = JsonConvert.DeserializeObject<List<dynamic>>(data.ToString());
                foreach (Object entry in dataList)
                {
                    values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(entry.ToString());
                    String hash = values["hash_id"];
                    output.Add(new MediaSourceResult(new Uri("https://www.artstation.com/projects/" + hash + ".json"),null, this.url, this, MediaResultType.DownloadSource, "json"));
                }
                page++;
                new_url = preparePageUrl(page);
                json = GetPageContents(new_url);
            }
            return output;

        }


        private MediaSourceResults GetImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();


            Dictionary<string, dynamic> values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(page_contents);
            Object data = values["assets"];
            List<dynamic> dataList = JsonConvert.DeserializeObject<List<dynamic>>(data.ToString());
            foreach (Object entry in dataList)
            {
                values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(entry.ToString());
                bool hasImage = true;
                if (values["has_image"] != null)
                    hasImage = values["has_image"];

                if (!string.IsNullOrEmpty(values["image_url"]))
                {
                    if (!Uri.IsWellFormedUriString(values["image_url"], UriKind.Absolute))
                        continue;
                    
                    output.Add(new MediaSourceResult(new Uri(values["image_url"]),page_url, this.url,this, MediaResultType.Download));
                } else
                {
                    throw new NotSupportedException();
                }
            }



            return output;

        }



    }
}
