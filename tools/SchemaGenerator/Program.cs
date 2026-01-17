using Meziantou.Framework;
using SchemaGenerator;

if (args.Length < 3)
{
    Console.WriteLine("Usage: SchemaGenerator <openApiPath> <protocolDir> <collectorDir> [--force]");
    return 1;
}

var openApiPath = FullPath.Combine(Environment.CurrentDirectory, args[0]);
var protocolDir = FullPath.Combine(Environment.CurrentDirectory, args[1]);
var collectorDir = FullPath.Combine(Environment.CurrentDirectory, args[2]);
var force = args.Contains("--force", StringComparer.OrdinalIgnoreCase);

var guard = new GenerationGuard(force ? GuardMode.Force : GenerationGuard.DefaultMode);

try
{
    SchemaGeneratorTool.Generate(openApiPath, protocolDir, collectorDir, guard);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
