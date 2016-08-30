using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using BulkMediaDownloader.MediaSources;
using BulkMediaDownloader.Download;
using CefSharp;

namespace BulkMediaDownloader {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow, IGetCredentials {

        DownloadManager manager { get; set; }
        private Queue<Uri> urls = new Queue<Uri>();

        public MainWindow() {
            InitializeComponent();
            Cef.Initialize(new CefSettings());            
            DownloadManager.CredentialsProvider = this;
            manager = new DownloadManager();
            MediaSourceManager.MediaSourceStatusChanged += Manager_MediaSourceStatusChanged;
        }

        private void Manager_MediaSourceStatusChanged(object sender, MediaSourceEventArgs e) {
            WriteMessageToLog(e.Message);
        }

        private void DaWindow_Loaded(object sender, RoutedEventArgs e) {
            try {
                lstDownloadables.DataContext = manager;
                lstDownloadables.ItemsSource = manager;
                statusBarProgress.DataContext = manager;
                statusProgressBarText.DataContext = manager;
                maxDownloadsCombo.DataContext = manager;
                this.DataContext = manager;
                //remainingDownloads.DataContext = manager;
                manager.Start();
            } catch (Exception ex) {
                ShowException(ex);
                this.Close();
            }
        }



        private bool setDownloadFolder() {
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

            if (dlg.ShowDialog(this) == CommonFileDialogResult.Cancel) {
                return false;
            }
            string selected_dir = dlg.FileName;
            Properties.Settings.Default.LastDownloadDir = selected_dir;
            Properties.Settings.Default.Save();
            return true;
        }

        private void startProcess() {
            while (urls.Count > 0) {
                Uri url = urls.Dequeue();

                AMediaSource source = null;

                statusLabel.Content = "Loading images from " + url.ToString();
                try {
                    if (String.IsNullOrWhiteSpace(Properties.Settings.Default.LastDownloadDir) ||
                        !System.IO.Directory.Exists(Properties.Settings.Default.LastDownloadDir)) {
                        if (!setDownloadFolder())
                            return;
                    }

                    string download_dir = Properties.Settings.Default.LastDownloadDir; ;

                    source = MediaSourceManager.GetMediaSourceForUrl(url);

                    if (source.RequiresLogin && !this.getCredentials(source)) {
                        return;
                    }

                    string album_folder = source.getFolderNameFromURL(url);

                    if (!string.IsNullOrWhiteSpace(album_folder)) {
                        download_dir = System.IO.Path.Combine(download_dir, album_folder);
                        if (!System.IO.Directory.Exists(download_dir)) {
                            System.IO.Directory.CreateDirectory(download_dir);
                        }
                    }


                    logText.AppendText("Using download location " + download_dir + Environment.NewLine);

                    MediaSourceResult result = source.GetInitialStage();

                    manager.AddMediaSourceResult(result, download_dir);

                    statusLabel.Content = String.Empty;

                } catch (Exception ex) {
                    ShowException(ex);
                } finally {
                    if (urls.Count > 0) {
                        startProcess();                        
                    } else {
                        EnableInterface();
                    }
                }
            }


        }

        public bool getCredentials(AMediaSource source) {
            WebSiteLoginWindow loginWindow = new WebSiteLoginWindow(source.LoginURL, "");
            loginWindow.Owner = this;
            if (!loginWindow.ShowDialog().Value) {
                return false;
            }
            AMediaSource.SetCookies(loginWindow.FoundCookies);
            return true;
        }



        private void ShowException(Exception e, bool show_message_box = true) {
            if (show_message_box)
                MessageBox.Show(e.Message);

            ShowExceptionHelper(e);
            Exception ex = e.InnerException;
            while (ex != null) {
                ShowExceptionHelper(ex);
                ex = ex.InnerException;
            }
        }

        private void ShowExceptionHelper(Exception e) {
            logText.AppendText(e.Message + Environment.NewLine);
            logText.AppendText(e.StackTrace + Environment.NewLine);
            if (e is System.Net.WebException) {
                System.Net.WebException we = (System.Net.WebException)e;
                //logText.AppendText("Request URL:" + we.Response.ResponseUri.ToString() + Environment.NewLine);
                //var encoding = ASCIIEncoding.ASCII;
                //using (var reader = new System.IO.StreamReader(we.Response.GetResponseStream(), encoding)) {
                //    string responseText = reader.ReadToEnd();
                //    logText.AppendText("Response Content:" + Environment.NewLine);
                //    logText.AppendText(responseText + Environment.NewLine);
                //}

            }
        }

        private void Worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
            if (e.UserState != null) {
                if (e.UserState is Exception) {
                    ShowException((Exception)e.UserState, false);
                } else {
                    if (!String.IsNullOrWhiteSpace(e.UserState.ToString())) {
                        WriteMessageToLog(e.UserState.ToString());
                    }
                }
            }
        }

        private void WriteMessageToLog(String message) {
            App.Current.Dispatcher.Invoke((Action)(() => {
                lock (logText) {
                    logText.AppendText(message + Environment.NewLine);
                    logText.ScrollToEnd();
                }
            }));
        }

        private void DisableInterface() {
            SetInterface(false);
        }

        private void EnableInterface() {
            SetInterface(true);
        }

        private void SetInterface(bool enabled) {


        }


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (manager != null) {
                manager.Stop();
            }
        }





        //private void chkAllPages_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (chkAllPages.IsChecked == true)
        //    {
        //        Properties.Settings.Default.DetectAdditionalPages = true;
        //        Properties.Settings.Default.Save();
        //    }
        //    else {
        //        Properties.Settings.Default.DetectAdditionalPages = false;
        //        Properties.Settings.Default.Save();
        //    }
        //}

        private void pauseButton_Click(object sender, RoutedEventArgs e) {
            foreach (ADownloadable down in this.lstDownloadables.SelectedItems) {
                down.Pause();
                down.Save();
            }
        }

        private void pauseAllButton_Click(object sender, RoutedEventArgs e) {
            manager.PauseAll();
            e.Handled = true;
        }

        private void startButton_Click(object sender, RoutedEventArgs e) {
            foreach (ADownloadable down in this.lstDownloadables.SelectedItems) {
                down.Reset();
                down.Save();
            }
        }

        private void startAllButton_Click(object sender, RoutedEventArgs e) {
            manager.RestartAll();
            e.Handled = true;
        }

        private void startFailedButton_Click(object sender, RoutedEventArgs e) {
            this.manager.RestartFailed();
            e.Handled = true;
        }

        private void clearButton_Click(object sender, RoutedEventArgs e) {
            manager.ClearCompleted();
        }

        private void clearAllButton_Click(object sender, RoutedEventArgs e) {
            this.manager.ClearAllDownloads();
            e.Handled = true;
        }

        private void clearSelectedButton_Click(object sender, RoutedEventArgs e) {
            List<ADownloadable> to_remove = new List<ADownloadable>();
            foreach (ADownloadable down in this.lstDownloadables.SelectedItems) {
                down.Pause();
                to_remove.Add(down);
            }
            foreach (ADownloadable down in to_remove) {
                manager.Remove(down);
                down.Delete();
            }
            e.Handled = true;
        }

        private void addDownload_Click(object sender, RoutedEventArgs e) {
            try {
                MultiLineInput mli = new MultiLineInput();
                mli.Owner = this;
                if (mli.ShowDialog().Value) {
                    String text = mli.Contents;
                    List<Uri> uris = new List<Uri>();

                    foreach (string url in text.Split('\n')) {
                        if (String.IsNullOrWhiteSpace(url))
                            continue;

                        uris.Add(new Uri(url));
                    }

                    foreach (Uri uri in uris) {
                        urls.Enqueue(uri);
                    }

                    startProcess();
                }
            } catch (Exception ex) {
                ShowException(ex);
            }
        }

        private void downloadFolderButton_Click(object sender, RoutedEventArgs e) {
            setDownloadFolder();
        }

        private void contextMenuCopy_Click(object sender, RoutedEventArgs e) {
            StringBuilder text = new StringBuilder();
            foreach (ADownloadable item in lstDownloadables.SelectedItems) {
                text.AppendLine(item.URL.ToString());
            }
            if (text.Length > 0)
                Clipboard.SetText(text.ToString());
        }


        private void clearOutputButton_Click(object sender, RoutedEventArgs e) {
            logText.Clear();
        }


    }
}
