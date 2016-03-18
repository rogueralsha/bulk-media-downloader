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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using bulk_image_downloader.ImageSources;

namespace bulk_image_downloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        DownloadManager manager;

        private String selected_dir;
        private String download_dir;

        private Queue<Uri> urls = new Queue<Uri>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                manager = new DownloadManager();
                lstDownloadables.DataContext = manager;
                lstDownloadables.ItemsSource = manager;
                inputMaxDownloads.DataContext = manager;
                remainingDownloads.DataContext = manager;
                manager.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error!");
                this.Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                startProcess(new Uri(txtURL.Text), false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }


        private bool processing =  false;
        private void startProcess(Uri url, bool resuse_dir)
        {
            AImageSource source = null;
            statusLabel.Content = "Loading images from " + url.ToString();
            try
            {
                ComboBoxItem item = (ComboBoxItem)cboUrlType.SelectedItem;

                if (!resuse_dir)
                {
                    CommonOpenFileDialog dlg = new CommonOpenFileDialog();
                    dlg.Title = "Select download folder";
                    dlg.IsFolderPicker = true;
                    dlg.InitialDirectory = Properties.Settings.Default.LastDownloadDir;
                    dlg.AddToMostRecentlyUsedList = false;
                    dlg.AllowNonFileSystemItems = false;
                    dlg.DefaultDirectory = Properties.Settings.Default.LastDownloadDir;
                    dlg.EnsureFileExists = true;
                    dlg.EnsurePathExists = true;
                    dlg.EnsureReadOnly = false;
                    dlg.EnsureValidNames = true;
                    dlg.Multiselect = false;
                    dlg.ShowPlacesList = true;

                    if (dlg.ShowDialog(this) == CommonFileDialogResult.Cancel)
                    {
                        return;
                    }
                    selected_dir = dlg.FileName;
                    Properties.Settings.Default.LastDownloadDir = selected_dir;
                    Properties.Settings.Default.Save();
                }



                download_dir = selected_dir;

                switch (item.Tag.ToString())
                {
                    case "shimmie":
                        source = new ShimmieImageSource(url);
                        break;
                    case "flickr":
                        source = new FlickrImageSource(url);
                        break;
                    case "juicebox":
                        //source = new JuiceBoxImageSource(url);
                        break;
                    case "nextgen":
                        source = new NextGENImageSource(url);
                        break;
                    case "deviantart":
                        source = new DeviantArtImageSource(url);
                        break;
                    case "hentaifoundry":
                        source = new HentaiFoundryImageSource(url);
                        break;
                    default:
                        throw new Exception("URL Type not supported");
                }

                string album_folder = source.getFolderNameFromURL(url);
                if(!string.IsNullOrWhiteSpace(album_folder))
                {
                    download_dir = System.IO.Path.Combine(download_dir, album_folder);
                    if(!System.IO.Directory.Exists(download_dir))
                    {
                        System.IO.Directory.CreateDirectory(download_dir);
                    }
                }

                if (source.RequiresLogin)
                {
                    WebSiteLoginWindow loginWindow = new WebSiteLoginWindow(source.LoginURL, "");
                    loginWindow.ShowDialog();
                    AImageSource.SetCookies(loginWindow.FoundCookies);

                }

                source.worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                source.Start();
                processing = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }


        }

        private void DisableInterface()
        {
            SetInterface(false);
        }

        private void EnableInterface()
        {
            SetInterface(true);
        }

        private void SetInterface(bool enabled)
        {


        }

        void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
                if (urls.Count > 0)
                {
                    startProcess(urls.Dequeue(), true);
                    return;
                }
                processing = false;
                EnableInterface();
                return;
            }

            Dictionary<Uri, List<Uri>> images = (Dictionary<Uri, List<Uri>>)e.Result;
            foreach (Uri page in images.Keys)
            {
                foreach (Uri image in images[page])
                {
                    DownloadManager.DownloadImage(image, download_dir, page.ToString());
                }
            }
            DownloadManager.SaveAll();

            if(urls.Count>0)
            {
                startProcess(urls.Dequeue(), true);
                return;
            }

            statusLabel.Content = String.Empty;

            EnableInterface();
            processing = false;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (manager != null)
            {
                manager.Stop();
            }
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            this.manager.ClearAllDownloads();
        }

        private void btnRetryFailed_Click(object sender, RoutedEventArgs e)
        {
            this.manager.RestartFailed();
        }

        private void btnClearCompleted_Click(object sender, RoutedEventArgs e)
        {
            manager.ClearCompleted();
        }

        private void btnPauseSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (Downloadable down in this.lstDownloadables.SelectedItems)
            {
                down.Pause();
            }
            DownloadManager.SaveAll();
        }

        private void chkAllPages_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAllPages.IsChecked == true)
            {
                Properties.Settings.Default.DetectAdditionalPages = true;
                Properties.Settings.Default.Save();
            }
            else {
                Properties.Settings.Default.DetectAdditionalPages = false;
                Properties.Settings.Default.Save();
            }
        }

        private void resume_selected_Click(object sender, RoutedEventArgs e)
        {
            foreach (Downloadable down in this.lstDownloadables.SelectedItems)
            {
                down.Reset();
            }
            DownloadManager.SaveAll();
        }

        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                MultiLineInput mli = new MultiLineInput();
                if (mli.ShowDialog().Value)
                {
                    String text = mli.Contents;
                    List<Uri> uris = new List<Uri>();

                    foreach (string url in text.Split('\n'))
                    {
                        uris.Add(new Uri(url));
                    }

                    foreach(Uri uri in uris)
                    {
                        urls.Enqueue(uri);
                    }

                    if (!processing)
                        startProcess(urls.Dequeue(), false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }

        }
    }
}
