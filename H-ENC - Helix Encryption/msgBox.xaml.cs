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

namespace H_ENC___Helix_Encryption
{
    /// <summary>
    /// Interaction logic for msgBox.xaml
    /// </summary>
    public partial class msgBox : Window
    {
        public msgBox()
        {
            InitializeComponent();
                        this.Closed += new EventHandler(settings_Closed);
        }

        void settings_Closed(object sender, EventArgs e)
        {
            this.Close();
        }

        private void msgboxClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void msgboxClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
