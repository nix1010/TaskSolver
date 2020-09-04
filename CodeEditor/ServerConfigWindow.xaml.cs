using CodeEditor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CodeEditor
{
    /// <summary>
    /// Interaction logic for ServerConfigWindow.xaml
    /// </summary>
    public partial class ServerConfigWindow : Window
    {
        public ServerConfigWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            txtBoxPort.Text = txtBoxPort.Text.Trim();

            if (new Regex("[^\\d]").IsMatch(txtBoxPort.Text)) // if contains any non digit character
            {
                MessageBoxCenterer.MessageBoxCentered(this, "Port number format is invalid", "Error");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Ok_Click(sender, new RoutedEventArgs(e.RoutedEvent));
            }
        }
    }
}
