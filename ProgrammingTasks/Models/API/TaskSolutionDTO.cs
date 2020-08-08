using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProgrammingTasks.Models.API
{
    public class TaskSolutionDTO
    {
        public ProgrammingLanguage ProgrammingLanguage { get; set; }
        public string Code { get; set; }
    }
}