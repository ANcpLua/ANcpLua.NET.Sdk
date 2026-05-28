using System.IO.Compression;
using Meziantou.Framework;
using Xunit;

namespace ANcpLua.Sdk.Tests.Infrastructure;

internal static class NuGetPackage
{
    public static string FindSingle(FullPath directory)
    {
        var matches = Directory.GetFiles(directory, "*.nupkg", SearchOption.AllDirectories)
            .Where(static f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Single(matches);
        return matches[0];
    }

    public static async Task<IReadOnlyList<string>> ExtractAsync(string nupkgPath, FullPath destination)
    {
        await ZipFile.ExtractToDirectoryAsync(nupkgPath, destination, TestContext.Current.CancellationToken);
        return Directory.GetFiles(destination, "*", SearchOption.AllDirectories);
    }
}
