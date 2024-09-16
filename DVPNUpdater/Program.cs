/* Сборка v1.0.1 - 12/09/2024
██████▄░██░░░██░█████▄░██████▄░░░██░░░██░█████▄░██████▄░▄████▄░████████░▄█████░█████▄
██░░░██░██░░░██░██░░██░██░░░██░░░██░░░██░██░░██░██░░░██░██░░██░░░░██░░░░██░░░░░██░░██
██░░░██░██░░░██░█████▀░██░░░██░░░██░░░██░█████▀░██░░░██░██░░██░░░░██░░░░█████░░█████▀
██░░░██░██░░██░░██░░░░░██░░░██░░░██░░░██░██░░░░░██░░░██░██████░░░░██░░░░██░░░░░██░░██
██████▀░▀███▀░░░██░░░░░██░░░██░░░▀█████▀░██░░░░░██████▀░██░░██░░░░██░░░░▀█████░██░░██
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
 */

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    // Текущяя директория, из которой запускается обновщик
    private static readonly string updaterDirectory = AppDomain.CurrentDomain.BaseDirectory;

    // Путь до самого VPN приложения
    private static readonly string projectDirectory = Path.GetFullPath(Path.Combine(updaterDirectory, "..")); 

    public static async Task Main(string[] args)
    {

        // СОДЕРЖИМОЕ МАНИФЕСТА
        // FILE_NAME : FILE_HASH

        // Локальнй манифест файл
        var jsonFilePath1 = Path.Combine(updaterDirectory, "fileHashes.json"); 

        // Манифест файл с  последнего обновления 
        var serverJsonUrl = "https://downgrad.com/fileHashes.json";

        // Ссылка на директория с самыми последними файлами
        var filesBaseUrl = "https://downgrad.com/downgradvpn/"; 

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var clientFileContents = await ReadLocalJsonFileAsync(jsonFilePath1, options);
        var serverFileContents = await DownloadJsonFileAsync(serverJsonUrl, options);

        if (clientFileContents?.Files != null && serverFileContents?.Files != null)
        {
            var (addedFiles, removedFiles, outdatedFiles) = FindDifferences(clientFileContents.Files, serverFileContents.Files);

            if (addedFiles.Count > 0)
            {
                Console.WriteLine("Files added:");
                foreach (var fileName in addedFiles)
                {
                    Console.WriteLine(fileName);
                    await DownloadFileAsync(filesBaseUrl + fileName, Path.Combine(projectDirectory, fileName));
                }
            }
            else
            {
                Console.WriteLine("No files added.");
            }

            if (removedFiles.Count > 0)
            {
                Console.WriteLine("Files removed:");
                foreach (var fileName in removedFiles)
                {
                    Console.WriteLine(fileName);
                    DeleteFile(Path.Combine(projectDirectory, fileName));
                }
            }
            else
            {
                Console.WriteLine("No files removed.");
            }

            if (outdatedFiles.Count > 0)
            {
                Console.WriteLine("Files updated:");
                foreach (var fileName in outdatedFiles)
                {
                    Console.WriteLine(fileName);
                    await DownloadFileAsync(filesBaseUrl + fileName, Path.Combine(projectDirectory, fileName));
                }
            }
            else
            {
                Console.WriteLine("No files updated.");
            }
            await UpdateLocalJsonFileAsync(Path.Combine(updaterDirectory, "fileHashes.json"), serverFileContents);
        }
        RunMainApplication();
    }

    static async Task<FileHashCollection?> ReadLocalJsonFileAsync(string filePath, JsonSerializerOptions options)
    {
        try
        {
            using FileStream openStream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<FileHashCollection>(openStream, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading local JSON file {filePath}: {ex.Message}");
            return null;
        }
    }

    static async Task<FileHashCollection?> DownloadJsonFileAsync(string url, JsonSerializerOptions options)
    {
        try
        {
            var response = await httpClient.GetStringAsync(url);
            return JsonSerializer.Deserialize<FileHashCollection>(response, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading JSON file: {ex.Message}");
            return null;
        }
    }

    static (List<string> addedFiles, List<string> removedFiles, List<string> outdatedFiles) FindDifferences(List<JsonFile> clientFiles, List<JsonFile> serverFiles)
    {
        var clientFileDict = clientFiles.ToDictionary(f => f.Name, f => f.Hash);
        var serverFileDict = serverFiles.ToDictionary(f => f.Name, f => f.Hash);

        var addedFiles = serverFiles.Where(f => !clientFileDict.ContainsKey(f.Name)).Select(f => f.Name).ToList();
        var removedFiles = clientFiles.Where(f => !serverFileDict.ContainsKey(f.Name)).Select(f => f.Name).ToList();
        var outdatedFiles = serverFiles.Where(f => clientFileDict.ContainsKey(f.Name) && clientFileDict[f.Name] != f.Hash).Select(f => f.Name).ToList();

        return (addedFiles, removedFiles, outdatedFiles);
    }

    static async Task DownloadFileAsync(string url, string filePath)
    {
        try
        {
            var fileBytes = await httpClient.GetByteArrayAsync(url);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllBytesAsync(filePath, fileBytes);
            Console.WriteLine($"Downloaded and saved {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file {filePath}: {ex.Message}");
        }
    }

    static void DeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"Deleted {filePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
        }
    }

    static async Task UpdateLocalJsonFileAsync(string updaterJsonFilePath, FileHashCollection serverFileHashes)
    {
        try
        {
            var json = JsonSerializer.Serialize(serverFileHashes);
            await File.WriteAllTextAsync(updaterJsonFilePath, json);
            Console.WriteLine($"Updated {updaterJsonFilePath} with new file hashes.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating JSON file: {ex.Message}");
        }
    }

    static void RunMainApplication()
    {
        try
        {        
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;      
            string parentDirectory = Directory.GetParent(Directory.GetParent(currentDirectory)?.FullName)?.FullName;     
            string appPath = Path.Combine(parentDirectory ?? "", "DowngradVPN.exe");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true, 
                WorkingDirectory = parentDirectory 
            };

            if (File.Exists(appPath))
            {
                Process.Start(startInfo);
                //Console.WriteLine("DowngradVPN.exe has been started successfully.");
            }
            else
            {
                Console.WriteLine($"DowngradVPN.exe not found at path: {appPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }
}

public class JsonFile
{
    public string Name { get; set; }
    public string Hash { get; set; }
}

public class FileHashCollection
{
    [JsonPropertyName("files")]
    public List<JsonFile> Files { get; set; }
}




//using System.Text.Json;
//using System.Text.Json.Serialization;

//class Program
//{
//    private static readonly HttpClient httpClient = new HttpClient();
//    private static readonly string updaterDirectory = AppDomain.CurrentDomain.BaseDirectory;
//    private static readonly string projectDirectory = Path.GetFullPath(Path.Combine(updaterDirectory, ".."));

//    public static async Task Main(string[] args)
//    {
//        var jsonFilePath1 = Path.Combine(updaterDirectory, "fileHashes.json");
//        var serverJsonUrl = "http://147.45.77.19/fileHashes.json";
//        var filesBaseUrl = "http://147.45.77.19/downgradvpn/";

//        var options = new JsonSerializerOptions
//        {
//            PropertyNameCaseInsensitive = true
//        };

//        var clientFileContents = await ReadLocalJsonFileAsync(jsonFilePath1, options);
//        var serverFileContents = await DownloadJsonFileAsync(serverJsonUrl, options);

//        if (clientFileContents?.Files != null && serverFileContents?.Files != null)
//        {
//            var (addedFiles, removedFiles, outdatedFiles) = FindDifferences(clientFileContents.Files, serverFileContents.Files);

//            foreach (var fileName in addedFiles)
//                await DownloadFileAsync(filesBaseUrl + fileName, Path.Combine(projectDirectory, fileName));

//            foreach (var fileName in removedFiles)
//                DeleteFile(Path.Combine(projectDirectory, fileName));

//            foreach (var fileName in outdatedFiles)
//                await DownloadFileAsync(filesBaseUrl + fileName, Path.Combine(projectDirectory, fileName));

//            await UpdateLocalJsonFileAsync(Path.Combine(updaterDirectory, "fileHashes.json"), serverFileContents);
//        }
//    }

//    static async Task<FileHashCollection?> ReadLocalJsonFileAsync(string filePath, JsonSerializerOptions options)
//    {
//        try
//        {
//            using FileStream openStream = File.OpenRead(filePath);
//            return await JsonSerializer.DeserializeAsync<FileHashCollection>(openStream, options);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error reading local JSON file {filePath}: {ex.Message}");
//            return null;
//        }
//    }

//    static async Task<FileHashCollection?> DownloadJsonFileAsync(string url, JsonSerializerOptions options)
//    {
//        try
//        {
//            var response = await httpClient.GetStringAsync(url);
//            return JsonSerializer.Deserialize<FileHashCollection>(response, options);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error downloading JSON file: {ex.Message}");
//            return null;
//        }
//    }

//    static (List<string> addedFiles, List<string> removedFiles, List<string> outdatedFiles) FindDifferences(List<JsonFile> clientFiles, List<JsonFile> serverFiles)
//    {
//        var clientFileDict = clientFiles.ToDictionary(f => f.Name, f => f.Hash);
//        var serverFileDict = serverFiles.ToDictionary(f => f.Name, f => f.Hash);

//        var addedFiles = serverFiles.Where(f => !clientFileDict.ContainsKey(f.Name)).Select(f => f.Name).ToList();
//        var removedFiles = clientFiles.Where(f => !serverFileDict.ContainsKey(f.Name)).Select(f => f.Name).ToList();
//        var outdatedFiles = serverFiles.Where(f => clientFileDict.ContainsKey(f.Name) && clientFileDict[f.Name] != f.Hash).Select(f => f.Name).ToList();

//        return (addedFiles, removedFiles, outdatedFiles);
//    }

//    static async Task DownloadFileAsync(string url, string filePath)
//    {
//        try
//        {
//            var fileBytes = await httpClient.GetByteArrayAsync(url);
//            var directory = Path.GetDirectoryName(filePath);
//            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
//            {
//                Directory.CreateDirectory(directory);
//            }
//            await File.WriteAllBytesAsync(filePath, fileBytes);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error downloading file {filePath}: {ex.Message}");
//        }
//    }

//    static void DeleteFile(string filePath)
//    {
//        try
//        {
//            if (File.Exists(filePath))
//            {
//                File.Delete(filePath);
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
//        }
//    }

//    static async Task UpdateLocalJsonFileAsync(string updaterJsonFilePath, FileHashCollection serverFileHashes)
//    {
//        try
//        {
//            var json = JsonSerializer.Serialize(serverFileHashes);
//            await File.WriteAllTextAsync(updaterJsonFilePath, json);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error updating JSON file: {ex.Message}");
//        }
//    }
//}

//public class JsonFile
//{
//    public string Name { get; set; }
//    public string Hash { get; set; }
//}

//public class FileHashCollection
//{
//    [JsonPropertyName("files")]
//    public List<JsonFile> Files { get; set; }
//}
