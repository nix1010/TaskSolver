using CodeEditorApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace CodeEditorApplication
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultWindow : Window
    {
        public ResultWindow()
        {
            InitializeComponent();
        }

        public void PopulateWindow(RunResult runResult)
        {
            this.Title += " [" + runResult.TaskTitle + "]";

            lblExamplesPassed.Text = runResult.CorrectExamples + " / " + runResult.exampleResults.Count;

            foreach (ExampleResult exampleResult in runResult.exampleResults)
            {
                foreach (PropertyInfo propertyInfo in typeof(ExampleResult).GetProperties())
                {
                    TextBox label = new TextBox()
                    {
                        Text = propertyInfo.Name + ": " + propertyInfo.GetValue(exampleResult, null),
                        FontSize = 14
                    };

                    StackPanel.Children.Add(label);
                }

                StackPanel.Children.Add(new Separator());
            }
        }
    }
}
