using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProgrammingTasks.Models.API
{
    public class TaskDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}