using DowngradVPN.Core;
using DowngradVPN.Models;
using DowngradVPN.MVVM.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DowngradVPN.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {


        public static class GlobalData
        {
            
            public static ResponseJSON SerializedJson { get; set; }
        }

        //public class ServersResponse
        //{
        //    public ResponseJSON serversList {  get; set; }  
        //}

        public class Server
        {
            [JsonPropertyName("server_id")]
            public int ServerID { get; set; }

            [JsonPropertyName("server_country")]
            public string ServerCountry { get; set; }

            [JsonPropertyName("server_name")]
            public string ServerName { get; set; }

            [JsonPropertyName("total_clients")]
            public int TotalClients { get; set; }

            [JsonPropertyName("used_clients")]
            public int UsedClients { get; set; }
        }

        public class ResponseJSON
        {
            [JsonPropertyName("servers")]
            public Server[] Servers { get; set; }
        }

        public class LoginResponse
        {
            [JsonPropertyName("user_id")]
            public int UserId { get; set; }

            [JsonPropertyName("user_email")]
            public string UserEmail { get; set; }

            [JsonPropertyName("user_password")]
            public string UserPassword { get; set; }

            [JsonPropertyName("session_key")]
            public string SessionKey { get; set; }

        }



        public async Task FetchServersAsync()
        {
            using (var client = new HttpClient())
            {
                var endpoint = new Uri("http://147.45.77.19:8080/servers");

                
                var payload = new { user_id = UserData.Id };
                var requestData = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var result = await client.PostAsync(endpoint, requestData);
                var json = await result.Content.ReadAsStringAsync(); // Read response as async

                if (result.IsSuccessStatusCode)
                {
                    GlobalData.SerializedJson = JsonSerializer.Deserialize<ResponseJSON>(json);
                }
                else
                {
                    Console.WriteLine("Failed to fetch servers");
                }
            }
        }


        private const string _StoredUsername = "admin";
        private const string _StoredPassword = "password";

        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand SigninViewCommand { get; set; }
        public RelayCommand SignOutCommand { get; set; }

        public HomeView homeVM {  get; set; }
        public SigninView SigninView { get; set; }
        //public SigninViewModel signinVM { get; set; }

        
        private object _currentView;

        public object CurrentView
        {

            get { return _currentView; }
            set
            {
                _currentView = value;
                OnProperyChanged();
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnProperyChanged(); 
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            using (var client = new HttpClient())
            {
                var endpoint = new Uri("http://147.45.77.19:8080/login");
                var loginData = new { email = username, password = password };
                var jsonContent = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

                try
                {
                    var result = await client.PostAsync(endpoint, jsonContent);
                    if (result.IsSuccessStatusCode)
                    {
                        var json = await result.Content.ReadAsStringAsync();
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json);

                        if (loginResponse != null)
                        {
                            UserData.Id = loginResponse.UserId;
                            UserData.Email = loginResponse.UserEmail;
                            UserData.PasswordHash = loginResponse.UserPassword;
                            UserData.SessionKey = loginResponse.SessionKey;

                            UserData.SaveToFile();
                        }
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }



        public async Task performLoginAsync()
        {
            if (await LoginAsync(SigninView.Username, SigninView.Password))
            {             
                CurrentView = homeVM;
                Title = "DowngradVPN(v1.0.1) - Home"; 
            }
            else
            {
                Title = "DowngradVPN(v1.0.1) - Sign In";
            }
        }

        public MainViewModel()
        {
            homeVM = new HomeView(this);
            SigninView = new SigninView();

            CurrentView = SigninView;
            Title = "DowngradVPN(v1.0.1) - Signining in";

            if (UserData.LoadFromFile())
            {  
                CurrentView = homeVM;
                Title = "DowngradVPN(v1.0.1) - Home";
                FetchServersAsync();
            }
            else
            {  
                CurrentView = SigninView;
                Title = "DowngradVPN(v1.0.1) - Signing in";
            }

            SignOutCommand = new RelayCommand(o =>
            {
               
                UserData.ClearData();      
                CurrentView = SigninView;
                Title = "DowngradVPN(v1.0.1) - Signing in";
            });
            HomeViewCommand = new RelayCommand(async o =>
            {
               
                //CurrentView = homeVM;
                await performLoginAsync();
                await FetchServersAsync();
                //if (o is SigninView view)
                //{
                //    var username = view.txtUsername.Text;
                //    var password = view.txtPassword.Password;

                //    if (username == _StoredUsername && password == _StoredPassword)
                //    {
                //        CurrentView = homeVM;
                //    }
                //    else
                //    {
                //        MessageBox.Show("Invalid username or password.");
                //    }
                //}



                // POST METHOD  - email: hashed_password 

                // POST method response - user's servers or error
                // send error if user credentials is incorrect


                //hash password


                //Title = "DowngradVPN(v1.0.1dev) - Home";
                //if (SigninView.Username == _StoredUsername && SigninView.Password == _StoredPassword)
                //{

                //    // get user email and passeord

                //    // fetch data from a server

                //    // get user data into a public structure 

                //    // if credentials is correct, then  chngae current view to homepage

                //    // load saerver and clients as json from a server by user email

                //    // load configs from a server -- change it to download only config that user selected 

                //}
                //else
                //{
                //    // Handle invalid login
                //    MessageBox.Show("Invalid username or password.");
                //}
            });
        }
    }
}
