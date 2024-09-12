using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace DowngradVPN
{
    public partial class SplashScreen : Window
    {
        private const string OpenVPNPath = @"./openvpn/bin/openvpn.exe";
        private const string InstallerUrl = "http://147.45.77.19/openvpn-installer.msi";
        private const string InstallerPath = @"C:\temp\openvpn-installer.msi";


        private const string LocalManifestPath = @"./updater/fileHashes.json";
        private const string ServerManifestUrl = "http://147.45.77.19/fileHashes.json";
        private const string UpdaterExecutable = @"./updater/update.exe";
        public SplashScreen()
        {
            InitializeComponent();
            CheckForUpdates();
            CheckAndInstallOpenVPN();
        }

        private async void CheckAndInstallOpenVPN()
        {
           
            if (!IsOpenVPNInstalled())
            {
                StatusTextBlock.Text = "OpenVPN is not installed. \nDownloading and installing now...";
                await DownloadInstaller();
                InstallOpenVPN();
            }

            else
            {
                OpenMainWindow();
            }
        }

        private bool IsOpenVPNInstalled()
        {
            return File.Exists(OpenVPNPath);
        }

        private async Task DownloadInstaller()
        {
            using (WebClient webClient = new WebClient())
            {
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(InstallerPath)))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(InstallerPath));
                }
                await webClient.DownloadFileTaskAsync(new Uri(InstallerUrl), InstallerPath);
            }
        }

        private void InstallOpenVPN()
        {
            string installPath = Path.Combine(Directory.GetCurrentDirectory(), "openvpn");

            Process installerProcess = new Process();
            installerProcess.StartInfo.FileName = "msiexec";
            installerProcess.StartInfo.Arguments = $"/i \"{InstallerPath}\" PRODUCTDIR=\"{installPath}\" ADDLOCAL=OpenVPN,OpenVPN.Service,Drivers,Drivers.TAPWindows6 /quiet /norestart";
            installerProcess.StartInfo.UseShellExecute = false;
            installerProcess.StartInfo.RedirectStandardOutput = true;
            installerProcess.Start();

            installerProcess.WaitForExit();

            if (installerProcess.ExitCode == 0)
            {
                MessageBox.Show("OpenVPN installation completed successfully.");
                OpenMainWindow();
            }
            else
            {
                MessageBox.Show("OpenVPN installation failed (try to run as administrator).");
                Application.Current.Shutdown();
            }
        }








        private void OpenMainWindow()
        {
            MainWindow mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow; // Set the main window to the new MainWindow instance
            mainWindow.Show();
            this.Close();
        }

        private async Task CheckForUpdates()
        {
            var localManifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater", "fileHashes.json");

            if (File.Exists(localManifestPath))
            {
                var localManifest = await ReadLocalManifestAsync(localManifestPath);
                var serverManifest = await DownloadServerManifest();

                if (serverManifest != null && !CompareManifests(localManifest, serverManifest))
                {
                    // If the manifests differ, run the updater
                    MessageBox.Show("Manifests differ. Starting the updater...");
                    RunUpdater();
                    Application.Current.Shutdown();
                }
                else
                {
                    // If manifests are identical or server manifest is null, proceed
                    MessageBox.Show("No updates found. Starting main app...");
                    
                }
            }
            else
            {
                MessageBox.Show("Local manifest not found. Starting the updater...");
                // If no local manifest, assume an update is required
                RunUpdater();
                Application.Current.Shutdown();
            }
        }


        private async Task<string?> ReadLocalManifestAsync(string path)
        {
            try
            {
                return await File.ReadAllTextAsync(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading local manifest: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> DownloadServerManifest()
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    return await webClient.DownloadStringTaskAsync(new Uri(ServerManifestUrl));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading server manifest: {ex.Message}");
                return null;
            }
        }

        private bool CompareManifests(string? localManifest, string? serverManifest)
        {
            if (localManifest == null || serverManifest == null)
            {
                MessageBox.Show("One of the manifests is null.");
                return false;
            }

            return localManifest == serverManifest;
        }

        // Method to run the updater
        private void RunUpdater()
        {
            try
            {
                // Get the absolute path to the updater executable
                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater", "update.exe");

                if (File.Exists(updaterPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(updaterPath) // Set the working directory to the updater folder
                    });
                }
                else
                {
                    MessageBox.Show($"Updater not found at path: {updaterPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running updater: {ex.Message}");
            }
        }
    }
}
