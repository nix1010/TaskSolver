﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProgrammingTasks.Models.API
{
    public class RunResultDTO
    {
        public int CorrectExamples { get; set; }
        public List<ExampleResultDTO> exampleResults = new List<ExampleResultDTO>();
    }
}