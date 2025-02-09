using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security.Principal;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;

namespace RAMLIMITER
{
    class Program
    {
        public static string variableger = "Discord";

        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        // Method to check the program's current privileges - created by Hypn0tick
        static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Method to elevate the program's privileges if the user chooses to do so - created by Hypn0tick
        static void ElevatePrivileges(string args)
        {
            string argsString = string.Concat(args);
            if (!IsAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Assembly.GetEntryAssembly().CodeBase;
                proc.Arguments = args;
                proc.Verb = "runas";

                Console.Clear();
                Console.WriteLine("RAM Limiter does not currently have admin. privileges.\nDepending on the programs you wish to limit, admin. privileges may be required.\n\nWould you like to run RAM Limiter as admin.? (y/n)");
                ConsoleKey adminResponse = Console.ReadKey(true).Key;
                if (adminResponse == ConsoleKey.Y)
                {
                    try
                    {
                        Process.Start(proc);
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not elevate program. \n\n" + ex.ToString());
                    }
                }
            }
        }

        // Method to get all processes matching the custom process name
        public static List<Process> GetCustom(string processName)
        {
            return Process.GetProcessesByName(processName).ToList();
        }

        // Method to limit RAM usage of multiple custom processes
        static void CustomRamLimiter(int min, int max, bool useConfig = false, string configFile = "RAMLimiter.ini")
        {
            List<string> processNames = null;
            if (useConfig)
            {
                configInput:
                if (configFile == null)
                {
                    Console.WriteLine("Input the name of a configuration file to be read:\nNOTE: This file should contain a list of processes to limit, with one name on each line.");
                    configFile = Console.ReadLine();
                }
                string configDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                Console.WriteLine(configDir + configFile);
                if (File.Exists(configFile))
                {
                    var lines = File.ReadLines(configFile);
                    string processNamesInput = null;
                    foreach (var line in lines)
                    {
                        string output = Regex.Replace(line, "//.*", "");
                        processNamesInput = output + "," + processNamesInput;
                    }
                    processNames = processNamesInput.Split(',').Select(p => p.Trim().ToLower()).ToList();
                }
                else
                {
                    Console.WriteLine("ERROR: Could not find the " + configFile + " configuration file.\n");
                    configFile = null;
                    goto configInput;
                }
            }
            else
            {
                Console.WriteLine("Type Process Names Separated by Commas (e.g., chrome,obs,discord):");
                string processNamesInput = Console.ReadLine();
                processNames = processNamesInput.Split(',').Select(p => p.Trim().ToLower()).ToList();
            }
            List<Process> processesToLimit = new List<Process>();
            foreach (var processName in processNames)
            {
                processesToLimit.AddRange(GetCustom(processName));
            }

            foreach (var process in processesToLimit)
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    while (!process.HasExited)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            SetProcessWorkingSetSize(process.Handle, min, max);
                        }

                        var wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

                        var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new
                        {
                            FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                            TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
                        }).FirstOrDefault();

                        if (memoryValues != null)
                        {
                            var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(process.ProcessName + ": Total RAM usage: {0}", percent);
                            Thread.Sleep(3000);
                        }
                    }
                }).Start();
            }
            Thread.Sleep(-1);
        }

        // Method to get the process ID of Discord
        public static int GetDiscord()
        {
            int DiscordId = -1;
            long workingSet = 0;
            foreach (Process discord in Process.GetProcessesByName("Discord"))
            {
                if (discord.WorkingSet64 > workingSet)
                {
                    workingSet = discord.WorkingSet64;
                    DiscordId = discord.Id;
                }
            }
            return DiscordId;
        }

        // Method to get the process ID of Chrome
        public static int GetChrome()
        {
            int chromeId = -1;
            long workingSet1 = 0;
            foreach (Process chrome in Process.GetProcessesByName("Chrome"))
            {
                if (chrome.WorkingSet64 > workingSet1)
                {
                    workingSet1 = chrome.WorkingSet64;
                    chromeId = chrome.Id;
                }
            }
            return chromeId;
        }

        // Method to get the process ID of OBS
        public static int GetOBS()
        {
            int OBSId = -1;
            long workingSet2 = 0;
            foreach (Process OBS in Process.GetProcessesByName("obs64"))
            {
                if (OBS.WorkingSet64 > workingSet2)
                {
                    workingSet2 = OBS.WorkingSet64;
                    OBSId = OBS.Id;
                }
            }
            return OBSId;
        }

        // Method to limit both Chrome and Discord
        static void Both(int min, int max)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    while (GetChrome() != -1)
                    {
                        if (GetChrome() != -1)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();

                            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            {
                                SetProcessWorkingSetSize(Process.GetProcessById(GetChrome()).Handle, min, max);
                            }

                            var wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

                            var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new {
                                FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                                TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
                            }).FirstOrDefault();

                            if (memoryValues != null)
                            {
                                var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("CHROME: Total RAM usage: {0}", percent);
                                Thread.Sleep(3000);
                            }

                            Thread.Sleep(1);
                        }
                    }
                }
            }).Start();
            Thread.Sleep(5000);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    while (GetDiscord() != -1)
                    {
                        if (GetDiscord() != -1)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();

                            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            {
                                SetProcessWorkingSetSize(Process.GetProcessById(GetDiscord()).Handle, min, max);
                            }

                            var wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

                            var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new
                            {
                                FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                                TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
                            }).FirstOrDefault();

                            if (memoryValues != null)
                            {
                                var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("DISCORD: Total RAM usage: {0}", percent);
                                Thread.Sleep(3000);
                            }

                            Thread.Sleep(1);
                        }
                    }
                }

            }).Start();
            Thread.Sleep(-1);
        }

        // Method to limit Discord RAM usage
        static void DiscordRamLimiter(int min, int max)
        {
            while (GetDiscord() != -1)
            {
                if (GetDiscord() != -1)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        SetProcessWorkingSetSize(Process.GetProcessById(GetDiscord()).Handle, min, max);
                    }

                    var wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

                    var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new {
                        FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                        TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
                    }).FirstOrDefault();

                    if (memoryValues != null)
                    {
                        var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("DISCORD: Total RAM usage: {0}", percent);
                        Thread.Sleep(10000);
                    }

                    Thread.Sleep(1);
                }
            }
        }

        // Method to limit Chrome RAM usage
        static void ChromeRamLimiter(int min, int max)
        {
            while (GetChrome() != -1)
            {
                if (GetChrome() != -1)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        SetProcessWorkingSetSize(Process.GetProcessById(GetChrome()).Handle, min, max);
                    }

                    var wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

                    var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new {
                        FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                        TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
                    }).FirstOrDefault();

                    if (memoryValues != null)
                    {
                        var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("CHROME: Total RAM usage: {0}", percent);
                        Thread.Sleep(5000);
                    }

                    Thread.Sleep(1);
                }
            }
        }

        // Method to limit OBS RAM usage
        static void OBSRamLimiter(int min, int max)
        {
            while (GetOBS() != -1)
            {
                if (GetOBS() != -1)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        SetProcessWorkingSetSize(Process.GetProcessById(GetOBS()).Handle, min, max);
                    }

                    var wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

                    var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new {
                        FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                        TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
                    }).FirstOrDefault();

                    if (memoryValues != null)
                    {
                        var percent = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("OBS: Total RAM usage: {0}", percent);
                        Thread.Sleep(5000);
                    }

                    Thread.Sleep(1);
                }
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "RAM Limiter - Modified by Hypn0tick";

        start:
            string argsString = string.Concat(args);
            ElevatePrivileges(argsString);
            Console.Clear();
            foreach (string arg in args)
            {
                if (arg == "-f" | arg == "-file")
                {
                    CustomRamLimiter(-1, -1, true);
                }
                if (arg == "-c" | arg == "-custom")
                {
                    CustomRamLimiter(-1, -1, false);
                }
                if (arg == "-discord")
                {
                    if (args.Contains("-chrome"))
                    {
                        Both(-1, -1);
                    }
                    else
                    {
                        DiscordRamLimiter(-1, -1);
                    }
                }
                if (arg == "-chrome")
                {
                    if (args.Contains("-discord"))
                    {
                        Both(-1, -1);
                    }
                    else
                    {
                        ChromeRamLimiter(-1, -1);
                    }
                }
                if (arg == "-obs")
                {
                    OBSRamLimiter(-1, -1);
                }
            }
            Console.Clear();
            Console.WriteLine("Just Limit Discord: 1");
            Console.WriteLine("Just Limit Chrome: 2");
            Console.WriteLine("Just Limit OBS: 3");
            Console.WriteLine("Limit Discord & Chrome: 4");
            Console.WriteLine("Limit Custom: 5");
            Console.WriteLine("Limit Custom (External List): 6");
            ConsoleKey response = Console.ReadKey(true).Key;
            Console.WriteLine();
            if (response == ConsoleKey.D1)
            {
                Console.Clear();
                DiscordRamLimiter(-1, -1);
            }
            else if (response == ConsoleKey.D2)
            {
                Console.Clear();
                ChromeRamLimiter(-1, -1);
            }
            else if (response == ConsoleKey.D3)
            {
                Console.Clear();
                OBSRamLimiter(-1, -1);
            }
            else if (response == ConsoleKey.D4)
            {
                Console.Clear();
                Both(-1, -1);
            }
            else if (response == ConsoleKey.D5)
            {
                Console.Clear();
                CustomRamLimiter(-1, -1);
            }
            else if (response == ConsoleKey.D6)
            {
                Console.Clear();
                CustomRamLimiter(-1, -1,true);
            }
            else
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Don't Use The Numpad, Use The Numbers At The Top Of Your Keyboard.");
                Thread.Sleep(2500);
                goto start;
            }
        }
    }
}
