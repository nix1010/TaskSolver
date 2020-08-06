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
using System.Reflection;

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
            lblExamplesPassed.Content = runResult.CorrectExamples + " / " + runResult.exampleResults.Count;

            foreach (ExampleResult exampleResult in runResult.exampleResults)
            {
                foreach (PropertyInfo propertyInfo in typeof(ExampleResult).GetProperties())
                {
                    Label label = new Label()
                    {
                        Content = propertyInfo.Name + ": " + propertyInfo.GetValue(exampleResult, null),
                        FontSize = 14
                    };

                    StackPanel.Children.Add(label);
                }

                StackPanel.Children.Add(new Separator());
            }
        }
    }
}
