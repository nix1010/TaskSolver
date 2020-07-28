using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProgrammingTasks.Models.API
{
    public class TaskSolutionDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public ProgrammingLanguage ProgrammingLanguage { get; set; }
        public int TaskId { get; set; }
        public string Code { get; set; }
    }
}