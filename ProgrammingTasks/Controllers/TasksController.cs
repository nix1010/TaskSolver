using ProgrammingTasks.Models;
using ProgrammingTasks.Models.API;
using ProgrammingTasks.Models.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace ProgrammingTasks.Controllers
{
    public class TasksController : ApiController
    {
        // GET api/tasks
        [HttpGet]
        public List<TaskDTO> GetTasks()
        {
            using (programming_tasksEntities entitities = new programming_tasksEntities())
            {
                List<TaskDTO> tasksDTO = new List<TaskDTO>();
                List<task> tasks = entitities.tasks.ToList();
                
                for (int i = 0; i < tasks.Count; ++i)
                {
                    tasksDTO.Add(new TaskDTO()
                    {
                        Id = tasks[i].id,
                        Title = tasks[i].title,
                        Description = tasks[i].description
                    });
                }

                return tasksDTO;
            }
         
        }

        // GET api/tasks/{id}
        [HttpGet]
        public TaskDTO GetTask(int id)
        {
            using (programming_tasksEntities entitities = new programming_tasksEntities())
            {
                task taskResult = entitities.tasks.Find(id);
                
                if (taskResult == null) //not found
                {
                    ExceptionHandler.ThrowException(HttpStatusCode.NotFound, "Task with id " + id + " not found");
                }

                return new TaskDTO()
                {
                    Id = taskResult.id,
                    Title = taskResult.title,
                    Description = taskResult.description
                };
            }
        }

        // POST api/tasks/{id}
        [HttpPost]
        [UserAuthentication]
        public RunResultDTO SendSolution(int id, [FromBody] TaskSolutionDTO taskSolution)
        {
            using (programming_tasksEntities entities = new programming_tasksEntities())
            {
                string username = Thread.CurrentPrincipal.Identity.Name; //get username from authentication

                user userResult = entities.users.First(user => user.username == username);
                task taskResult = entities.tasks.Find(id);

                if (taskResult == null)
                {
                    ExceptionHandler.ThrowException(HttpStatusCode.NotFound, "Task with id " + id + " doesn't exist");
                }

                CodeExecutor codeExecutor = new CodeExecutor();

                RunResultDTO runResult = codeExecutor.RunTask(taskSolution, taskResult);

                entities.users_solutions.Add(new users_solutions()
                {
                    user = userResult,
                    task = taskResult,
                    code = taskSolution.Code,
                    status = runResult.CorrectExamples == taskResult.examples.Count,
                    description = runResult.CorrectExamples == taskResult.examples.Count ?
                                    "Code executed successfully for all examples" :
                                    "Code failed at some example(s)",
                    date = DateTime.Now
                });

                entities.SaveChanges();
                
                return runResult;
            }
        }
    }
}