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
using System.Windows.Media.Animation;
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
        private int _reservedServerUsage = -1;

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



       
        public HomeView(MainViewModel viewModel)
        {
            InitializeComponent();
            ShowSpinner(true); // Show spinner until data is fetched
            Loaded += async (s, e) =>
            {
                await InitializeData(viewModel);
                ShowSpinner(false); // Hide spinner after data is ready
            };
        }

        private async Task InitializeData(MainViewModel viewModel)
        {
            await viewModel.FetchServersAsync(); // Wait for data
            InitializeCountries(); // Initialize countries after data is fetched
            CountryComboBox.ItemsSource = Countries;
            CountryComboBox.SelectedIndex = Countries.Any() ? 0 : -1; // Select first if available
        }


        private async void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CountryComboBox.SelectedItem is CountryItem selectedCountry)
            {
                AppendLog($"Selected Country: {selectedCountry.Name}");

                // Fetch the client config file from the server asynchronously
                //await DownloadClientConfig(selectedCountry.id);
                reservedServerID = selectedCountry.id;
                _reservedServerUsage = selectedCountry.usage;

            }
        }

//         private async Task DownloadClientConfig(int serverId)
//      {
//          string protocol = ovpnRadioButton.IsChecked == true ? "ovpn" : "wgrd";
//          string url = $"http://localhost:7070/api/downloadfreeclient/{serverId}?protocol={protocol}";
        
//          try
//          {
//              using (HttpClient client = new HttpClient())
//              {
//                  HttpResponseMessage response = await client.GetAsync(url);
        
//                  if (response.IsSuccessStatusCode)
//                  {
//                      // Read the Client ID from the headers
//                      if (response.Headers.Contains("X-Client-ID"))
//                      {
//                          ClientManager.ReservedClientID = int.Parse(response.Headers.GetValues("X-Client-ID").FirstOrDefault());
//                          AppendLog($"Reserved Client ID: {ClientManager.ReservedClientID}");
//                      }
        
//                      // Save the client config
//                      var clientConfig = await response.Content.ReadAsByteArrayAsync();
//                      string filePath = Path.Combine(Environment.CurrentDirectory, $"client.{(protocol == "ovpn" ? "ovpn" : "conf")}");
//                      File.WriteAllBytes(filePath, clientConfig);
        
//                      AppendLog($"Downloaded and saved client config to: {filePath}");
//                  }
//                  else
//                  {
//                      AppendLog("Failed to download client config");
//                  }
//              }
//          }
//          catch (Exception ex)
//          {
//              AppendLog($"Error while downloading client config: {ex.Message}");
//          }
//      }


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
                        // Read the Client ID from the headers
                        if (response.Headers.Contains("X-Client-ID"))
                        {

                            ClientManager.ReservedClientID = int.Parse(response.Headers.GetValues("X-Client-ID").FirstOrDefault());
                            AppendLog($"Reserved Client ID: {ClientManager.ReservedClientID}");
                        }

                        // Save the client config
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


        private void ShowSpinner(bool isVisible)
        {
            Dispatcher.Invoke(() =>
            {
                SpinnerGrid.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                PowerIcon.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;

                if (isVisible)
                {
                    StartSpinnerAnimation();
                }
                else
                {
                    StopSpinnerAnimation();
                }
            });
        }

        private void StartSpinnerAnimation()
        {
          
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)), 
                RepeatBehavior = RepeatBehavior.Forever
            };

           
            SpinnerRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        private void StopSpinnerAnimation()
        {
          
            SpinnerRotation.BeginAnimation(RotateTransform.AngleProperty, null);
        }


        private async void StartVpn()
        {
           
            Dispatcher.Invoke(() =>
            {
                powerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0BEC5")); // Gray
                ShowSpinner(true); 
            });

            bool fatalErrorOccurred = false; 
            bool connectionSuccessful = false; 
            try
            {
                if (_reservedServerUsage == 100)
                {
                    MessageBox.Show("This server is full. Please select another one.");
                    return;
                }

                var runningOpenVpnProcesses = Process.GetProcessesByName("openvpn");
                if (runningOpenVpnProcesses.Length > 0)
                {
                    foreach (var process in runningOpenVpnProcesses)
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit();
                            Dispatcher.Invoke(() => AppendLog("Killed existing OpenVPN process."));
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => AppendLog($"Failed to kill OpenVPN process: {ex.Message}"));
                        }
                    }
                }

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

                openVpnProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.Invoke(() => AppendLog(e.Data));

                        if (e.Data.Contains("Exiting due to fatal error"))
                        {
                            fatalErrorOccurred = true;
                            Dispatcher.Invoke(() =>
                            {
                                powerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")); // Red
                                AppendLog("Fatal error detected! Changing button to red.");
                            });
                            ClientManager.ReleaseClientAsync();
                        }

                        if (e.Data.Contains("Initialization Sequence Completed"))
                        {
                            connectionSuccessful = true;
                            Dispatcher.Invoke(() =>
                            {
                                ShowSpinner(false);
                                powerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); // Green
                                AppendLog("Connection successful! Changing button to green.");
                            });
                        }
                    }
                };

                openVpnProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.Invoke(() => AppendLog(e.Data));

                        if (e.Data.Contains("Exiting due to fatal error"))
                        {
                            fatalErrorOccurred = true;
                            Dispatcher.Invoke(() =>
                            {
                                powerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")); // Red
                                AppendLog("Fatal error detected! Changing button to red.");
                            });
                            ClientManager.ReleaseClientAsync();
                        }
                    }
                };

                openVpnProcess.Start();
                openVpnProcess.BeginOutputReadLine();
                openVpnProcess.BeginErrorReadLine();

                Dispatcher.Invoke(() => AppendLog("Starting VPN"));
                await openVpnProcess.WaitForExitAsync();

                if (!fatalErrorOccurred && connectionSuccessful)
                {
                    // Connection successful, button is already green
                }
                else if (!fatalErrorOccurred)
                {
                    Dispatcher.Invoke(() => AppendLog("VPN process exited without fatal error, but connection was not established."));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    powerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")); // Red
                    AppendLog($"Error starting VPN: {ex.Message}");
                });
                ClientManager.ReleaseClientAsync();
            }
            finally
            {

                Dispatcher.Invoke(() =>
                {
                    ShowSpinner(false); 
                });
            }
        }






        private void StopVpn()
        {
            try
            {
                ShowSpinner(true);
                var processManager = ProcessManager.Instance;
                processManager.KillProcess();
                AppendLog($"Client released {ClientManager.ReservedClientID}");

               
                

                AppendLog("Stopping VPN");
                powerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")); // Blue
                ShowSpinner(true);
            }
            catch (Exception ex)
            {
                AppendLog($"Error stopping VPN: {ex.Message}");
            }
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