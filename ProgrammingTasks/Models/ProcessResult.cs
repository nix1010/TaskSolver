using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProgrammingTasks.Models
{
    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
    }
}