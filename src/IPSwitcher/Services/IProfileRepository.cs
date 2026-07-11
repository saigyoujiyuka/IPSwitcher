using IPSwitcher.Models;

namespace IPSwitcher.Services;

public interface IProfileRepository
{
    IReadOnlyList<NetworkProfile> Load();

    void Save(IReadOnlyList<NetworkProfile> profiles);

    string ExportToFile(string path, IReadOnlyList<NetworkProfile> profiles);

    IReadOnlyList<NetworkProfile> ImportFromFile(string path);
}

public sealed class ProfileContainer
{
    public List<NetworkProfile> Profiles { get; set; } = new();
}
