using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Akkd;
using ConsoleTables;
using static Akkd.Common.ConsoleHelpers;
using Akkd.Extensions;

namespace Akkd.ProcessManager {
    internal class Program {
        private static readonly Logger logger = new Logger();

        private const string INDENT = "    ";

        [STAThread]
        private static void Main(string[] args) {
            logger.Clear();

            CenterAndResizeConsole();

            var sw = Stopwatch.StartNew();

            var processes = ProcessEx.GetProcesses();

            //PrintAllProcesses(processes);
            PrintAllProcesses02(processes);
            //PrintAllProcesses03(processes);

            ////PrintAllProcesses();

            sw.Stop();

            Console.WriteLine("Time taken: {0}ms", sw.Elapsed.TotalMilliseconds);

            Console.WriteLine("\nPress any key to continue . . .");
            Console.ReadKey();
        }

        private static void PrintAllProcesses() {
            var indent = "    ";
            var processes = Process.GetProcesses();
            var sb = new StringBuilder();

            for (int i = 0; i < processes.Length; i++) {
                var process = processes[i];

                sb.AppendLine($"{i + 1}.");
                sb.AppendLine($"{indent}   id: {process.Id}");
                sb.AppendLine($"{indent} name: {process.ProcessName}");

                //sb.AppendLine($"{indent} path: {GetProcessPath(process)}");


                //Console.WriteLine($"{indent}64bit: {IsWin64Emulator(process)}");
                //Console.WriteLine($"{indent}module: {process.MainModule}");
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }

        private static void PrintAllProcesses(List<ProcessEx> processes) {
            const string indent = "    ";
            var sb = new StringBuilder();

            processes = processes.OrderByDescending(p => p.MemoryUsed).ToList();

            var count = 0;

            for (int i = 0; i < processes.Count; i++) {
                var process = processes[i];

                sb.AppendLine($"{i + 1}.");
                sb.AppendLine($"{indent}      processId: {process.ProcessId}");
                sb.AppendLine($"{indent}parentProcessId: {process.ParentProcessId}");
                sb.AppendLine($"{indent}           name: {process.ProcessName}");
                sb.AppendLine($"{indent}       fileName: {process.FileName}");
                sb.AppendLine($"{indent}         handle: {process.Handle}");
                sb.AppendLine($"{indent}           user: {process.User}");
                sb.AppendLine($"{indent}         memory: {ProcessEx.ToString(process.MemoryUsed)}");
                //sb.AppendLine($"{indent}         memory: {process.MemoryUsed}");
                //sb.AppendLine($"{indent}        memory2: {ProcessEx.ToString(process.MemoryUsed2)}");
                //sb.AppendLine($"{indent}        memory2: {process.MemoryUsed2}");
                sb.AppendLine($"{indent}           arch: {process.Arch}");
                sb.AppendLine($"{indent}    commandLine: {process.CommandLine}");
                sb.AppendLine($"{indent}         handle: {process.Handle}");

                //sb.AppendLine($"{indent} name: {process.ProcessName}");

                //sb.AppendLine($"{indent} path: {GetExecutablePathAboveVista(process.Id)}");

                //sb.AppendLine($"{indent} path: {GetProcessPath(process)}");


                //Console.WriteLine($"{indent}64bit: {IsWin64Emulator(process)}");
                //Console.WriteLine($"{indent}module: {process.MainModule}");
                sb.AppendLine();

                if (process.FileName == "")
                    count++;
            }

            Console.WriteLine($"{count / (decimal)processes.Count}");

            //Clipboard.SetText(sb.ToString());

            var staThread = new Thread(x => {
                try {
                    Clipboard.SetText(sb.ToString());
                } catch (Exception) {
                    // ignored
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        private static void PrintAllProcesses02(List<ProcessEx> processes) {
            //processes = processes.OrderByDescending(p => p.MemoryUsed).ToList();
            processes = processes
                .FindAll(p => p.User != "Unknown")
                .OrderByDescending(p => p.ProcessGroupMemoryUsed)
                .ThenByDescending(p => p.MemoryUsed) // ThenByDescending
                .ToList();

            var processTableRows = processes.Select(process => ProcessExTableRow.From(process)).ToList();

            // Needed to preserve order?
            var mem = processTableRows[0].MemoryUsed;

            //var processTableRowsArray = processTableRows
            //    .OrderByDescending(p => p.GroupMemoryUsedNum)
            //    .ThenByDescending(p => p.MemoryUsedNum);

            var headerRow = new string[] {
                "ProcessName",
                "ProcessId",
                "ParentProcessId",
                "MemoryUsed",
                "GroupMemoryUsed",
                //"User",
                //"Arch",
                //"FileName",
                //"CommandLine"

                //"Handle",
            };

            var table = new ConsoleTable(headerRow);

            // Needed to preserve order?
            // var mem = processTableRows[0].MemoryUsed;

            foreach (var process in processTableRows) {
                table.AddRow(new string[] {
                    process.ProcessName,
                    process.ProcessId.ToString(),
                    process.ParentProcessId.ToString(),
                    process.MemoryUsed,
                    process.GroupMemoryUsed,
                    //process.User,
                    //process.Arch,
                    //process.FileName,
                    //process.CommandLine

                    //"Handle",
                });
            }

            SetClipboard(table.ToMinimalString());
            //SetClipboard(table2.ToMinimalString());
            //logger.Debug(table2.ToMinimalString());

            //var table2 = ConsoleTable
            //    .From<ProcessExTableRow>(processTableRows)
            //    .Configure(o => o.NumberAlignment = Alignment.Right);
        }

        private static void PrintAllProcesses03(List<ProcessEx> processes) {
            processes = processes
                .OrderBy(p => p.ProcessId)
                .ThenBy(p => p.ParentProcessId)
                .ToList();

            PrintProcessTree(processes);
        }

        private static void PrintProcessTree(List<ProcessEx> processes) {
            var lines = new List<string>();

            foreach (var process in processes) {
                // Start with root processes.
                if (process.ParentProcess != null)
                    continue;

                // Add this process to the TreeView.
                lines.Add(PrintProcessTreeHelper(process));
            }

            var output = string.Join("\n", lines);

            //logger.Debug(output);

            SetClipboard(output);
        }

        private static string PrintProcessTreeHelper(ProcessEx process, List<string> lines = null, int level = 0) {
            if (lines == null)
                lines = new List<string>();

            lines.Add($"{INDENT.Repeat(level)}{process.ProcessName} [{process.ProcessId}] {ProcessEx.ToString(process.MemoryUsed)} ({ProcessEx.ToString(process.ProcessGroupMemoryUsed)})");

            foreach (var childProcess in process.ChildProcesses) {
                PrintProcessTreeHelper(childProcess, lines, level + 1);
            }

            return string.Join("\n", lines);
        }

        private static void SetClipboard(string text) {
            var staThread = new Thread(x => {
                try {
                    Clipboard.SetText(text);
                } catch (Exception) { }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }
    }
}
