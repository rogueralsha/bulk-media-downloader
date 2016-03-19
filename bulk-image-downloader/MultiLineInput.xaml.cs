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

namespace BulkMediaDownloader
{
    /// <summary>
    /// Interaction logic for MultiLineInput.xaml
    /// </summary>
    public partial class MultiLineInput : Window
    {
        public MultiLineInput()
        {
            InitializeComponent();
            if(Clipboard.ContainsText()) {
                string text = Clipboard.GetText();
                string[] lines = text.Split(new char[] { '\n','\r' });
                foreach(string line in lines) {
                    Uri test;
                    if(Uri.TryCreate(line, UriKind.Absolute, out test)) {
                        textBox.AppendText(test.ToString() +Environment.NewLine);
                    }
                }
            }
        }
        public String Contents
        {
            get
            {
                return textBox.Text;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e) {
            this.DialogResult = false;
            this.Close();
        }

        private void clearButton_Click(object sender, RoutedEventArgs e) {
            textBox.Clear();
        }
    }
}
