using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PInvoke;
using Akkd.ProcessManager;

namespace Akkd.ProcessManager {
    public class ProcessEx {
        #region Private Fields

        Kernel32.SafeObjectHandle _handle;

        #endregion Private Fields

        #region Properties

        public int ProcessId { get; }

        public int ParentProcessId { get; }

        public List<ProcessEx> ChildProcesses { get; }

        public IntPtr Handle => _handle.DangerousGetHandle();

        public string ProcessName { get; }

        public string FileName => GetProcessFileName(Handle);

        public string User => GetProcessUser(Handle);

        public long MemoryUsed => GetProcessMemory(Handle);

        public string Arch => (Is64Bit(Handle)) ? "x64" : "x86";

        public string CommandLine => GetCommandLine(ProcessId);

        #endregion Properties

        #region Constructors

        public ProcessEx(int processId, string processName, int parentProcessId) {
            ProcessId = processId;

            ParentProcessId = parentProcessId;

            ProcessName = processName;

            _handle = GetProcessHandle(processId);

            ChildProcesses = new List<ProcessEx>();
        }

        #endregion Constructors

        public static unsafe List<ProcessEx> GetProcesses() {
            var processList = new List<ProcessEx>();
            var hProcessSnap = Kernel32.CreateToolhelp32Snapshot(Kernel32.CreateToolhelp32SnapshotFlags.TH32CS_SNAPPROCESS, 0);
            var pe32 = new Kernel32.PROCESSENTRY32 { dwSize = sizeof(Kernel32.PROCESSENTRY32) };

            if (hProcessSnap.IsInvalid)
                return processList;

            if (!Kernel32.Process32First(hProcessSnap, ref pe32))
                return processList;

            do {
                var process = new ProcessEx(pe32.th32ProcessID, pe32.ExeFile, pe32.th32ParentProcessID);

                var parentProcess = processList.FirstOrDefault(proc => proc.ProcessId == process.ParentProcessId);

                if (parentProcess != null) {
                    parentProcess.ChildProcesses.Add(process);
                }

                processList.Add(process);
            } while (Kernel32.Process32Next(hProcessSnap, ref pe32));

            return processList;
        }

        public static string ToString(long numOfBytes) {
            double bytes = numOfBytes;

            if (bytes < 1024)
                return bytes.ToString();

            bytes /= 1024;

            if (bytes < 1024) {
                return bytes.ToString("#.# KB");
            }

            bytes /= 1024;

            if (bytes < 1024) {
                return bytes.ToString("#.# MB");
            }

            bytes /= 1024;

            return bytes.ToString("#.# GB");
        }

        public static string GetCommandLine(int processId) {
            // max size of a command line is USHORT/sizeof(WCHAR), so we are going
            // just max USHORT for sanity's sake.
            var sb = new StringBuilder(0xFFFF);

            switch (IntPtr.Size) {
                case 4:
                    Win32.GetProcCmdLine32((uint)processId, sb, (uint)sb.Capacity);
                    break;

                case 8:
                    Win32.GetProcCmdLine64((uint)processId, sb, (uint)sb.Capacity);
                    break;
            }

            return sb.ToString();
        }

        public static string GetCommandLineIgnoreFirst(int processId) {
            var t = GetCommandLineArray(processId).ToList();

            t.RemoveAt(0);

            return RebuildArgumentsFromArray(t.ToArray());
        }

        public static string[] GetCommandLineArray(int processId) {
            return CommandLineToArgs(GetCommandLine(processId));
        }

        #region Private Methods

        private Kernel32.SafeObjectHandle GetProcessHandle(int processId) {
            //return Kernel32.OpenProcess((int)Kernel32.ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION | (int)Kernel32.ProcessAccess.PROCESS_VM_READ, false, processId);
            return Kernel32.OpenProcess((int)Kernel32.ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        }

        private string GetProcessFileName(IntPtr hProcess) {
            var buffer = new StringBuilder(1024);

            if (hProcess == IntPtr.Zero)
                return string.Empty;

            var size = (uint)buffer.Capacity;

            return Win32.QueryFullProcessImageName(hProcess, 0, buffer, ref size) ? buffer.ToString() : string.Empty;
        }

        private string GetProcessUser(IntPtr hProcess) {
            string user = "Unknown";
            WindowsIdentity wi = null;

            try {
                AdvApi32.OpenProcessToken(hProcess, 8, out var processHandle);

                wi = new WindowsIdentity(processHandle.DangerousGetHandle());

                user = wi.Name;

                return user.Contains("\\") ? user.Substring(user.IndexOf("\\") + 1) : user;
            } catch (Exception) {
                return user;
            } finally {
                wi?.Dispose();
            }
        }

        private unsafe long GetProcessMemory(IntPtr hProcess) {
            var memory = (long)0;
            var pmc = new Win32.PROCESS_MEMORY_COUNTERS { cb = (uint)sizeof(Win32.PROCESS_MEMORY_COUNTERS) };

            if (Win32.GetProcessMemoryInfo(hProcess, ref pmc, pmc.cb)) {
                memory = (long)pmc.WorkingSetSize;

                //Console.WriteLine($"            WorkingSetSize: {ToString((long)pmc.WorkingSetSize)}");
                //Console.WriteLine($"   QuotaPeakPagedPoolUsage: {ToString((long)pmc.QuotaPeakPagedPoolUsage)}");
                //Console.WriteLine($"       QuotaPagedPoolUsage: {ToString((long)pmc.QuotaPagedPoolUsage)}");
                //Console.WriteLine($"QuotaPeakNonPagedPoolUsage: {ToString((long)pmc.QuotaPeakNonPagedPoolUsage)}");
                //Console.WriteLine($"    QuotaNonPagedPoolUsage: {ToString((long)pmc.QuotaNonPagedPoolUsage)}");
                //Console.WriteLine($"             PagefileUsage: {ToString((long)pmc.PagefileUsage)}");
                //Console.WriteLine($"         PeakPagefileUsage: {ToString((long)pmc.PeakPagefileUsage)}");
                //Console.WriteLine($"                        cb: {ToString((long)pmc.cb)}");
            }

            return memory;
        }

        private bool Is64Bit(IntPtr hProcess) {
            if (!Environment.Is64BitOperatingSystem)
                return false;

            if (!Win32.IsWow64Process(hProcess, out var isWow64))
                throw new Win32Exception();

            return !isWow64;
        }

        private static string[] CommandLineToArgs(string commandLine) {
            var argv = Win32.CommandLineToArgvW(commandLine, out var argc);

            if (argv == IntPtr.Zero) { throw new System.ComponentModel.Win32Exception(); }

            try {
                var args = new string[argc];

                for (var i = 0; i < args.Length; ++i) {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);

                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            } finally {
                Marshal.FreeHGlobal(argv);
            }
        }

        private static string RebuildArgumentsFromArray(string[] arrArgs) {
            string Encode(string s) => string.IsNullOrEmpty(s) ? "\"\"" : Regex.Replace(Regex.Replace(s, @"(\\*)" + "\"", @"$1\$0"), @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");

            if ((arrArgs != null) && (arrArgs.Length > 0)) {
                return string.Join(" ", Array.ConvertAll(arrArgs, Encode));
            }

            return string.Empty;
        }

        #endregion

        public override string ToString() {
            return $"{ProcessName} [{ProcessId}]";
        }
    }
}
