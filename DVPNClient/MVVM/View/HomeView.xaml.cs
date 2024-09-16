using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using static DowngradVPN.MVVM.ViewModel.MainViewModel;
using DowngradVPN.MVVM.ViewModel;
using System.Net;
using System.Net.Http;
using System.Text.Json;
namespace DowngradVPN.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public class CountryItem
        {
            public int id { get; set; }
            public string Name { get; set; }
            public string country { get; set; }
            public int usage { get; set; }

            public string FlagImage { get; set; }
        }
        public class ClientConfigResponse
        {
            public int ClientId { get; set; }
            public string ClientConfig { get; set; }
        }

        private int reservedServerID = -1;
        private int reservedClientID = -1;

        public ObservableCollection<CountryItem> Countries { get; set; }

        private void InitializeCountries()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Countries = new ObservableCollection<CountryItem>();
                var servers = GlobalData.SerializedJson?.Servers;
                if (servers != null)
                {
                    foreach (var server in servers)
                    {
                        Countries.Add(new CountryItem
                        {
                            id = server.ServerID,
                            Name = server.ServerName,
                            country = server.ServerCountry,
                            usage = (int)((double)server.UsedClients / server.TotalClients * 100),
                            FlagImage = "../../Images/se.png"
                        });
                    }
                }
            });
        }



        private void ShowSpinner(bool isVisible)
        {
            LoadingSpinner.Dispatcher.Invoke(() => LoadingSpinner.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed);
            PowerIcon.Dispatcher.Invoke(() => PowerIcon.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible);
        }

        public HomeView(MainViewModel viewModel)
        {
            InitializeComponent();
            ShowSpinner(true); 
            Loaded += async (s, e) =>
            {
                await InitializeData(viewModel);
                ShowSpinner(false);
            };
        }

        private async Task InitializeData(MainViewModel viewModel)
        {
            await viewModel.FetchServersAsync();
            InitializeCountries(); 
            CountryComboBox.ItemsSource = Countries;
            CountryComboBox.SelectedIndex = Countries.Any() ? 0 : -1; 
        }


        private async void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CountryComboBox.SelectedItem is CountryItem selectedCountry)
            {
                AppendLog($"Selected Country: {selectedCountry.Name}");

                
                //await DownloadClientConfig(selectedCountry.id);
                reservedServerID = selectedCountry.id;

            }
        }


        private async Task DownloadClientConfig(int serverId)
        {
            string url = $"https://downgrad.com/api/downloadfreeclient/{serverId}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        
                        if (response.Headers.Contains("X-Client-ID"))
                        {

                            ClientManager.ReservedClientID = int.Parse(response.Headers.GetValues("X-Client-ID").FirstOrDefault());
                            AppendLog($"Reserved Client ID: {ClientManager.ReservedClientID}");
                        }

                       
                        var clientConfig = await response.Content.ReadAsByteArrayAsync();
                        string filePath = Path.Combine(Environment.CurrentDirectory, "client.ovpn");
                        File.WriteAllBytes(filePath, clientConfig);

                        AppendLog($"Downloaded and saved client config to: {filePath}");
                    }
                    else
                    {
                        AppendLog("Failed to download client config");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error while downloading client config: {ex.Message}");
            }
        }



        private Process openVpnProcess;

        private void AppendLog(string logMessage)
        {
            if (logMessage != null)
            {
                LogTextBox.Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText(logMessage + Environment.NewLine);
                    LogTextBox.CaretIndex = LogTextBox.Text.Length;
                    LogTextBox.ScrollToEnd();
                });
            }
        }

        private async void OnClick3(object sender, RoutedEventArgs e)
        {
            var processManager = ProcessManager.Instance;

            if (!processManager.IsProcessRunning())
            {
               
                StartVpn();
            }
            else
            {
               
                StopVpn();
            }
        }

        private async void StartVpn()
        {
            await DownloadClientConfig(reservedServerID);
            string protocol = udpRadioButton.IsChecked == true ? "udp" : "tcp";
            
            var processManager = ProcessManager.Instance;
            processManager.InitializeProcess();
            var openVpnProcess = processManager.OurProcess;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "openvpn", "bin", "openvpn.exe"),
                Arguments = "--config client.ovpn",
                Verb = "runas",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            openVpnProcess.StartInfo = startInfo;
            openVpnProcess.OutputDataReceived += (sender, e) => AppendLog(e.Data);
            openVpnProcess.ErrorDataReceived += (sender, e) => AppendLog(e.Data);

            openVpnProcess.Start();
            openVpnProcess.BeginOutputReadLine();
            openVpnProcess.BeginErrorReadLine();

            LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText("Starting VPN" + Environment.NewLine));
            await openVpnProcess.WaitForExitAsync();
            LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText("VPN process exited" + Environment.NewLine));

            //string protocol = udpRadioButton.IsChecked == true ? "udp" : "tcp";
            //openVpnProcess = new Process();
            //ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.FileName = @"C:\Program Files\OpenVPN\bin\openvpn.exe";
            //startInfo.Arguments = $"--config sw_downgrad_{protocol}.ovpn";
            //startInfo.Verb = "runas";
            //startInfo.CreateNoWindow = true; // Prevents the terminal window from being displayed
            //startInfo.UseShellExecute = false; // Allows redirection of standard output and error
            //startInfo.RedirectStandardOutput = true; // Redirects standard output
            //startInfo.RedirectStandardError = true; // Redirects standard error

            //openVpnProcess.StartInfo = startInfo;
            //openVpnProcess.OutputDataReceived += (sender, e) => AppendLog(e.Data);
            //openVpnProcess.ErrorDataReceived += (sender, e) => AppendLog(e.Data);

            //openVpnProcess.Start();
            //openVpnProcess.BeginOutputReadLine();
            //openVpnProcess.BeginErrorReadLine();

            //LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText("Starting VPN" + Environment.NewLine));
            //await openVpnProcess.WaitForExitAsync();
            //LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText("VPN process exited" + Environment.NewLine));
        }

        private void StopVpn()
        {
            var processManager = ProcessManager.Instance;
            processManager.KillProcess();
            AppendLog($"Client released{ClientManager.ReservedClientID}");
          
            LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText("Stopping VPN" + Environment.NewLine));
        }
        //private void AppendLog(string logMessage)
        //{
        //    if (logMessage != null)
        //    {
        //        LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText(logMessage + Environment.NewLine));
        //    }
        //}
        //private void OnClick3(object sender, RoutedEventArgs e)
        //{
        //    StartVpn();
        //    //if (_openVpnProcess == null || _openVpnProcess.HasExited)
        //    //{
        //    //    StartVpn();
        //    //}
        //    //else
        //    //{
        //    //    StopVpn();
        //    //}
        //}


        //private async void StartVpn()
        //{

        //    Process openVpnProcess = new Process();
        //    ProcessStartInfo startInfo = new ProcessStartInfo();
        //    startInfo.FileName = @"C:\Program Files\OpenVPN\bin\openvpn.exe";
        //    startInfo.Arguments = "--config sw_downgrad_udp.ovpn";
        //    startInfo.Verb = "runas";
        //    startInfo.CreateNoWindow = true; // Prevents the terminal window from being displayed
        //    startInfo.UseShellExecute = false; // Allows redirection of standard output and error
        //    startInfo.RedirectStandardOutput = true; // Redirects standard output
        //    startInfo.RedirectStandardError = true; // Redirects standard error

        //    openVpnProcess.StartInfo = startInfo;
        //    openVpnProcess.OutputDataReceived += (sender, e) => AppendLog(e.Data);
        //    openVpnProcess.ErrorDataReceived += (sender, e) => AppendLog(e.Data);

        //    openVpnProcess.Start();
        //    openVpnProcess.BeginOutputReadLine();
        //    openVpnProcess.BeginErrorReadLine();

        //    LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText("Starting VPN"));
        //    await openVpnProcess.WaitForExitAsync();


        //    LogTextBox.AppendText("Config file not found\n");
        //    return;
        //}

        //ProcessStartInfo startInfo = new ProcessStartInfo
        //{
        //    FileName = @"C:\Program Files\OpenVPN\bin\openvpn.exe", 
        //    Arguments = $"--config \"{configPath}\"",
        //    RedirectStandardOutput = true,
        //    RedirectStandardError = true,
        //    UseShellExecute = false,
        //    CreateNoWindow = true
        //};

        //_openVpnProcess = new Process { StartInfo = startInfo };
        //_openVpnProcess.OutputDataReceived += (sender, args) => LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText(args.Data + "\n"));
        //_openVpnProcess.ErrorDataReceived += (sender, args) => LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText("ERROR: " + args.Data + "\n"));

        //try
        //{
        //    LogTextBox.AppendText("Starting OpenVPN process...\n");
        //    _openVpnProcess.Start();
        //    _openVpnProcess.BeginOutputReadLine();
        //    _openVpnProcess.BeginErrorReadLine();
        //    powerBtn.Background = Brushes.Red;
        //}
        //catch (Exception ex)
        //{
        //    LogTextBox.Dispatcher.Invoke(() => LogTextBox.AppendText($"Failed to start OpenVPN: {ex.Message}\n"));
        //}
    //}

        //private void disconnect()
        //{
        //    Process.Start(new ProcessStartInfo
        //    {

        //    }).WaitForExit();
        //}

        //private void StopVpn()
        //{
        //    if (_openVpnProcess != null && !_openVpnProcess.HasExited)
        //    {
        //        try
        //        {
        //            _openVpnProcess.Kill();
        //            _openVpnProcess.WaitForExit();
        //            LogTextBox.AppendText("VPN Disconnected.\n");
        //        }
        //        catch (Exception ex)
        //        {
        //            LogTextBox.AppendText($"Failed to stop OpenVPN: {ex.Message}\n");
        //        }
        //        finally
        //        {
        //            _openVpnProcess?.Dispose();
        //            _openVpnProcess = null;
        //            powerBtn.Background = Brushes.Blue;
        //        }
        //    }
        //}
    }
}