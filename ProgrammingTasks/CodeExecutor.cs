using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Diagnostics;
using ProgrammingTasks.Models;
using ProgrammingTasks.Models.API;
using ProgrammingTasks.Models.Entity;

namespace ProgrammingTasks
{
    public class CodeExecutor
    {
        private readonly string codeLocation;
        private readonly string binLocation;

        public CodeExecutor()
        {
            this.codeLocation = HttpRuntime.AppDomainAppPath + "code";
            this.binLocation = codeLocation + "\\bin";

            if (!Directory.Exists(binLocation))
            {
                Directory.CreateDirectory(binLocation);
            }
        }

        public RunResultDTO RunExamples(TaskSolutionDTO taskSolution, ICollection<example> examples)
        {
            RunResultDTO runResult = new RunResultDTO();

            ProcessResult processResult = Compile(taskSolution);

            if (processResult.ExitCode != 0)
            {
                ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "Compile error"/* + processResult.Error*/);
            }

            foreach (example example in examples)
            {
                string description;
                
                processResult = Run(taskSolution, example);

                if (processResult.ExitCode == 0)
                {
                    if (processResult.Output == example.output)
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

                runResult.exampleResults.Add(new ExampleResultDTO()
                {
                    Input = example.input,
                    Output = example.output,
                    SolutionResult = processResult.Output + processResult.Error,
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

            if (taskSolution.Code == null || taskSolution.Code.Trim() == "")
            {
                ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "No code supplied");
            }

            if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.JAVA)
            {
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