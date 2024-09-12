using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DowngradVPN.Models;

namespace DowngradVPN.State.Authenticators
{
    public interface IAuthenticator
    {
        Account CurrentAccount { get; }
        bool IsLoggedIn { get; }

        Task<bool> Login(string username, string password);
        void Logout();
    }
}
