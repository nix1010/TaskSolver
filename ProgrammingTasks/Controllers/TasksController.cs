using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ProgrammingTasks.Models;

namespace ProgrammingTasks.Controllers
{
    public class TasksController : ApiController
    {
        // GET api/tasks
        [HttpGet]
        public string getTasks()
        {
            return "api/tasks";
        }

        // GET api/tasks/{id}
        [HttpGet]
        public string getTask(int id)
        {
            return "api/tasks/" + id;
        }

        // POST api/tasks/{id}
        [HttpPost]
        public HttpResponseMessage sendSolution(int id, [FromBody] TaskSolutionDTO dto)
        {
           return Request.CreateResponse(HttpStatusCode.OK, dto.SolutionCode + " "  + dto.ProgrammingLanguage.ToString());
        }
        
    }
}