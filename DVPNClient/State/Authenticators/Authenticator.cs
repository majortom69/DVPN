using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DowngradVPN.Models;
using DowngradVPN.Services.AuthenticationServices;

namespace DowngradVPN.State.Authenticators
{
    public class Authenticator : IAuthenticator
    {

        private readonly IAuthenticationService _authenticationService;

        public Authenticator(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public Account CurrentAccount { get; private set; }

        public bool IsLoggedIn => CurrentAccount != null; // если у нас есть пользователь, то тогда он авторизован

        public async Task<bool> Login(string username, string password)
        {
            bool success = true;
            try
            {
                CurrentAccount = await _authenticationService.Login(username, password);
                
            }
            catch (Exception)
            {
                success = false;
                //throw;
            }

            return success;
        }

        public void Logout()
        {
            CurrentAccount = null;
        }

    }
}
