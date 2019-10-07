using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Akkd.ProcessManager {
    internal class Program {
        //[STAThread]
        private static void Main(string[] args) {
            var sw = Stopwatch.StartNew();

            var processes = ProcessEx.GetProcesses();

            PrintAllProcesses(processes);

            //PrintAllProcesses();

            sw.Stop();

            //var elapsedMs = sw.ElapsedMilliseconds;

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
                sb.AppendLine($"{indent}           arch: {process.Arch}");
                sb.AppendLine($"{indent}    commandLine: {process.CommandLine}");
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
    }
}
