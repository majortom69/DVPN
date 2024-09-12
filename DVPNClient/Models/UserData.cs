using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DowngradVPN.Models
{
    public static class UserData
    {
        public static int Id { get; set; }
        public static string Email { get; set; }
        public static string PasswordHash { get; set; }
        public static string SessionKey { get; set; }

        public static void SaveToFile()
        {
            var data = new { Id, Email, PasswordHash, SessionKey };
            File.WriteAllText("user_data.json", JsonSerializer.Serialize(data));
        }

        public static bool LoadFromFile()
        {
            if (File.Exists("user_data.json"))
            {
                var json = File.ReadAllText("user_data.json");
                var userData = JsonSerializer.Deserialize<UserDataFile>(json);
                if (userData != null)
                {
                    Id = userData.Id;
                    Email = userData.Email;
                    PasswordHash = userData.PasswordHash;
                    SessionKey = userData.SessionKey;  // No error now
                    return true;
                }
            }
            return false;
        }

        public static void ClearData()
        {
            Id = -1;
            Email = null;
            PasswordHash = null;
            SessionKey = null;

            if (File.Exists("user_data.json"))
            {
                File.Delete("user_data.json");
            }
        }

        private class UserDataFile
        {
            public int Id { get; set; }
            public string Email { get; set; }
            public string PasswordHash { get; set; }
            public string SessionKey { get; set; }  // Removed static
        }
    }
}

//but why  we dont store hashed password? I thought we later would use it for security  so user  would not able to access someones data, but only he owns. Or do you know better solution?



