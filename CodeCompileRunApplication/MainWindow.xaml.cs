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
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Controls;

namespace CodeCompileRunApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string fileName;
        private ProgrammingLanguage programmingLanguage;

        public MainWindow()
        {
            InitializeComponent();

            cmbProgrammingLanguage.ItemsSource = Enum.GetValues(typeof(ProgrammingLanguage));
            fileName = null;

        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            
            if (openDialog.ShowDialog() == true)
            {
                using (FileStream fileStream = new FileStream(openDialog.FileName, FileMode.Open))
                {
                    TextRange textRange = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);

                    textRange.Load(fileStream, DataFormats.Text);
                }     
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            if (saveDialog.ShowDialog() == true)
            {
                using (FileStream fileStream = new FileStream(saveDialog.FileName, FileMode.Create))
                {
                    TextRange textRange = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);

                    textRange.Save(fileStream, DataFormats.Text);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmbProgrammingLanguage_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (cmbProgrammingLanguage.SelectedItem != null)
            {
                programmingLanguage = (ProgrammingLanguage)cmbProgrammingLanguage.SelectedItem;
            }
        }
    }
}
