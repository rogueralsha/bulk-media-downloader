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

namespace bulk_image_downloader
{
    /// <summary>
    /// Interaction logic for MultiLineInput.xaml
    /// </summary>
    public partial class MultiLineInput : Window
    {
        public MultiLineInput()
        {
            InitializeComponent();
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
    }
}
