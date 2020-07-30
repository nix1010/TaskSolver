using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;

namespace ProgrammingTasks.Models
{
    public class ErrorInfo
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}