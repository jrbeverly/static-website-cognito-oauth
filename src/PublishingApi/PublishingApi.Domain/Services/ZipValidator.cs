using System.IO.Compression;

namespace PublishingApi.Domain.Services;

public static class ZipValidator
{
    public sealed record ValidationResult(bool IsValid, string? Error, List<ZipArchiveEntry>? Entries);

    private const int MaxEntryCount = 10_000;

    public static ValidationResult Validate(ZipArchive archive, long maxUncompressedMb = 500)
    {
        var entries = archive.Entries.Where(e => !e.FullName.EndsWith('/')).ToList();

        if (entries.Count > MaxEntryCount)
            return new ValidationResult(false,
                $"Entry count ({entries.Count}) exceeds {MaxEntryCount} limit", null);

        foreach (var entry in entries)
        {
            if (entry.FullName.Contains(".."))
                return new ValidationResult(false, $"Path traversal detected: {entry.FullName}", null);
        }

        var maxUncompressed = maxUncompressedMb * 1024L * 1024L;
        var totalUncompressed = entries.Sum(e => e.Length);

        if (totalUncompressed > maxUncompressed)
            return new ValidationResult(false,
                $"Uncompressed size ({totalUncompressed / 1024 / 1024} MB) exceeds {maxUncompressedMb} MB limit",
                null);

        return new ValidationResult(true, null, entries);
    }
}
