using AIComplianceAgent.Core.Agent;
using AIComplianceAgent.Core.Formatting;

Console.WriteLine("=== AI Compliance Agent - CLI ===");
var inputPath = Console.ReadLine();
//var inputPath = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ?? "input.json";
var format = ReadOption(args, "--format") ?? "json";

if (!File.Exists(inputPath))
{
    Console.WriteLine($"Missing input file: {inputPath}");
    return;
}

var json = File.ReadAllText(inputPath);
var result = await AgentRunner.Run(json);

Console.WriteLine(result);

static string? ReadOption(string[] args, string name)
{
    for (var index = 0; index < args.Length; index++)
    {
        if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
        {
            return args[index + 1];
        }

        if (args[index].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
        {
            return args[index][(name.Length + 1)..];
        }
    }

    return null;
}
