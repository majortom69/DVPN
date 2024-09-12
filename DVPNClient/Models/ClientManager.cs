using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public static class ClientManager
{
    public static int ReservedClientID { get; set; } = -1;

    public static async Task ReleaseClientAsync()
    {
        if (ReservedClientID == -1) return;

        string url = $"http://147.45.77.19:8080/releaseclient/{ReservedClientID}";
        using (HttpClient client = new HttpClient())
        {
            var response = await client.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Client released");
            }
            else
            {
                Console.WriteLine("Failed to release client");
            }
        }

        ReservedClientID = -1;  // Reset the ID after releasing
    }
}

