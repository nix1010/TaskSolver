﻿using CodeEditor.Models;
using CodeEditor.Utility;
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CodeEditor
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
        private string hostName;
        private string port;
        private string hostUrl;
        private readonly Timer timer;
        private List<ProgrammingTask> tasks;
        private CompletionWindow completionWindow;

        public MainWindow()
        {
            InitializeComponent();

            SetHostUrl("localhost", "50791");
            
            timer = new Timer((Object stateInfo) =>
            {
                PingHost();
            }, null, 0, 10000);

            ConfigureCodeCompletion();

            cmbProgrammingLanguage.ItemsSource =
                typeof(ProgrammingLanguage).GetFields().Select(element => element.GetValue(null).ToString());

            if (cmbProgrammingLanguage.Items.Count > 0)
            {
                cmbProgrammingLanguage.SelectedItem = cmbProgrammingLanguage.Items[0];
            }

            username = password = null;
            completionWindow = null;

            NewDocument();
        }

        private void SetHostUrl(string hostName, string port)
        {
            this.hostName = hostName;
            this.port = port;
            this.hostUrl = "http://" + hostName + ":" + port + "/";
        }

        private void SetLabelInfo(string content)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                lblInfo.Content = content;
            }));
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

            if (UnsavedChanges())
            {
                return;
            }

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

        private bool UnsavedChanges()
        {
            if (this.Title.Contains("*"))
            {
                MessageBoxResult messageBoxResult =
                    MessageBoxCenterer.MessageBoxCentered(this, "Do you want to save changes?", "Alert", MessageBoxButton.YesNoCancel);

                if ((messageBoxResult == MessageBoxResult.Yes && !Save())
                    ||
                    messageBoxResult == MessageBoxResult.Cancel)
                {
                    return true;
                }
            }

            return false;
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
                MessageBoxCenterer.MessageBoxCentered(this, "Please select task", "Warning");
                return;
            } 
            
            if (cmbProgrammingLanguage.SelectedIndex == -1)
            {
                MessageBoxCenterer.MessageBoxCentered(this, "Please select programming language", "Warning");
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
            SetLabelInfo("Sending solution...");

            //don't block current thread
            Thread thread = new Thread(() =>
            {
                HttpResponseMessage responseMessage = HttpRequest(hostUrl + "/api/tasks/" + selectedIndex, HttpMethod.Post, body,
                    headers);
                
                string responseText = responseMessage.Content.ReadAsStringAsync().Result;

                Dispatcher.Invoke(new Action(() => { ucSpinner.Visibility = System.Windows.Visibility.Hidden; }));
                Dispatcher.Invoke(new Action(() => { btnSendSolution.IsEnabled = true; }));
                SetLabelInfo("");

                if (responseMessage.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    UpdateServerStatus(false);
                }
                else
                {
                    UpdateServerStatus(true);
                }

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
                    
                    MessageBoxCenterer.MessageBoxCentered(this, JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText)["Message"],
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
            
            bool? submitted = loginWindow.ShowDialog();
            
            if (submitted == true)
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
            
            return submitted == true;
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTasks.SelectedIndex != -1)
            {
                MessageBoxCenterer.MessageBoxCentered(this, tasks[cmbTasks.SelectedIndex].Description, tasks[cmbTasks.SelectedIndex].Title);
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

        private void ServerConfig_Click(object sender, RoutedEventArgs e)
        {
            ServerConfigWindow serverConfig = new ServerConfigWindow();
            serverConfig.txtBoxHostUrl.Text = hostName;
            serverConfig.txtBoxPort.Text = port;

            //center
            serverConfig.Top = this.Top + (this.Height - serverConfig.Height) / 2;
            serverConfig.Left = this.Left + (this.Width - serverConfig.Width) / 2;
            
            bool? result = serverConfig.ShowDialog();
            
            if (result == true)
            {
                SetHostUrl(serverConfig.txtBoxHostUrl.Text.Trim(), serverConfig.txtBoxPort.Text.Trim());

                Thread t = new Thread(() =>
                {
                    PingHost();
                });
                t.IsBackground = true;
                t.Start();
            }
        }

        #endregion

        #region----- Window Events -----

        private void Window_Loaded(object sender, EventArgs e)
        {
            PopulateTasksAsynchronically();
        }

        private void PingHost()
        {
            try
            {
                using (TcpClient client = new TcpClient(hostName, Convert.ToInt32(port)))
                {
                    UpdateServerStatus(true);
                }
            }
            catch (Exception)
            {
                UpdateServerStatus(false);
            }
        }

        private void UpdateServerStatus(bool status)
        {
            if (status)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    lblServerStatus.Foreground = System.Windows.Media.Brushes.Green;
                    lblServerStatus.Content = "Available";
                }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    lblServerStatus.Foreground = System.Windows.Media.Brushes.Red;
                    lblServerStatus.Content = "Not Available";
                }));
            }
        }

		private void PopulateTasksAsynchronically()
		{
            SetLabelInfo("Refreshing tasks...");

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
            HttpResponseMessage responseMessage = HttpRequest(hostUrl + "api/tasks", HttpMethod.Get);

            SetLabelInfo("");

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

                Dispatcher.Invoke(new Action(() =>
                {
                    if (cmbTasks.Items.Count > 0)
                    {
                        cmbTasks.SelectedItem = cmbTasks.Items[0];
                    }
                }));

                UpdateServerStatus(true);
            }
            else
            {
                if (responseMessage.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    UpdateServerStatus(false);
                }

                MessageBoxCenterer.MessageBoxCentered(this, "Failed to obtain tasks", responseMessage.StatusCode.ToString());
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (UnsavedChanges())
            {
                e.Cancel = true;
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

        private HttpResponseMessage HttpRequest(string url, HttpMethod method, Dictionary<string, string> body = null,
            Dictionary<string, string> headers = null)
        {
            HttpClient client = new HttpClient();
            FormUrlEncodedContent content = null;
            HttpStatusCode statusCode;
            string responseText = "";

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (body != null)
            {
                content = new FormUrlEncodedContent(body);    
            }

            try
            {
                Task<HttpResponseMessage> response = client.SendAsync(new HttpRequestMessage(method, url)
                {
                    Content = content,
                });

                statusCode = response.Result.StatusCode;
                responseText = response.Result.Content.ReadAsStringAsync().Result;
            }
            catch (UriFormatException e)
            {
                statusCode = HttpStatusCode.BadRequest;
                responseText = JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    {"StatusCode", statusCode.ToString()},
                    {"Message", e.Message}
                });
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
        
        #endregion
    }
}
