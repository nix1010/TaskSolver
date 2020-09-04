using ProgrammingTasks.Models;
using ProgrammingTasks.Models.API;
using ProgrammingTasks.Models.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace ProgrammingTasks
{
    public class CodeExecutor
    {
        private readonly string codeLocation;
        private readonly string binLocation;

        public CodeExecutor()
        {
            Random r = new Random();

            this.codeLocation = HttpRuntime.AppDomainAppPath + "code\\" +
                (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + "-" + r.Next(int.MaxValue);
            this.binLocation = codeLocation + "\\bin";

            if (!Directory.Exists(binLocation))
            {
                Directory.CreateDirectory(binLocation);
            }
        }

        public RunResultDTO RunTask(TaskSolutionDTO taskSolution, task task)
        {
            if (taskSolution == null)
            {
                ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "No body provided");
            }

            if (taskSolution.Code == null || taskSolution.Code.Trim() == "")
            {
                ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "No code supplied");
            }

            ILanguage languageType = null;
            string filePath = codeLocation;

            if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.JAVA)
            {
                filePath += "\\Main.java";

                languageType = new CompiledLanguage(
                    filePath,
                    "javac -d \"" + binLocation + "\" \"" + filePath + "\"",
                    "java -cp \"" + binLocation + "\"; Main");
            }
            else if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.C_PLUS_PLUS)
            {
                filePath += "\\main.cpp";
                /*
                languageType = new CompiledLanguage(
                    filePath,
                    "gcc -cpp \"" + filePath + "\" -o \"" + binLocation + "\\mainCpp\"",
                    "\"" + binLocation + "\\mainCpp\"");
                 * */
                
                languageType = new CompiledLanguage(
                    filePath,
                    "g++ -std=c++1z -c \"" + filePath + "\" -o \"" + binLocation + "\\main.o\" " + //create o file in bin folder
                    "&& g++ -std=c++1z \"" + binLocation + "\\main.o\" -o \"" + binLocation + "\\main\"", //link o file to exe
                    "\"" + binLocation + "\\main\"");
            }
            else if (taskSolution.ProgrammingLanguage == ProgrammingLanguage.C_SHARP)
            {
                filePath += "\\Main.cs";

                languageType = new CompiledLanguage(
                    filePath,
                    "csc -out:\"" + binLocation + "\\main.exe\" \"" + filePath + "\"",
                    "\"" + binLocation + "\\main\"");
            }
            else
            {
                ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "Specified language not supported");
            }

            return languageType.RunSolution(taskSolution.Code, task);
        }

        private abstract class ILanguage
        {
            protected ProcessResult RunProcess(string argument, string[] inputs = null)
            {
                string result = "";
                string error = "";

                StreamWriter inputWriter = null;
                StreamReader outputReader = null;
                StreamReader errorReader = null;
                ProcessStartInfo startInfo = new ProcessStartInfo();

                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = argument;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                Process process = Process.Start(startInfo);

                inputWriter = process.StandardInput;
                outputReader = process.StandardOutput;
                errorReader = process.StandardError;

                Thread t = new Thread(() =>
                {
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
                });
                t.IsBackground = true;
                t.Start();
                
                if (!t.Join(5000))
                {
                    t.Interrupt();
                    KillProcessAndChildren(process.Id);
                    error = ": Execution too long";
                }

                return new ProcessResult() { ExitCode = process.ExitCode, Output = result, Error = error };
            }

            private void KillProcessAndChildren(int pid)
            {
                using (var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid))
                {
                    var moc = searcher.Get();

                    foreach (ManagementObject mo in moc)
                    {
                        KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
                    }

                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        proc.Kill();
                    }
                    catch (Exception)
                    {
                        // Process already exited.
                    }
                }
            }

            public abstract RunResultDTO RunSolution(string solutionCode, task task);
        }

        private class ScriptLanguage : ILanguage
        {
            protected string filePath;
            protected string runCommand;

            public ScriptLanguage(string filePath, string runCommand)
            {
                this.filePath = filePath;
                this.runCommand = runCommand;
            }

            protected RunResultDTO RunTaskExamples(task task)
            {
                ProcessResult processResult = null;
                RunResultDTO runResult = new RunResultDTO()
                {
                    TaskTitle = task.title
                };

                foreach (example example in task.examples)
                {
                    string description;

                    processResult = RunProcess("/C " + runCommand, example.input.Split(';'));

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
                        SolutionResult = processResult.Output,
                        Description = description +
                            //remove path to the file from error
                        processResult.Error.Replace(filePath.Replace(Path.GetFileName(filePath), ""), "")
                    });
                }

                return runResult;
            }

            protected void WriteCodeToFile(string solutionCode)
            {
                StreamWriter writer = new StreamWriter(filePath);
                writer.Write(solutionCode);
                writer.Close();
            }

            public override RunResultDTO RunSolution(string solutionCode, task task)
            {
                WriteCodeToFile(solutionCode);

                return RunTaskExamples(task);
            }
        }

        private class CompiledLanguage : ScriptLanguage
        {
            private string compileCommand;

            public CompiledLanguage(string filePath, string compileCommand, string runCommand)
                : base(filePath, runCommand)
            {
                this.compileCommand = compileCommand;
            }

            public override RunResultDTO RunSolution(string solutionCode, task task)
            {
                WriteCodeToFile(solutionCode);

                ProcessResult processResult = RunProcess("/C " + compileCommand);

                if (processResult.ExitCode != 0)
                {
                    ExceptionHandler.ThrowException(HttpStatusCode.BadRequest, "Compile error " +
                        //remove path to the file from error
                        processResult.Error.Replace(filePath.Replace(Path.GetFileName(filePath), ""), ""));
                }

                return base.RunTaskExamples(task);
            }
        }
    }
}