using System.IO.Compression;
using FluentAssertions;
using PublishingApi.Domain.Services;
using Xunit;

namespace PublishingApi.Tests.Unit.Domain;

public class ZipValidationTests
{
    [Fact]
    public void Validate_ValidZip_ReturnsValid()
    {
        using var archive = CreateZipArchive(new Dictionary<string, string>
        {
            ["index.html"] = "<html></html>",
            ["style.css"] = "body {}"
        });

        var result = ZipValidator.Validate(archive);

        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_ZipWithPathTraversal_ReturnsErrorWithOffendingEntry()
    {
        using var archive = CreateZipArchive(new Dictionary<string, string>
        {
            ["../evil.html"] = "<h1>Hacked</h1>",
            ["index.html"] = "<html></html>"
        });

        var result = ZipValidator.Validate(archive);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Path traversal");
        result.Error.Should().Contain("../evil.html");
    }

    [Fact]
    public void Validate_ZipExceedingUncompressedLimit_ReturnsError()
    {
        using var archive = CreateLargeZipArchive(2_000_000);

        var result = ZipValidator.Validate(archive, maxUncompressedMb: 1);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds");
    }

    [Fact]
    public void Validate_EmptyZip_ReturnsValidWithNoEntries()
    {
        using var archive = CreateZipArchive(new Dictionary<string, string>());

        var result = ZipValidator.Validate(archive);

        result.IsValid.Should().BeTrue();
        result.Entries.Should().BeEmpty();
    }

    private static ZipArchive CreateZipArchive(Dictionary<string, string> files)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        ms.Position = 0;
        return new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
    }

    private static ZipArchive CreateLargeZipArchive(long targetSizeBytes)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("large.txt", CompressionLevel.NoCompression);
            using var writer = new BinaryWriter(entry.Open());
            var chunk = new byte[81920];
            long written = 0;
            while (written < targetSizeBytes)
            {
                var toWrite = (int)Math.Min(chunk.Length, targetSizeBytes - written);
                writer.Write(chunk, 0, toWrite);
                written += toWrite;
            }
        }
        ms.Position = 0;
        return new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
    }
}
