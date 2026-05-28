using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Infrastructure;

public readonly record struct RepositoryRoot
{
    private RepositoryRoot(FullPath path) => FullPath = path;

    public FullPath FullPath { get; }

    public FullPath this[string relativePath] => FullPath / relativePath;

    public static RepositoryRoot Locate(params string[] markerFiles)
    {
        if (markerFiles.Length is 0)
            markerFiles = ["*.sln", "*.slnx", ".git"];

        var directory = FullPath.CurrentDirectory();
        while (true)
        {
            foreach (var marker in markerFiles)
                if (marker.Contains('*'))
                {
                    if (Directory.GetFiles(directory, marker).Length > 0)
                        return new RepositoryRoot(directory);
                }
                else
                {
                    var markerPath = directory / marker;
                    if (File.Exists(markerPath) || Directory.Exists(markerPath))
                        return new RepositoryRoot(directory);
                }

            var parent = directory.Parent;
            if (parent == directory)
                throw new DirectoryNotFoundException(
                    "Repository root not found. Searched for: " + string.Join(", ", markerFiles));
            directory = parent;
        }
    }

    public static implicit operator FullPath(RepositoryRoot root) => root.FullPath;

    public static implicit operator string(RepositoryRoot root) => root.FullPath;
}
