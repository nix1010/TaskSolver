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

            username = password = null;

            NewDocument();
        }

        private MessageBoxResult MessageBoxCentered(string messageBoxText, string caption)
        {
            MessageBoxCenterer.PrepToCenterMessageBoxOnWindow(this);
            return MessageBox.Show(messageBoxText, caption);
        }

        #region----- Button Events -----

        private void New_Click(object sender, RoutedEventArgs e)
        {
            NewDocument();
        }

        private void NewDocument()
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
            if (username == null || password == null)
            {
                if (!ShowLoginWindow())
                {
                    return;
                }
            }

            if (cmbTasks.SelectedIndex == -1)
            {
                MessageBoxCentered("Please select task", "Warning");
                return;
            } 
            
            if (cmbProgrammingLanguage.SelectedIndex == -1)
            {
                MessageBoxCentered("Please select programming language", "Warning");
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
            Thread thread = new Thread(() =>
            {
                HttpResponseMessage responseMessage = PostRequest(host + "/api/tasks/" + selectedIndex, body, headers);

                Dispatcher.Invoke(new Action(() => { ucSpinner.Visibility = System.Windows.Visibility.Hidden; }));
                
                string responseText = responseMessage.Content.ReadAsStringAsync().Result;
                
                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    RunResult runResult = JsonConvert.DeserializeObject<RunResult>(responseText);

                    Dispatcher.Invoke(new Action(() =>
                    {
                        ResultWindow resultWindow = new ResultWindow();

                        //center
                        resultWindow.Top = this.Top + (this.Height - resultWindow.Height) / 2;
                        resultWindow.Left = this.Left + (this.Width - resultWindow.Width) / 2;

                        resultWindow.PopulateWindow(runResult);
                        
                        resultWindow.Show();
                    }));
                }
                else
                {
                    if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Logout();
                        }));
                    }

                    
                    MessageBoxCentered(JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText)["Message"], "Error");
                }

            });

            thread.IsBackground = true;
            thread.Start();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void Logout()
        {
            username = password = null;

            Menu.Items.RemoveAt(Menu.Items.Count - 1);
        }

        private bool ShowLoginWindow()
        {
            LoginWindow loginWindow = new LoginWindow();

            //center
            loginWindow.Top = this.Top + (this.Height - loginWindow.Height) / 2;
            loginWindow.Left = this.Left + (this.Width - loginWindow.Width) / 2;
            
            loginWindow.ShowDialog();
            
            bool submitted = loginWindow.IsSubmitted();
            
            if (submitted)
            {
                username = loginWindow.txtBoxUsername.Text;
                password = loginWindow.passwordBox.Password;

                MenuItem menuItem = new MenuItem()
                {
                    Header = "_Logout",
                    Name = "Logout"
                };

                menuItem.Click += Logout_Click;

                Menu.Items.Add(menuItem);
            }
            
            return submitted;
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTasks.SelectedIndex != -1)
            {
                MessageBoxCentered(tasks[cmbTasks.SelectedIndex].Description, tasks[cmbTasks.SelectedIndex].Title);
            }
        }
        #endregion

        #region----- Window Events -----

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            Thread thread = new Thread(PopulateTasks);
            
            thread.IsBackground = true;
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
                    try
                    {
                        // force WPF to render UI elemnts
                        Dispatcher.Invoke(new Action(() => { cmbTasks.Items.Add(tasks[i].Title); }));
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            else
            {
                MessageBoxCentered("Failed to obtain tasks", responseMessage.StatusCode.ToString());
            }
        }

        #endregion

        #region----- Key Events -----

        private void MainWindow_TabKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true;

                rtbEditor.CaretPosition.InsertTextInRun("\t");   
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
