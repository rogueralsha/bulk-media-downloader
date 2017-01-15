using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkMediaDownloader.MediaSources {
    public class MediaSourceManager {


        private static readonly Dictionary<Uri, AMediaSource> MediaSources = new Dictionary<Uri, AMediaSource>();

        private static readonly IList<Type> mediaSourceTypes = new List<Type> {
            typeof(ArtstationMediaSource),
            typeof(BloggerMediaSource),
            typeof(ComicArtCommunityMediaSource),
            typeof(FlickrMediaSource),
            //typeof(GelbooruMediaSource),
            typeof(GfycatMediaSource),
            typeof(HentaiFoundryMediaSource),
            typeof(ImageFapMediaSource),
            typeof(ImgurMediaSource),
            //typeof(JuiceBoxImageSource),
            //typeof(NextGENMediaSource),
            typeof(PixivMediaSource),
            typeof(ShimmieMediaSource),
            typeof(SitemapMediaSource),
            typeof(TumblrMediaSource),
            typeof(DeviantArtMediaSource),
            typeof(WebmshareMediaSource),
            typeof(GoogleDriveMediaSource),
            typeof(DropBoxMediaSource),
            typeof(EHentaiMediaSource)
            };

        private static readonly IList<Type> hostedMediaSources = new List<Type> {
            typeof(GfycatMediaSource),
            typeof(ImgurMediaSource),
            typeof(WebmshareMediaSource),
            typeof(GoogleDriveMediaSource),
            typeof(DropBoxMediaSource)
            };


        public static AMediaSource GetMediaSourceForUrl(Uri uri, bool mediaHostsOnly = false) {
            if (MediaSources.ContainsKey(uri))
                return MediaSources[uri];
            IList<Type> sources;

            if (mediaHostsOnly)
                sources = hostedMediaSources;
            else
                sources = mediaSourceTypes;

           AMediaSource output = null;
            foreach (Type mediaSourceType in sources) {
                bool result = (bool)mediaSourceType.InvokeMember("ValidateUrl", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] {uri});
                if (result) {
                    output = (AMediaSource)Activator.CreateInstance(mediaSourceType, new Object[] { uri });
                    break;
                }
            }
            if (output == null)
                throw new UrlNotRecognizedException("URL not recognized: " + uri.ToString());


            output.StatusChanged += MediaSource_StatusChanged;
            MediaSources.Add(uri, output);
            return output;
        }

        public static event EventHandler<MediaSourceEventArgs> MediaSourceStatusChanged;

        private static void MediaSource_StatusChanged(object sender, MediaSourceEventArgs e) {
            if (MediaSourceStatusChanged != null)
                MediaSourceStatusChanged(sender, e);
        }
    }
}
