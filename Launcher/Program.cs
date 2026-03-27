using Parnassus.Launcher;
using Spectre.Console;

if (args.Length <= 0)
{
    Console.WriteLine("No launch argument found.");
    return;
}

LaunchManager launcher = new(args[0],
    "./Parnassus/blacklists",
    "./Parnassus/modsList.txt",
    "./Mods",
    "./Mods/blacklist.txt",
    "./Parnassus/config.json");

try
{
    string? errorMessage = launcher.Execute();
    if (errorMessage != null)
    {
        AnsiConsole.MarkupLine($"[red]{errorMessage}[/]");
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Unexpected Internal Error Occurred: {ex.Message}[/]");
    AnsiConsole.MarkupLine("Press ENTER to quit.");
    Console.ReadLine();
    return;
}