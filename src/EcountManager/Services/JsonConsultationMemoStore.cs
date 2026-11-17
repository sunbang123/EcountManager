using System.IO;
using System.Text.Json;
using EcountManager.Models;

namespace EcountManager.Services;

public sealed class JsonConsultationMemoStore : IConsultationMemoStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public JsonConsultationMemoStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _filePath = Path.Combine(appDataPath, "EcountManager", "consultation-memos.json");
    }

    public async Task<IReadOnlyList<ConsultationMemo>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath)) return [];
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<ConsultationMemo>>(stream, SerializerOptions, cancellationToken) ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(IReadOnlyCollection<ConsultationMemo> memos, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = _filePath + ".tmp";
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, memos, SerializerOptions, cancellationToken);
            }

            File.Move(temporaryPath, _filePath, true);
        }
        finally
        {
            _gate.Release();
        }
    }
}

