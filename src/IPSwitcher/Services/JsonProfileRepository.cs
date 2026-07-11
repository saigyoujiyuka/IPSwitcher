using System.IO;
using System.Text.Json;
using IPSwitcher.Models;

namespace IPSwitcher.Services;

public sealed class JsonProfileRepository : IProfileRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _filePath;
    private readonly string _dirPath;

    public JsonProfileRepository()
    {
        _dirPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IPSwitcher");
        _filePath = Path.Combine(_dirPath, "profiles.json");
    }

    public IReadOnlyList<NetworkProfile> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new List<NetworkProfile>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var container = JsonSerializer.Deserialize<ProfileContainer>(json, Options);
            return container?.Profiles ?? new List<NetworkProfile>();
        }
        catch
        {
            return new List<NetworkProfile>();
        }
    }

    public void Save(IReadOnlyList<NetworkProfile> profiles)
    {
        Directory.CreateDirectory(_dirPath);
        var container = new ProfileContainer { Profiles = profiles.ToList() };
        var json = JsonSerializer.Serialize(container, Options);
        File.WriteAllText(_filePath, json);
    }

    public string ExportToFile(string path, IReadOnlyList<NetworkProfile> profiles)
    {
        var container = new ProfileContainer { Profiles = profiles.ToList() };
        var json = JsonSerializer.Serialize(container, Options);
        File.WriteAllText(path, json);
        return path;
    }

    public IReadOnlyList<NetworkProfile> ImportFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var container = JsonSerializer.Deserialize<ProfileContainer>(json, Options)
            ?? throw new InvalidDataException("配置文件格式无效");
        return container.Profiles ?? new List<NetworkProfile>();
    }
}
