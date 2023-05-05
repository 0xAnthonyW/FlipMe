using Microsoft.Win32;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Management;
using System.Runtime.InteropServices;


namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            DisableUAC();

            string usbPath = @"D:\PassExpire";
            string windowsBlocker = @"D:\Win11Blocker\WindowsBlocker.ps1";
            string software = @"D:\Software";
            string destination = @"C:\Users\admin\Desktop";
            string taskPass = Path.Combine(destination, "TaskPasswordExpire.ps1");
            string flipme = Path.Combine(destination, @"Software\FlipMe.ps1");
            string passExpirePath = Path.Combine(destination, "PassExpire");
            string passFilePath = Path.Combine(destination, "PasswordExpire.ps1");
            string taskfilePath = Path.Combine(destination, "TaskPasswordExpire.ps1");
            string softwareDestination = Path.Combine(destination, "Software");
            string admin = "admin";

            ProcessFolders(usbPath, destination, passExpirePath, passFilePath, taskfilePath);

            ExecutePowerShellScript(taskPass);

            ClearRecycleBin();

            ExecutePowerShellScript(flipme);

            CheckWindowsVersionAndRunBlocker(windowsBlocker);

            SetAdminPasswordNeverExpires(admin);

            SetupSoftwareDestination(software, softwareDestination);

            SetTimeZone();

            EnableUAC();

            SetBrightness();

            Console.WriteLine("All done. Press enter to exit.");
            Console.ReadLine();
        }

        private static void DisableUAC()
        {
            Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin", 0, RegistryValueKind.DWord);
            Console.WriteLine("UAC OFF");
            Console.ReadLine();
        }

        private static void EnableUAC()
        {
            Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin", 5, RegistryValueKind.DWord);
            Console.WriteLine("UAC Enabled");
            Console.ReadLine();
        }
        private static void ProcessFolders(string usbPath, string destination, string passExpirePath, string passFilePath, string taskfilePath)
        {
            if (Directory.Exists(passExpirePath))
            {
                Console.WriteLine($"Removing existing {passExpirePath}...");
                Directory.Delete(passExpirePath, true);
            }

            Directory.CreateDirectory(passExpirePath);
            CopyFolder(usbPath, passExpirePath);

            if (Directory.Exists(passExpirePath))
            {
                Console.WriteLine($"Removing existing {passExpirePath}...");
                Directory.Delete(passExpirePath, true);
            }

            Directory.CreateDirectory(passExpirePath);

            if (!File.Exists(passFilePath))
            {
                Console.WriteLine($"Copying PasswordExpire.ps1 to {destination}...");
                File.Copy(Path.Combine(passExpirePath, "PasswordExpire.ps1"), passFilePath, true);
            }
            else
            {
                Console.WriteLine($"Removing existing {passFilePath}...");
                File.Delete(passFilePath);
                File.Copy(Path.Combine(passExpirePath, "PasswordExpire.ps1"), passFilePath, true);
            }

            if (!File.Exists(taskfilePath))
            {
                Console.WriteLine($"Copying TaskPasswordExpire.ps1 to {destination}...");
                File.Copy(Path.Combine(passExpirePath, "TaskPasswordExpire.ps1"), taskfilePath, true);
            }
            else
            {
                Console.WriteLine($"Removing existing {taskfilePath}...");
                File.Delete(taskfilePath);
                File.Copy(Path.Combine(passExpirePath, "TaskPasswordExpire.ps1"), taskfilePath, true);
            }

            if (Directory.Exists(passExpirePath))
            {
                Directory.Delete(passExpirePath, true);
                Console.WriteLine("PassExpire Folder has been removed");
            }
            else
            {
                Console.WriteLine("PassExpire Folder does not exist.");
            }
        }

        private static void CopyFolder(string sourcePath, string destPath)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            string[] files = Directory.GetFiles(sourcePath);
            foreach (string file in files)
            {
                string destFile = Path.Combine(destPath, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            string[] folders = Directory.GetDirectories(sourcePath);
            foreach (string folder in folders)
            {
                string destFolder = Path.Combine(destPath, Path.GetFileName(folder));
                CopyFolder(folder, destFolder);
            }
        }

        private static void ExecutePowerShellScript(string scriptPath)
        {
            // Create a process to run the PowerShell executable
            Process process = new Process();
            process.StartInfo.FileName = "powershell.exe";

            // Pass the script path and arguments to the PowerShell process
            process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File {scriptPath}";

            // Start the PowerShell process and wait for it to complete
            process.Start();
            process.WaitForExit();

            // Check if the PowerShell script executed successfully
            if (process.ExitCode == 0)
            {
                Console.WriteLine($"PowerShell script '{scriptPath}' executed successfully.");
            }
            else
            {
                Console.WriteLine($"Error: PowerShell script '{scriptPath}' failed to execute.");
            }
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        private static void ClearRecycleBin()
        {
            const uint SHERB_NOCONFIRMATION = 0x00000001;
            const uint SHERB_NOPROGRESSUI = 0x00000002;
            const uint SHERB_NOSOUND = 0x00000004;

            uint flags = SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND;

            uint result = SHEmptyRecycleBin(IntPtr.Zero, null, flags);
            if (result == 0)
            {
                Console.WriteLine("Recycle bin cleared.");
            }
            else
            {
                Console.WriteLine($"Failed to clear recycle bin. Error code: {result}");
            }
        }

        private static void CheckWindowsVersionAndRunBlocker(string windowsBlocker)
        {
            Dictionary<string, string> registryPaths = new Dictionary<string, string>()
    {
        { "Path1", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion" },
        { "Path2", @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion" }
    };

            Dictionary<string, string> GetDisplayVersions(Dictionary<string, string> paths)
            {
                var displayVersions = new Dictionary<string, string>();

                foreach (var entry in paths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(entry.Value))
                    {
                        if (key != null)
                        {
                            var value = key.GetValue("DisplayVersion");
                            displayVersions[entry.Key] = value?.ToString();
                        }
                        else
                        {
                            displayVersions[entry.Key] = null;
                        }
                    }
                }

                return displayVersions;
            }

            var displayVersions = GetDisplayVersions(registryPaths);

            if (displayVersions["Path1"] != null && displayVersions["Path2"] != null)
            {
                if (displayVersions["Path1"].Equals(displayVersions["Path2"], StringComparison.OrdinalIgnoreCase)
                    && displayVersions["Path1"].Equals("22H2", StringComparison.OrdinalIgnoreCase))
                {
                    ExecutePowerShellScript(windowsBlocker);
                    Console.WriteLine("WindowsBlocker is done.");
                }
                else if (displayVersions["Path1"].Equals(displayVersions["Path2"], StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Display Versions match but are not 22H2.");
                }
                else
                {
                    Console.WriteLine("Display Versions Mismatch Detected. No further action needed.");
                }
            }
        }

        private static void SetAdminPasswordNeverExpires(string admin)
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
            {
                UserPrincipal user = UserPrincipal.FindByIdentity(context, admin);

                if (user != null)
                {
                    user.PasswordNeverExpires = true;
                    user.Save();
                }
                else
                {
                    Console.WriteLine("User not found");
                }
            }
        }

        private static void SetupSoftwareDestination(string software, string softwareDestination)
        {
            if (Directory.Exists(softwareDestination))
            {
                Directory.Delete(softwareDestination, true);
            }
            Directory.CreateDirectory(softwareDestination);

            foreach (string dirPath in Directory.GetDirectories(software, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(software, softwareDestination));
            }

            foreach (string newPath in Directory.GetFiles(software, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(software, softwareDestination), true);
            }

            Console.WriteLine($"Software has been copied to {softwareDestination}");
        }

        private static void SetTimeZone()
        {
            string timezone = "Central Standard Time";
            Process.Start("tzutil.exe", $"/s \"{timezone}\"").WaitForExit();
            Console.WriteLine("Timezone has been set to CST");
        }

        private static void SetBrightness()
        {
            ConnectionOptions connectionOptions = new ConnectionOptions();
            connectionOptions.EnablePrivileges = true;

            // Set up WMI scope and query
            ManagementScope scope = new ManagementScope("\\\\.\\root\\WMI", connectionOptions);
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM WmiMonitorBrightnessMethods");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

            // Set brightness to 100%
            ManagementObjectCollection objects = searcher.Get();
            foreach (ManagementObject obj in objects)
            {
                obj.InvokeMethod("WmiSetBrightness", new Object[] { UInt32.MaxValue, 100 });
            }

            Console.WriteLine("Brightness has been set to 100%");
        }
    }
}