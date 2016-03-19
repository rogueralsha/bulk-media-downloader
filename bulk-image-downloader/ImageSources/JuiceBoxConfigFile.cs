using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Xml;
namespace BulkMediaDownloader.ImageSources
{
    class JuiceBoxConfigFile {
        XmlDocument doc;
        public JuiceBoxConfigFile(Stream stream) {
            doc = new XmlDocument();

            doc.Load(stream);

        }

        public List<String> GetImageURLS() {
            List<String> output = new List<String>();


            foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                if (node.Name == "image" && !String.IsNullOrWhiteSpace(node.Attributes["imageURL"].Value)) {
                    output.Add(node.Attributes["imageURL"].Value);
                } 
            }


            return output;
        }


    }
}
