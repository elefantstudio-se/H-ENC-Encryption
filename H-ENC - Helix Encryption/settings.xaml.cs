using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Threading;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Checksums;

namespace H_ENC___Helix_Encryption
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class settings : MetroWindow
    {
        public settings()
        {
            InitializeComponent();
            this.Closed += new EventHandler(settings_Closed);
        }

        void settings_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
            
        }
        private void txSlValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            string dotremove = "";
            txSlValue.Text += dotremove;
        }
        private void settings_Onload(object sender, RoutedEventArgs e)
        {
            //hämtar sparade värden ifrån application settings.
            txDefault.Text = Properties.Settings.Default.defaultCompress;
        }

        public partial class MainWindow : UserControl
        {

        }

        private void txDefault_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            txDefault.Text = dialog.SelectedPath;
        }

        private void btnSpara_Click(object sender, RoutedEventArgs e)
        {
                //Sparar infon till application settings.
                Properties.Settings.Default.defaultFolder = txDefault.Text;
                Properties.Settings.Default.defaultCompress = txDefault.Text;
                // spara till application user settings... Enklare än att fiffla med INI filer.
                Properties.Settings.Default.Save();
            }

        private void txDefault_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        }
    }
