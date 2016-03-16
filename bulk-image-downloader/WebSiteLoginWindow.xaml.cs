using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using CefSharp;

namespace bulk_image_downloader
{
    /// <summary>
    /// Interaction logic for WebSiteLoginWindow.xaml
    /// </summary>
    public partial class WebSiteLoginWindow : Window
    {
        private string login_url;
        private string desired_cookie_name;

        private CachedCookies _cookies = new CachedCookies();

        public WebSiteLoginWindow(string login_url, string desired_cookie_name)
        {

            this.login_url = login_url;
            this.desired_cookie_name = desired_cookie_name;
            InitializeComponent();
            
            this.webBrowser.Address=login_url;
        }

        public CachedCookies FoundCookies
        {
            get
            {
                ICookieManager cookie_managed = Cef.GetGlobalCookieManager();
                cookie_managed.VisitUrlCookies(login_url, false, _cookies);
                //cookie_managed.VisitAllCookies(_cookies);
                System.Threading.Thread.Sleep(1000); //Gotta give the cookies time to populate
                return _cookies;
            }
        }


    }



}
