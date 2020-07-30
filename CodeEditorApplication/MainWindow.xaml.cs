using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditorApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string fileName;
        private string username;
        private string password;
        private const string host = "http://localhost:50791/";
        private List<CodeEditorApplication.Task> tasks;

        public MainWindow()
        {
            
            InitializeComponent();
            
            this.KeyDown += new KeyEventHandler(MainWindow_TabKeyDown);
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            
            cmbProgrammingLanguage.ItemsSource = Enum.GetValues(typeof(ProgrammingLanguage));

            username = password = "user1";

            //password = Sha256(password);

            New_Click(null, null);
        }

        #region----- Button Events -----

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
            if (cmbProgrammingLanguage.SelectedIndex == -1)
            {
                MessageBox.Show("Please select langauge", "Warning");
                return;
            }

            if (cmbTasks.SelectedIndex == -1)
            {
                MessageBox.Show("Please select task", "Warning");
                return;
            }

            Dictionary<string, string> body = new Dictionary<string, string>();

            body.Add("ProgrammingLanguage", cmbProgrammingLanguage.SelectedItem.ToString());
            body.Add("Code", new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd).Text);

            Dictionary<string, string> headers = new Dictionary<string, string>();

            headers.Add(HttpRequestHeader.Authorization.ToString(), "User " + username + ":" + password);

            int selectedIndex = cmbTasks.SelectedIndex + 1;

            ucSpinner.Visibility = System.Windows.Visibility.Visible;

            //don't block current thread
            new Thread(() =>
                {
                    HttpResponseMessage responseMessage = PostRequest(host + "/api/tasks/" + selectedIndex, body, headers);

                    Dispatcher.Invoke(new Action(() => { ucSpinner.Visibility = System.Windows.Visibility.Hidden; }));

                    if (responseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        string responseText = responseMessage.Content.ReadAsStringAsync().Result;

                        MessageBox.Show(responseText);
                        RunResult runResult = JsonConvert.DeserializeObject<RunResult>(responseText);

                        MessageBox.Show("Correct: " + runResult.CorrectExamples, "Result");
                        /*
                        foreach (ExampleResult r in runResult.exampleResults)
                        {
                            MessageBox.Show(r.Input + " " + r.Output + " " + r.SolutionResult + " " + r.Description, "Result");
                        }
                        */
                    }
                    else
                    {
                        MessageBox.Show(responseMessage.StatusCode.ToString() + ": "
                            + responseMessage.Content.ReadAsStringAsync().Result, "Error");
                    }

                }).Start();
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTasks.SelectedIndex != -1)
            {
                MessageBox.Show(tasks[cmbTasks.SelectedIndex].Description, tasks[cmbTasks.SelectedIndex].Title);
            }
        }
        #endregion

        #region----- Window Events -----

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            Thread thread = new Thread(PopulateTasks);

            thread.Start(); 
        }

        private void PopulateTasks()
        {
            HttpResponseMessage responseMessage = GetRequest(host + "api/tasks");

            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
                string responseText = responseMessage.Content.ReadAsStringAsync().Result;

                tasks = JsonConvert.DeserializeObject<List<CodeEditorApplication.Task>>(responseText);

                for (int i = 0; i < tasks.Count; ++i)
                {
                    // force WPF to render UI elemnts
                    Dispatcher.Invoke(new Action(() => { cmbTasks.Items.Add(tasks[i].Title); }));
                }
            }
            else
            {
                MessageBox.Show(responseMessage.StatusCode + ": " + responseMessage.Content.ReadAsStringAsync().Result);
            }
        }

        #endregion

        #region----- Key Events -----

        private void MainWindow_TabKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                rtbEditor.CaretPosition.InsertTextInRun("\t");
                rtbEditor.Focus();
            }
        }
        #endregion

        #region----- HTTP Requests -----

        private HttpResponseMessage PostRequest(string url, Dictionary<string, string> body, Dictionary<string, string> headers = null)
        {
            HttpClient client = new HttpClient();
            HttpStatusCode statusCode;
            string responseText;

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            
            FormUrlEncodedContent content = new FormUrlEncodedContent(body);
            Task<HttpResponseMessage> response = client.PostAsync(url, content);

            try
            {
                statusCode = response.Result.StatusCode;
                responseText = response.Result.Content.ReadAsStringAsync().Result;
            }
            catch (Exception)
            {
                statusCode = HttpStatusCode.RequestTimeout;
                responseText = "Error sendig request";
            }

            return new HttpResponseMessage(statusCode) { Content = new StringContent(responseText) };
        }

        private HttpResponseMessage GetRequest(string url, Dictionary<string, string> headers = null)
        {
            HttpClient client = new HttpClient();
            HttpStatusCode statusCode;
            string responseText;

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            
            Task<HttpResponseMessage> response = client.GetAsync(url);
            
            try
            {
                 statusCode = response.Result.StatusCode;
                 responseText = response.Result.Content.ReadAsStringAsync().Result;
            }
            catch(Exception)
            {
                statusCode = HttpStatusCode.RequestTimeout;
                responseText = "Error sending request";
            }

            return new HttpResponseMessage(statusCode) { Content = new StringContent(responseText) };
        }
        #endregion
    }
}
