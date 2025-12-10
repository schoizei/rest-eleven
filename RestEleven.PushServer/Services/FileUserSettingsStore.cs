using System.Text.Json;
using RestEleven.Shared.Dtos;

namespace RestEleven.PushServer.Services;

public class FileUserSettingsStore : IUserSettingsStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public FileUserSettingsStore(IHostEnvironment environment)
    {
        _root = Path.Combine(environment.ContentRootPath, "data", "settings");
    }

    public async Task<UserSettingsDto?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(userId);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<UserSettingsDto>(stream, SerializerOptions, cancellationToken);
    }

    public async Task SaveAsync(string userId, UserSettingsDto settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_root);
        var path = GetPath(userId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
    }

    private string GetPath(string userId) => Path.Combine(_root, $"{userId}.json");
}
