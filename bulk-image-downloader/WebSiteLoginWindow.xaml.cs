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

namespace BulkMediaDownloader
{
    /// <summary>
    /// Interaction logic for WebSiteLoginWindow.xaml
    /// </summary>
    public partial class WebSiteLoginWindow : Window, CefSharp.ICookieVisitor
    {
        private string login_url;
        private string desired_cookie_name;

        private List<CefSharp.Cookie> VisitedCookies = new List<CefSharp.Cookie>();

        public WebSiteLoginWindow(string login_url, string desired_cookie_name)
        {

            this.login_url = login_url;
            this.desired_cookie_name = desired_cookie_name;
            InitializeComponent();

            this.webBrowser.Address = login_url;

        }

        public bool Visit(CefSharp.Cookie cookie, int count, int total, ref bool deleteCookie)
        {
            this.VisitedCookies.Add(cookie);

            return true;
        }


        public List<CefSharp.Cookie> FoundCookies
        {
            get
            {
                ICookieManager cookie_manager = Cef.GetGlobalCookieManager();
                cookie_manager.VisitUrlCookies(login_url, true, this);

                System.Threading.Thread.Sleep(1000); //Gotta give the cookies time to populate
                return VisitedCookies;
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();

        }
    }



}
