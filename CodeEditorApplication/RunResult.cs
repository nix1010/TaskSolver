using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditorApplication
{
    public class RunResult
    {
        public int CorrectExamples { get; set; }
        public List<ExampleResult> exampleResults = new List<ExampleResult>();
    }
}
