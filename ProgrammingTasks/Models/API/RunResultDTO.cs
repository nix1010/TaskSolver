using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProgrammingTasks.Models.API
{
    public class RunResultDTO
    {
        public string TaskTitle { get; set; }
        public int CorrectExamples { get; set; }
        public List<ExampleResultDTO> exampleResults;
        
        public RunResultDTO()
        {
            this.exampleResults = new List<ExampleResultDTO>();
        }
    }
}