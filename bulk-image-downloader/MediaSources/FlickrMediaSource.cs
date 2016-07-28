using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace BulkMediaDownloader.MediaSources
{
    public class FlickrMediaSource : AMediaSource {
        //https://www.flickr.com/photos/29383501@N08/
        private String base_url;
        private static Regex address_regex = new Regex(@"https?://(?:www\.)?flickr\.com/photos/([^/]+)/?");
        private static Regex api_key_regex = new Regex(@"root\.YUI_config\.flickr\.api\.site_key = ""([^""]+)"";");
        private string album_name, user_id, api_key;

        private const  int MAX_PER_PAGE = 500;

        public FlickrMediaSource(Uri url)
            : base(url) {

            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("Flickr url not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            setAlbumName(url);
            worker.ReportProgress(-1, "Flickr API key: " + api_key);
        }

        public override string getFolderNameFromURL(Uri url) {
            return album_name;
        }

        private void setAlbumName(Uri url) {
            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("Flickr url not understood");
            }
            String content = GetPageContents(url);
            if (!api_key_regex.IsMatch(content))
                throw new Exception("Could not find API key");
            api_key = api_key_regex.Match(content).Groups[1].Value;

            MatchCollection address_matches = address_regex.Matches(url.ToString());
            this.base_url = address_matches[0].Value;
            this.user_id= address_matches[0].Groups[1].Value;
            Dictionary<string, dynamic> values = getFlickrRestAPIResponse(getPhotoStreamRestUrl(this.user_id), new Uri(this.base_url));
            this.album_name = values["user"]["username"];
        }

        private static Uri heartbeatUrl = new Uri("https://heartbeat.flickr.com/beacon");
        private Dictionary<string, dynamic> getFlickrRestAPIResponse(Uri url, Uri referrer = null) {
            GetPageContents(heartbeatUrl);
            String content = GetPageContents(url, referrer);
            return filterFlickrResponse(content);
        }

        private Dictionary<string, dynamic> filterFlickrResponse(String content) {
            content = content.Replace("jsonFlickrApi(", "").TrimEnd(')');
            Dictionary<string, dynamic> values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
            return values;
        }

        private Uri getPhotoSizesRestUrl(String photo_id) {
            //https://api.flickr.com/services/rest?photo_id=8130487218&hermes=1&sort=date_asc&viewerNSID=&method=flickr.photos.getSizes&csrf=&api_key=05535513bb59b9f37a1983a932cdcd10&format=json&hermesClient=1&nojsoncallback=1
            return new Uri(@"https://api.flickr.com/services/rest?photo_id=" + photo_id + "&hermes=1&sort=date_asc&viewerNSID=&method=flickr.photos.getSizes&csrf=&api_key=" + api_key + "&format=json&hermesClient=1&nojsoncallback=1");
        }
        private Uri getPhotoStreamRestUrl(String user_id, int page = 1, int per_page = 50) {
            //https://api.flickr.com/services/rest?per_page=500&page=1&get_user_info=1&user_id=29383501%40N08&view_as=use_pref&sort=use_pref&viewerNSID=&method=flickr.people.getPhotos&csrf=&api_key=05535513bb59b9f37a1983a932cdcd10&format=json&hermes=1&hermesClient=1&reqId=8e9b36d1&nojsoncallback=1
            return new Uri(@"https://api.flickr.com/services/rest?per_page=" + per_page + "&page=" + page + "&get_user_info=1&user_id=" + user_id + "&view_as=use_pref&sort=use_pref&viewerNSID=&method=flickr.people.getPhotos&csrf=&api_key=" + api_key + "&format=json&hermes=1&hermesClient=1&nojsoncallback=1");
        }

        protected override HashSet<Uri> GetPages(Uri page_url, String page_contents) {
            HashSet<Uri> output = new HashSet<Uri>();

            Dictionary<string, dynamic> values = getFlickrRestAPIResponse(getPhotoStreamRestUrl(this.user_id, per_page: MAX_PER_PAGE), new Uri(this.base_url));

            String totalPagesString = values["photos"]["pages"];
            int pages;
            if(!int.TryParse(totalPagesString, out pages)) {
                throw new Exception("Total pages not found in JSON");
            }

            for(int i = 1; i<= pages; i++) {
                output.Add(getPhotoStreamRestUrl(this.user_id, page: i, per_page: MAX_PER_PAGE));
            }
            return output;
        }


        public override HashSet<MediaSourceResult> GetMediaFromPage(Uri page_url) {
            String page_contents = this.GetPageContents(page_url);
            HashSet<MediaSourceResult> output = new HashSet<MediaSourceResult>();

            Dictionary<string, dynamic> stream_values = filterFlickrResponse(page_contents);

            Newtonsoft.Json.Linq.JArray photos = stream_values["photos"]["photo"];

            foreach (Dictionary<String, dynamic> photo in photos.ToObject<List<Dictionary<String, dynamic>>>()) {
                String id = photo["id"];
                Dictionary<string, dynamic> photo_values = getFlickrRestAPIResponse(getPhotoSizesRestUrl(id), page_url);
                Newtonsoft.Json.Linq.JArray sizes = photo_values["sizes"]["size"];

                String candidate = "";
                long candidateSize = 0;
                foreach (Dictionary<String, dynamic> size in sizes.ToObject<List<Dictionary<String, dynamic>>>()) {
                    String heightString = size["height"].ToString();
                    String widthString = size["width"].ToString();
                    int height, width;
                    if (!int.TryParse(heightString, out height))
                        throw new Exception("Could not parse height");
                    if (!int.TryParse(widthString, out width))
                        throw new Exception("Could not parse height");
                    long pixelCount = height * width;
                    if(pixelCount>candidateSize) {
                        candidateSize = pixelCount;
                        candidate = size["source"];
                    }
                }
                if (String.IsNullOrEmpty(candidate))
                    throw new Exception("Could not find size candidate for image " + id);

                output.Add(new MediaSourceResult(new Uri(candidate), page_url, this.url));
            }

            return output;

        }


    }
}
