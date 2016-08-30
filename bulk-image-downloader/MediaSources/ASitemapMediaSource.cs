using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;

namespace BulkMediaDownloader.MediaSources {
    // Abstract class to help with handling sitemap sources
    // While there is only one sitemap format, the concept of a sitemap spans several formats
    // This class should provide aid for them all
    public abstract class ASitemapMediaSource : AMediaSource {
        
        public ASitemapMediaSource(Uri url) : base(url) {

        }

        protected XmlNode getChildNode(XmlNode parent, string child_name, string child_attribute_name = null, string child_attribute_value = null) {
            foreach (XmlNode child in parent.ChildNodes) {
                if (child.Name.ToLower() != child_name.ToLower())
                    continue;

                if (child_attribute_name != null && child_attribute_value != null) {
                    if (child.Attributes[child_attribute_name] == null ||
                        child.Attributes[child_attribute_name].Value.ToLower() != child_attribute_value.ToLower())
                        continue;
                }
                return child;
            }
            return null;
        }


        protected List<XmlNode> getChildNodes(XmlNode parent, string child_name, string child_attribute_name = null, string child_attribute_value = null) {
            List<XmlNode> output = new List<XmlNode>();
            foreach (XmlNode child in parent.ChildNodes) {
                if (child.Name.ToLower() != child_name.ToLower())
                    continue;

                if (child_attribute_name != null && child_attribute_value != null) {
                    if (child.Attributes[child_attribute_name] == null ||
                        child.Attributes[child_attribute_name].Value.ToLower() != child_attribute_value.ToLower())
                        continue;
                }
                output.Add(child);
            }
            return output;
        }


        protected const String SITEMAP_STAGE = "sitemap";
        protected const String IMAGE_STAGE = "image";


        protected override MediaSourceResults ProcessDownloadSourceInternal(Uri url, string page_contents, string stage) {
            switch (stage) {
                case INITIAL_STAGE:
                    return GetPages(url, page_contents);
                case SITEMAP_STAGE:
                    return GetPagesFromSitemap(url, page_contents);
                case IMAGE_STAGE:
                    return GetMediaFromPage(url, page_contents);
                default:
                    throw new NotSupportedException(stage);
            }
        }

        protected MediaSourceResults GetPages(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page_contents);

            switch (doc.DocumentElement.Name.ToLower()) {
                case "sitemapindex":
                    //<sitemap>
                    //< loc > http://marcioabreuart.blogspot.com/sitemap.xml?page=2</loc>
                    //</ sitemap >
                    foreach (XmlNode child in getChildNodes(doc.DocumentElement, "sitemap")) {
                        XmlNode sitemapNode = getChildNode(child, "loc");
                        if (sitemapNode != null) {
                            output.Add(new MediaSourceResult(new Uri(sitemapNode.InnerText), page_url, this.url, this, MediaResultType.DownloadSource, SITEMAP_STAGE));
                        }
                    }
                    break;
                case "urlset":
                    return GetPagesFromSitemap(page_url, page_contents);
            }

            return output;
        }

        protected MediaSourceResults GetPagesFromSitemap(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page_contents);
            if (doc.DocumentElement.Name.ToLower() != "urlset")
                throw new Exception("Unrecognized root node: " + doc.Name);

            foreach (XmlNode urlNode in getChildNodes(doc.DocumentElement, "url")) {
                XmlNode locNode = getChildNode(urlNode, "loc");
                if (locNode != null) {
                    output.Add(new MediaSourceResult(new Uri(locNode.InnerText), page_url, this.url, this, MediaResultType.DownloadSource, IMAGE_STAGE));

                    Uri self_url = new Uri(locNode.InnerText);

                    // Sometimes images are linked right from the sitemap, we catch them here
                    List<XmlNode> imageNodes = getChildNodes(urlNode, "image:image");
                    foreach (XmlNode imageNode in imageNodes) {
                        XmlNode imageLocNode = getChildNode(imageNode, "image:loc");
                        if (imageLocNode != null && !string.IsNullOrEmpty(imageLocNode.InnerText)) {
                            output.Add(new MediaSourceResult(new Uri(imageLocNode.InnerText), self_url, this.url, this, MediaResultType.Download));
                        }
                    }

                }

            }

            return output;
        }

        protected virtual MediaSourceResults GetMediaFromPage(Uri page_url, String page_contents) {
            MediaSourceResults output = new MediaSourceResults();

            output.AddRange(getImagesAndDirectLinkedMedia(page_url, page_contents));

            return output;
        }

    }



}
