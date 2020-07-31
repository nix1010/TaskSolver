﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ProgrammingTasks.Models.API;
using ProgrammingTasks.Models.Entity;
using ProgrammingTasks.Models;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;

namespace ProgrammingTasks.Controllers
{
    public class TasksController : ApiController
    {
        private readonly string codeLocation;
        private readonly string binLocation;

        public TasksController()
        {
            this.codeLocation = HttpRuntime.AppDomainAppPath + "code";
            this.binLocation = codeLocation + "\\bin";
        }

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
                    tasksDTO.Add(new TaskDTO(){
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

                return new TaskDTO() {
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
            if (!Directory.Exists(binLocation))
            {
                Directory.CreateDirectory(binLocation);
            }

            using (programming_tasksEntities entities = new programming_tasksEntities())
            {
                string username = Thread.CurrentPrincipal.Identity.Name; //get username from authentication

                user userResult = entities.users.First(user => user.username == username);
                task taskResult = entities.tasks.Find(id);

                if (taskResult == null)
                {
                    ExceptionHandler.ThrowException(HttpStatusCode.NotFound, "Task with id " + id + " doesn't exist");
                }

                RunResultDTO runResult = RunExamples(taskSolution, taskResult.examples);
                
                entities.users_solutions.Add(new users_solutions()
                                                {
                                                    user = userResult,
                                                    task = taskResult,
                                                    code = taskSolution.Code,
                                                    status = runResult.CorrectExamples == taskResult.examples.Count,
                                                    description = runResult.CorrectExamples == taskResult.examples.Count ?
                                                                    "Code executed successfully for all examples" :
                                                                    "Code failed at some example(s)"
                                                });
                entities.SaveChanges();
                
                return runResult;
            }
        }

        private RunResultDTO RunExamples(TaskSolutionDTO taskSolution, ICollection<example> examples)
        {
            RunResultDTO runResult = new RunResultDTO();

            ProcessResult compileResult = Compile(taskSolution);

            if (compileResult.ExitCode != 0)
            {
                ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "Compile error: " + compileResult.Error);
            }

            foreach (example example in examples)
            {
                ProcessResult processInfo = Run(taskSolution, example);
                string description;

                if (processInfo.ExitCode == 0)
                {
                    if (processInfo.Output == example.output)
                    {
                        runResult.CorrectExamples++;
                        description = "Code runs successfully";
                    }
                    else
                    {
                        description = "Code failed";
                    }
                }
                else
                {
                    description = "Error running code";
                }
                
                runResult.exampleResults.Add(new ExampleResultDTO() { 
                                                Input = example.input,
                                                Output = example.output,                                
                                                SolutionResult = processInfo.Output + processInfo.Error,
                                                Description = description
                                            });
            }

            return runResult;
        }

        private ProcessResult Compile(TaskSolutionDTO taskSolution)
        {
            string fileName = "";
            string command = "";
            StreamWriter writer = null;

            if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.JAVA)
            {
                if (taskSolution.Code == null || taskSolution.Code.Trim() == "")
                {
                    ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "Empty file");
                }

                fileName = codeLocation + "\\Main.java";
                command = "/C javac -d " + binLocation + " " + fileName;
            }
            else if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.C_PLUS_PLUS)
            {
                fileName = codeLocation + "\\main.cpp";
                command = "/C gcc -o " + binLocation + "\\mainCpp " + fileName;
            }
            else if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.C_SHARP)
            {
                fileName = codeLocation + "\\main.cs";
                command = "/C csc -out:" + binLocation + "\\mainCSharp.exe " + fileName;
            }
            else
            {
                ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "Specified language not supported");
            }
           
            //write code to file

            writer = new StreamWriter(fileName);
            writer.Write(taskSolution.Code);
            writer.Close();

            return RunProcess(command);
        }

        private ProcessResult Run(TaskSolutionDTO taskSolution, example example)
        {
            string command = "";

            if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.JAVA)
            {
                command = "/C java -cp " + binLocation + "; Main";
            }
            else if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.C_PLUS_PLUS)
            {
                command = "/C " + binLocation + "\\mainCpp";
            }
            else if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.C_SHARP)
            {
                command = "/C " + binLocation + "\\mainCSharp";
            }
            else
            {
                ExceptionHandler.ThrowException(HttpStatusCode.NotFound, "Specified language not supported");
            }

            return RunProcess(command, example.input.Split(';'));
        }

        private ProcessResult RunProcess(string argument, string[] inputs = null)
        {
            string result = "";
            string error = "";

            StreamWriter inputWriter = null;
            StreamReader outputReader = null;
            StreamReader errorReader = null;
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = argument;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            process.StartInfo = startInfo;

            process.Start();
            
            inputWriter = process.StandardInput;
            outputReader = process.StandardOutput;
            errorReader = process.StandardError;

            if (inputs != null)
            {
                for (int i = 0; i < inputs.Length; ++i)
                {
                    inputWriter.WriteLine(inputs[i]);
                }
            }

            string text = "";

            do
            {
                text = outputReader.ReadLine();

                if (result.Length > 0 && (text != null && text.Length > 0))
                {
                    result += '\n';
                }

                result += text;

            } while (text != null && text.Length > 0);
            
            do
            {
                text = errorReader.ReadLine();

                if (error.Length > 0 && (text != null && text.Length > 0))
                {
                    error += '\n';
                }

                error += text;

            } while (text != null && text.Length > 0);
            
            if (!process.WaitForExit(5000))
            {
                process.Kill();
            }
            
            return new ProcessResult() { ExitCode = process.ExitCode, Output = result, Error = error };
        }
    }
}