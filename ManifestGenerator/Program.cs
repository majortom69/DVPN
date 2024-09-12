/* Сборка v1.0.1 - 12/09/2024
▄██▄▄██▄░▄████▄░██████▄░██████░██████░▄█████░▄██████░████████░░░▄██████░▄█████░██████▄
██░██░██░██░░██░██░░░██░░░██░░░██░░░░░██░░░░░██░░░░░░░░░██░░░░░░██░░░░░░██░░░░░██░░░██
██░██░██░██░░██░██░░░██░░░██░░░█████░░█████░░▀█████▄░░░░██░░░░░░██░░███░█████░░██░░░██
██░██░██░██████░██░░░██░░░██░░░██░░░░░██░░░░░░░░░░██░░░░██░░░░░░██░░░██░██░░░░░██░░░██
██░██░██░██░░██░██░░░██░██████░██░░░░░▀█████░██████▀░░░░██░░░░░░▀█████▀░▀█████░██░░░██
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ 

Генератор манифест файла для обновщика
СОДЕРЖИМОЕ МАНИФЕСТА
FILE_NAME : FILE_HASH
 */

using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

var parentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../");
var outputFileName = "fileHashes.json";

try
{
    var fileHashes = new List<FileHash>();

    foreach (var filePath in Directory.GetFiles(parentDirectory))
    {
        var fileName = Path.GetFileName(filePath);
        var md5Hash = await GetMd5HashAsync(filePath);
        fileHashes.Add(new FileHash { Name = fileName, Hash = md5Hash });
    }

    var result = new { files = fileHashes };
    var jsonString = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

    
    await File.WriteAllTextAsync(outputFileName, jsonString);

    await File.WriteAllTextAsync(outputFileName, jsonString);
    Console.WriteLine($"Hashes written to {outputFileName}");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}

async Task<string> GetMd5HashAsync(string filePath)
{
    using var md5 = MD5.Create();
    await using var stream = File.OpenRead(filePath);
    var hashBytes = await md5.ComputeHashAsync(stream);
    var sb = new StringBuilder();
    foreach (var b in hashBytes)
    {
        sb.Append(b.ToString("x2"));
    }
    return sb.ToString();
}

public class FileHash
{
    public string Name { get; set; }
    public string Hash { get; set; }
}

