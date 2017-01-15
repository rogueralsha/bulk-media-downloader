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
using System.Collections.Specialized;
using System.Web;

namespace BulkMediaDownloader.MediaSources
{
    public class EHentaiMediaSource: AMediaSource {
        //http://g.e-hentai.org/g/491173/3aba9707b6/
        private static Regex address_regex = new Regex(@"http[s]?:\/\/g\.e\-hentai\.org\/g\/\d+\/.+\/");
        //http://g.e-hentai.org/s/bdf977466a/491173-2
        private static Regex image_page_regex = new Regex(@"http[s]?:\/\/g\.e\-hentai\.org\/s\/[^\/]+\/\d+\-\d+");
        //http://g.e-hentai.org/fullimg.php?gid=491173&amp;page=137&amp;key=9q7rf4h8u3a
        private static Regex download_regex = new Regex(@"http[s]?:\/\/g\.e\-hentai\.org\/fullimg.php\?.+");


        private string album_name;

        public static bool ValidateUrl(Uri url) {
            return address_regex.IsMatch(url.ToString());
        }

        public EHentaiMediaSource(Uri url)
            : base(url) {

            if (!ValidateUrl(url)) {
                throw new Exception("E-Hentai URL not understood");
            }
            getFolderNameFromURL(url);
            this.LoginURL = @"http://e-hentai.org/bounce_login.php";
        }

        public override bool RequiresLogin {
            get {
                if (SuperWebClient.HasValidCookiesForDomain(new Uri("http://e-hentai.org"))) {
                    return false;
                }
                return true;
            }
        }

        public override string getFolderNameFromURL(Uri url)
        {
            if (!address_regex.IsMatch(url.ToString()))
            {
                throw new Exception("E-Hentai URL not understood");
            }

            // H1#gn
            String contents = GetPageContents(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(contents);
            HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//h1[@id='gn']");
            if (titleNode == null)
                throw new Exception("Title node not found");
            album_name = titleNode.InnerText;
            return titleNode.InnerText;
        }

        private const String IMAGES_STAGE = "images";
        private const String IMAGE_STAGE = "image";

        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return GetGalleryPages(url, page_contents);
                case IMAGES_STAGE:
                    return GetImagePages(url, page_contents);
                case IMAGE_STAGE:
                    return GetImage(url, page_contents);
                default:
                    throw new NotSupportedException(stage);
            }
        }

        private MediaSourceResults GetGalleryPages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);

            int maxPage = 0;

            UriBuilder builder = new UriBuilder(page_url);
            long prevLength = 0;
            NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
            query["p"] =maxPage.ToString();
            builder.Query = query.ToString();
            Uri currentPage = builder.Uri;

            String pageContents= GetPageContents(currentPage, page_url);
            long length = pageContents.Length;
            while(length!=prevLength) {
                output.Add(new MediaSourceResult(currentPage, page_url, this.url, this, MediaResultType.DownloadSource, IMAGES_STAGE));
                prevLength = length;
                maxPage++;
                query["p"] = maxPage.ToString();
                builder.Query = query.ToString();
                currentPage = builder.Uri;
                pageContents = GetPageContents(currentPage, page_url);
                length = pageContents.Length;
            }



            //table.ptb
            return output;

        }


        private MediaSourceResults GetImagePages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();


            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);

            HtmlNodeCollection nodes= doc.DocumentNode.SelectNodes("//a");
            if (nodes == null)
                throw new Exception("Could not find div.gtdm nodes");

            foreach(HtmlNode node in nodes) {
                if (node.Attributes["href"] == null)
                    continue;
                String link =  HttpUtility.HtmlDecode(node.Attributes["href"].Value);

                if(image_page_regex.IsMatch(link))
                    output.Add(new MediaSourceResult(new Uri(link), page_url, this.url, this, MediaResultType.DownloadSource, IMAGE_STAGE));

            }

            return output;

        }


        private MediaSourceResults GetImage(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();


            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page_contents);

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a");
            if (nodes == null)
                throw new Exception("Could not find div.gtdm nodes");

            foreach (HtmlNode node in nodes) {
                if (node.Attributes["href"] == null)
                    continue;
                String link = HttpUtility.HtmlDecode(node.Attributes["href"].Value);

                if (download_regex.IsMatch(link))
                    output.Add(new MediaSourceResult(new Uri(link), page_url, this.url, this, MediaResultType.Download));
            }

            return output;

        }

    }
}
