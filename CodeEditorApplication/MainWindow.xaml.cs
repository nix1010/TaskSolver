using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CodeEditorApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string fileName;
        private string currentFileContent;
        private string username;
        private string password;
        private const string host = "http://localhost:50791/";
        private List<ProgrammingTask> tasks;
        private CompletionWindow completionWindow;

        public MainWindow()
        {
            InitializeComponent();

            ConfigureCodeCompletion();

            cmbProgrammingLanguage.ItemsSource =
                typeof(ProgrammingLanguage).GetFields().Select(element => element.GetValue(null).ToString());

            username = password = null;
            completionWindow = null;

            NewDocument();
        }

        private MessageBoxResult MessageBoxCentered(string messageBoxText, string caption,
            MessageBoxButton messageBoxButton = MessageBoxButton.OK)
        {
            MessageBoxCenterer.PrepToCenterMessageBoxOnWindow(this);
            
            return MessageBox.Show(messageBoxText, caption, messageBoxButton);
        }

        #region----- Button Events -----

        private void New_Click(object sender, RoutedEventArgs e)
        {
            NewDocument();
        }

        private void NewDocument()
        {
            fileName = null;
            avalonEdit.Clear();
            currentFileContent = avalonEdit.Text;

            UpdateFileNameTitle();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            
            if (openDialog.ShowDialog() == true)
            {
                using (StreamReader streamReader = new StreamReader(openDialog.FileName))
                {
                    string content = "";

                    while (!streamReader.EndOfStream)
                    {
                        if (content.Length > 0)
                        {
                            content += '\n';
                        }

                        content += streamReader.ReadLine();
                    }

                    avalonEdit.Text = currentFileContent = content;
                    fileName = openDialog.FileName;
                    
                    UpdateFileNameTitle();
                    SetLanguage(System.IO.Path.GetExtension(fileName));
                }
            }
        }

        private void SetLanguage(string extension)
        {
            foreach (FieldInfo fieldInfo in typeof(ProgrammingLanguage).GetFields())
            {
                ProgrammingLanguage programmingLanguage = (ProgrammingLanguage)fieldInfo.GetValue(null);

                if (programmingLanguage.Extension == extension)
                {
                    cmbProgrammingLanguage.SelectedValue = programmingLanguage.Name;
                    break;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Save();    
        }

        private bool Save()
        {
            if (fileName != null)
            {
                WriteContentToFile();

                return true;
            }
            else
            {
                return SaveAs();
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private bool SaveAs()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            if (saveDialog.ShowDialog() == true)
            {
                fileName = saveDialog.FileName;

                UpdateFileNameTitle();
                WriteContentToFile();

                return true;
            }

            return false;
        }

        private void WriteContentToFile()
        {
            using (StreamWriter streamWriter = new StreamWriter(fileName))
            {
                streamWriter.Write(avalonEdit.Text);

                currentFileContent = avalonEdit.Text;
                
                UpdateContentChangedIndicator();
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
            body.Add("Code", avalonEdit.Text);

            Dictionary<string, string> headers = new Dictionary<string, string>();

            headers.Add(HttpRequestHeader.ContentType.ToString(), "application/json");
            headers.Add(HttpRequestHeader.Authorization.ToString(), "User " + username + ":" + password);
            
            int selectedIndex = cmbTasks.SelectedIndex + 1;

            ucSpinner.Visibility = System.Windows.Visibility.Visible;
            btnSendSolution.IsEnabled = false;

            //don't block current thread
            Thread thread = new Thread(() =>
            {
                HttpResponseMessage responseMessage = PostRequest(host + "/api/tasks/" + selectedIndex, body, headers);
                
                string responseText = responseMessage.Content.ReadAsStringAsync().Result;

                Dispatcher.Invoke(new Action(() => { ucSpinner.Visibility = System.Windows.Visibility.Hidden; }));
                Dispatcher.Invoke(new Action(() => { btnSendSolution.IsEnabled = true; }));

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

                    MessageBoxCentered(JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText)["Message"],
                        responseMessage.StatusCode.ToString());
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

        private void RefreshTasks_Click(object sender, RoutedEventArgs e)
        {
            PopulateTasksAsynchronically();
        }

        private void CodeCompletion_Click(object sender, RoutedEventArgs e)
        {
            ConfigureCodeCompletion();
        }

        #endregion

        #region----- Window Events -----

        private void Window_Loaded(object sender, EventArgs e)
        {
            PopulateTasksAsynchronically();
        }

		private void PopulateTasksAsynchronically()
		{
			Thread thread = new Thread(PopulateTasks);

            thread.IsBackground = true;
            thread.Start();
		}
		
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save_Click(sender, e);
            }
            else if ((e.Key == Key.Add || e.Key == Key.OemPlus) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                avalonEdit.FontSize += 1;
            }
            else if ((e.Key == Key.Subtract || e.Key == Key.OemMinus) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (avalonEdit.FontSize > 1)
                {
                    avalonEdit.FontSize -= 1;
                }
            }
        }

        private void PopulateTasks()
        {
            HttpResponseMessage responseMessage = GetRequest(host + "api/tasks");

            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
                string responseText = responseMessage.Content.ReadAsStringAsync().Result;

                tasks = JsonConvert.DeserializeObject<List<ProgrammingTask>>(responseText);

                Dispatcher.Invoke(new Action(() =>
                {
                    cmbTasks.Items.Clear();
                }));

                for (int i = 0; i < tasks.Count; ++i)
                {
                    try
                    {
                        // force WPF to render UI elemnts
                        Dispatcher.Invoke(new Action(() =>
                        {            
                            cmbTasks.Items.Add(tasks[i].Title);
                        }));
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.Title.Contains("*"))
            {
                MessageBoxResult messageBoxResult =
                    MessageBoxCentered("Do you want to save changes?", "Alert", MessageBoxButton.YesNoCancel);

                if ((messageBoxResult == MessageBoxResult.Yes && !Save())
                    ||
                    messageBoxResult == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        #endregion

        #region --- Component Events ---
        
        private void cmbProgrammingLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProgrammingLanguage.SelectedIndex != -1)
            {
                avalonEdit.SyntaxHighlighting = 
                    HighlightingManager.Instance.GetDefinition((string)cmbProgrammingLanguage.SelectedItem);
            }
        }

        private void avalonEdit_TextChanged(object sender, EventArgs e)
        {
            UpdateContentChangedIndicator();
        }

        private void ConfigureCodeCompletion()
        {
            if (CodeCompletion.IsChecked)
            {
                avalonEdit.TextArea.TextEntered += avalonEdit_TextEntered;
                avalonEdit.TextArea.TextEntering += avalonEdit_TextEntering;
            }
            else
            {
                avalonEdit.TextArea.TextEntered -= avalonEdit_TextEntered;
                avalonEdit.TextArea.TextEntering -= avalonEdit_TextEntering;
            }
        }

        private void avalonEdit_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (char.IsLetter(e.Text[0]) || e.Text[0] == '_')
            {
                int pos = avalonEdit.TextArea.Caret.Offset;
                string leftFromInput = avalonEdit.Text.Substring(0, pos);
                string rightFromInput = avalonEdit.Text.Substring(pos, avalonEdit.Text.Length - pos);

                //match first character in left half which is not word character
                Match matchLeft = Regex.Match(leftFromInput, "[^a-zA-Z&_]", RegexOptions.RightToLeft);
                //match first character in right half which is not word character
                Match matchRight = Regex.Match(rightFromInput, "[^a-zA-Z&_&0-9]");

                int posLeft = matchLeft.Index;
                int posRight = matchRight.Index;

                //if match then move to first character of word
                if (matchLeft.Success)
                {
                    posLeft += 1;
                }

                //if no match subtract current pos from end of file, later it increments
                if (!matchRight.Success)
                {
                    posRight = avalonEdit.Text.Length - pos;
                }

                string currentWord = avalonEdit.Text.Substring(posLeft, ((posRight + pos) - posLeft));
                
                //remove 'currentWord' from left and right halfs
                leftFromInput = leftFromInput.Substring(0, posLeft);
                rightFromInput = rightFromInput.Substring(posRight, rightFromInput.Length - posRight);

                //match all words that start with 'currentWord' (case insensitive)
                MatchCollection matchCollection = 
                    Regex.Matches(leftFromInput + rightFromInput, "(?i)\\b(" + currentWord + "\\w*)\\b");

                if (matchCollection.Count > 0)
                {
                    completionWindow = new CompletionWindow(avalonEdit.TextArea);

                    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                    HashSet<ICompletionData> uniqueData = new HashSet<ICompletionData>();

                    //insert matched words into window
                    foreach (Match match in matchCollection)
                    {
                        CompletionData completionData = new CompletionData(match.Value, posLeft, currentWord.Length);

                        //if not already inserted
                        if (uniqueData.Add(completionData))
                        {
                            data.Add(completionData);
                        }
                    }

                    completionWindow.Show();
                    completionWindow.Closed += delegate
                    {
                        completionWindow = null;
                    };
                }
                else if(completionWindow != null) //if nothing matches close window if opened
                {
                    completionWindow.Close();
                }
            }
        }

        private void avalonEdit_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
        }

        private void UpdateContentChangedIndicator()
        {           
            if (!avalonEdit.Text.Equals(currentFileContent))
            {
                if (!this.Title.Contains("*"))
                {
                    this.Title += "*";
                }
            }
            else
            {
                this.Title = this.Title.Replace("*", "");
            }
        }

        private void UpdateFileNameTitle()
        {
            int idx = this.Title.IndexOf(" [");

            if (idx != -1)
            {
                this.Title = this.Title.Remove(idx);
            }

            if(fileName != null)
            {
                this.Title += " [" + System.IO.Path.GetFileName(fileName) + "]";
            }
        }

        #endregion

        #region----- HTTP Requests -----

        private HttpResponseMessage PostRequest(string url, Dictionary<string, string> body, Dictionary<string, string> headers = null)
        {
            HttpClient client = new HttpClient();
            HttpStatusCode statusCode;
            string responseText = "";

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
                statusCode = HttpStatusCode.ServiceUnavailable;
                responseText = JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    {"StatusCode", statusCode.ToString()},
                    {"Message", "Error sending request"}
                });
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
                statusCode = HttpStatusCode.ServiceUnavailable;
                responseText = JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    {"StatusCode", statusCode.ToString()},
                    {"Message", "Error sending request"}
                });
            }

            return new HttpResponseMessage(statusCode) { Content = new StringContent(responseText) };
        }
        #endregion
    }
}
