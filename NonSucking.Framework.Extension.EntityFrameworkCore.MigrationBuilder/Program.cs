using NonSucking.Framework.Extension.EntityFrameworkCore.MigrationBuilder;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

var sourceOption
    = new Option<FileInfo>(
        new[] { "--source", "-s" }, 
        (argumentResult) => new FileInfo(argumentResult.Tokens[0].Value), 
        description: "Defines the source .dll to search for migration data"
    );

var targetOption
    = new Option<DirectoryInfo>(
        new[] { "--target", "-t" },
        (argumentResult) => new DirectoryInfo(argumentResult.Tokens[0].Value),
        description: "Defines the directory where the migration should be generated"
    );

var root = new RootCommand("search dll for DatabaseContext and generate Migration files")
            {
                 sourceOption,
                 targetOption
            };

root.SetHandler(Service.Start, sourceOption, targetOption);

var parser = new CommandLineBuilder(root).UseDefaults().Build(); 

ParseResult parsedArguments;

if (args is null || args.Length == 0)
{
    Console.Write("> ");
    var clr = Console.ReadLine();
    parsedArguments = parser.Parse(clr);
}
else
{
    parsedArguments = parser.Parse(args);
}

if (parsedArguments.Errors.Count > 0 && parsedArguments.Tokens.Count > 0)
{
    Console.WriteLine($"{parsedArguments.Errors.Count} Errors in the parse process.");
    foreach (var error in parsedArguments.Errors)
    {
        Console.WriteLine(error.Message);
    }

    parser.Invoke("-?");

    return 1;
}
else
{
    return parsedArguments.Invoke();
}