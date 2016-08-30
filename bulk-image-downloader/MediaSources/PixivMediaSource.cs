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
    public class PixivMediaSource : AMediaSource {
        //http://www.pixiv.net/member_illust.php?id=5548602
        private static Regex address_regex = new Regex(@"(.+pixiv.net/member_illust.php\?id=([\d]+))");

        private string album_name;

        public override bool RequiresLogin {
            get {
                if (SuperWebClient.HasValidCookiesForDomain(new Uri("http://pixiv.net"))) {
                    return false;
                }
                return true;
            }
        }

        public PixivMediaSource(Uri url)
            : base(url) {

            this.LoginURL = @"http://www.pixiv.net";

            if (!address_regex.IsMatch(url.ToString())) {
                throw new Exception("Pixiv URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            album_name = address_matches[0].Groups[2].Value;

        }

        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
        }

        public override string getFolderNameFromURL(Uri url)
        {
            if (!address_regex.IsMatch(url.ToString()))
            {
                throw new Exception("Pixiv URL not understood");
            }
            MatchCollection address_matches = address_regex.Matches(url.ToString());
            string contents = GetPageContents(new Uri(address_matches[0].Value));
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(contents);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//h1[@class='user']");
            if (node == null)
                throw new Exception("User node not found");

            string album_name = node.InnerText;
            return album_name;
        }


        private const String GALLERY_STAGE = "gallery";
        private const String IMAGE_STAGE = "image";
        private const String MANGA_STAGE = "manga";


        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return GetPages(url, page_contents);
                case GALLERY_STAGE:
                    return GetImagePages(url, page_contents);
                case IMAGE_STAGE:
                    return GetImages(url, page_contents);
                case MANGA_STAGE:
                    return GetMangaModeImages(url, page_contents);
                default:
                    throw new NotSupportedException(stage);
            }
        }

        private const String LAYOUT_THUMBNAIL_SELECTOR = "//div[@class='_layout-thumbnail']";

        private MediaSourceResults GetPages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            int i = 1;
            Uri new_url = new Uri(page_url.ToString() + "&p=" + i);
            String contents = GetPageContents(new_url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(contents);
            HtmlNodeCollection thumbnailNodes = doc.DocumentNode.SelectNodes(LAYOUT_THUMBNAIL_SELECTOR);
            while (thumbnailNodes!= null&&thumbnailNodes.Count>0) {
                output.Add(new MediaSourceResult(new_url, null, this.url, this, MediaResultType.DownloadSource, GALLERY_STAGE));
                i++;
                new_url = new Uri(page_url.ToString() + "&p=" + i);
                contents = GetPageContents(new_url);
                doc = new HtmlDocument();
                doc.LoadHtml(contents);
                thumbnailNodes = doc.DocumentNode.SelectNodes(LAYOUT_THUMBNAIL_SELECTOR);
            }

            return output;

        }


        private MediaSourceResults GetImagePages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);
            HtmlNodeCollection thumbnailNodes = doc.DocumentNode.SelectNodes(LAYOUT_THUMBNAIL_SELECTOR);
            if (thumbnailNodes != null && thumbnailNodes.Count > 0) {
                foreach (HtmlNode node in thumbnailNodes) {
                    HtmlNode parentNode = node.ParentNode;
                    if (parentNode.Name != "a")
                        continue;

                    Uri imagePageUrl = new Uri("http://www.pixiv.net" + WebUtility.HtmlDecode(parentNode.Attributes["href"].Value));
                    output.Add(new MediaSourceResult(imagePageUrl, null, this.url, this, MediaResultType.DownloadSource, IMAGE_STAGE));
                }
            }

            return output;


        }
        private MediaSourceResults GetImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);
            HtmlNode originalImageNode = doc.DocumentNode.SelectSingleNode("//img[@class='original-image']");

            if(originalImageNode!=null) {
                MediaSourceResult res = new MediaSourceResult(new Uri(originalImageNode.Attributes["data-src"].Value), page_url, this.url, this, MediaResultType.Download);
                res.SimpleHeaders = true;
                output.Add(res);
                return output;
            }

            // Original image not present, usually means MANGA MODE!!
            HtmlNode multipleWorksNode = doc.DocumentNode.SelectSingleNode("//a[contains(@class,'_work') and contains(@class,'multiple')]");
            if (multipleWorksNode != null) {
                Uri mangaUrl = new Uri("http://www.pixiv.net/" + WebUtility.HtmlDecode(multipleWorksNode.Attributes["href"].Value));
                output.Add(new MediaSourceResult(mangaUrl, page_url, this.url, this, MediaResultType.DownloadSource, MANGA_STAGE));
            }

            if (output.Count == 0)
                throw new Exception("No media found on " + page_url.ToString());

            return output;

        }

        private MediaSourceResults GetMangaModeImages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            HtmlDocument multipleWorksDoc = new HtmlDocument();
            multipleWorksDoc.LoadHtml(page_contents);
            HtmlNodeCollection fullSizeNodes = multipleWorksDoc.DocumentNode.SelectNodes("//a[@class='full-size-container _ui-tooltip']");
            foreach (HtmlNode fullSizeNode in fullSizeNodes) {
                Uri fullSizeUrl = new Uri("http://www.pixiv.net" + WebUtility.HtmlDecode(fullSizeNode.Attributes["href"].Value));
                String fullSizeContents = GetPageContents(fullSizeUrl, page_url);
                HtmlDocument fullSizeDoc = new HtmlDocument();
                fullSizeDoc.LoadHtml(fullSizeContents);
                HtmlNode imgNode = fullSizeDoc.DocumentNode.SelectSingleNode("//img");
                if (imgNode != null) {
                    MediaSourceResult res = new MediaSourceResult(new Uri(imgNode.Attributes["src"].Value), fullSizeUrl, this.url, this, MediaResultType.Download);
                    res.SimpleHeaders = true;
                    output.Add(res);
                }

            }

            if (output.Count == 0)
                throw new Exception("No media found on " + page_url.ToString());

            return output;

        }


    }
}
