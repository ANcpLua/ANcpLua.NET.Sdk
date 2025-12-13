using System.IO;
using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Helpers;

internal static class PathHelpers
{
    public  static FullPath GetRootDirectory()
    {
        var directory = FullPath.CurrentDirectory();
        while (true)
        {
            if (File.Exists(directory / "ANcpLua.NET.Sdk.slnx"))
                return directory;

            var parent = directory.Parent;
            if (parent == directory)
                throw new DirectoryNotFoundException("Cannot locate repository root (missing ANcpLua.NET.Sdk.slnx).");

            directory = parent;
        }
    }
}
