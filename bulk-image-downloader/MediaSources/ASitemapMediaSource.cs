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

namespace BulkMediaDownloader.MediaSources
{
    // Abstract class to help with handling sitemap sources
    // While there is only one sitemap format, the concept of a sitemap spans several formats
    // This class should provide aid for them all
    public abstract class ASitemapMediaSource : AMediaSource {


        public ASitemapMediaSource(Uri url): base(url) {

        }

        protected XmlNode getChildNode(XmlNode parent, string child_name, string child_attribute_name = null, string child_attribute_value = null)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child.Name.ToLower() != child_name.ToLower())
                    continue;

                if (child_attribute_name != null && child_attribute_value != null)
                {
                    if (child.Attributes[child_attribute_name] == null ||
                        child.Attributes[child_attribute_name].Value.ToLower() != child_attribute_value.ToLower())
                        continue;
                }
                return child;
            }
            return null;
        }
        protected List<XmlNode> getChildNodes(XmlNode parent, string child_name, string child_attribute_name = null, string child_attribute_value = null)
        {
            List<XmlNode> output = new List<XmlNode>();
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child.Name.ToLower() != child_name.ToLower())
                    continue;

                if (child_attribute_name != null && child_attribute_value != null)
                {
                    if (child.Attributes[child_attribute_name] == null ||
                        child.Attributes[child_attribute_name].Value.ToLower() != child_attribute_value.ToLower())
                        continue;
                }
                output.Add(child);
            }
            return output;
        }

    }



}
