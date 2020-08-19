using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditorApplication.Models
{
    public class RunResult
    {
        public string TaskTitle { get; set; }
        public int CorrectExamples { get; set; }
        public List<ExampleResult> exampleResults = new List<ExampleResult>();
    }
}
