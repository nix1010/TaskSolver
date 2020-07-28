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
using System.Net;
using System.Net.Http;
using System.IO;
using Microsoft.Win32;

namespace CodeEditorApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string fileName;
        private const string host = "http://localhost:50791/";
        private ProgrammingLanguage programmingLanguage;

        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(MainWindow_TabKeyDown);

            cmbProgrammingLanguage.ItemsSource = Enum.GetValues(typeof(ProgrammingLanguage));
            fileName = null;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            fileName = null;
            rtbEditor.Document.Blocks.Clear();
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
                
                fileName = openDialog.FileName;
                
                SetLanguage(System.IO.Path.GetExtension(fileName));
            }
        }

        private void SetLanguage(string extension)
        {
            switch (extension)
            {
                case ".cpp": cmbProgrammingLanguage.SelectedValue = ProgrammingLanguage.C_PLUS_PLUS; break;
                case ".cs": cmbProgrammingLanguage.SelectedValue = ProgrammingLanguage.C_SHARP; break;
                case ".java": cmbProgrammingLanguage.SelectedValue = ProgrammingLanguage.JAVA; break;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (fileName != null)
            {
                using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                {
                    TextRange textRange = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);

                    textRange.Save(fileStream, DataFormats.Text);
                }
            }
            else
            {
                SaveAs_Click(sender, e);
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            if (saveDialog.ShowDialog() == true)
            {
                using (FileStream fileStream = new FileStream(saveDialog.FileName, FileMode.Create))
                {
                    TextRange textRange = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);

                    textRange.Save(fileStream, DataFormats.Text);
                }

                fileName = saveDialog.FileName;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void cmbProgrammingLanguage_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (cmbProgrammingLanguage.SelectedItem != null)
            {
                programmingLanguage = (ProgrammingLanguage)cmbProgrammingLanguage.SelectedItem;
            }
        }

        private void MainWindow_TabKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                rtbEditor.CaretPosition.InsertTextInRun("\t");
                rtbEditor.Focus();
            }
        }

        private KeyValuePair<HttpStatusCode, string> PostRequest(string url, Dictionary<string, string> body)
        {
            HttpClient client = new HttpClient();

            FormUrlEncodedContent content = new FormUrlEncodedContent(body);
            Task<HttpResponseMessage> response = client.PostAsync(url, content);

            HttpStatusCode statusCode = response.Result.StatusCode;
            string responseText = response.Result.Content.ReadAsStringAsync().Result;

            return new KeyValuePair<HttpStatusCode, string>(statusCode, responseText);
        }

        private KeyValuePair<HttpStatusCode, string> GetRequest(string url)
        {
            HttpClient client = new HttpClient();

            Task<HttpResponseMessage> response = client.GetAsync(url);

            HttpStatusCode statusCode = response.Result.StatusCode;
            string responseText = response.Result.Content.ReadAsStringAsync().Result;

            return new KeyValuePair<HttpStatusCode, string>(statusCode, responseText);
        }
    }
}
