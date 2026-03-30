using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Parnassus.Launcher;

/// <summary>
/// LaunchManager: Controller that handles UI rendering and launch process.
/// </summary>
public class LaunchManager
{
    private readonly string launchCommand;
    private readonly string blacklistsDir;  // The directory of Parnassus's blacklists library. (Parnassus/blacklists)
    private readonly string modsListDir;    // The path of Parnassus's mods list. (Parnassus/modsList.txt)
    private readonly string modsDir;    // The directory of Everest mods. (Mods)
    private readonly string actualBlacklistDir; // The directory of the blacklist applied to Everest. (Mods/blacklist.txt)
    private readonly string configDir;  // The path of Parnassus's configurations and runtime data. (Parnassus/config.json)

    private BlacklistManager blacklistMan;
    private ModsListManager modsListMan;
    private SyncManager syncMan;

    private string logo = """
                                                   
                            @@@                    
                           @@@@@                   
                          @@@@@@@                  
                         @@@@@@@@                  
                        @@@@@@@@@@@@               
                      @@@@@@@@@@@@@@@@             
                     @@@@@@=-=@@@@@@@@@            
                   @@@@@@@.......*@@@@@@           
               @@@@@@@@:...........*@@@@@@@@       
              @@@@@@@@@+...........-@@@@@@@@@      
            @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@     
           @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@   
                                                   
        """;

    public LaunchManager(string _command, 
                        string _blacklistsDir, 
                        string _modsListDir, 
                        string _modsDir, 
                        string _actualBlacklistDir,
                        string _configDir)
    {
        launchCommand = _command;
        blacklistsDir = _blacklistsDir;
        modsListDir = _modsListDir;
        modsDir = _modsDir;
        actualBlacklistDir = _actualBlacklistDir;
        configDir = _configDir;

        blacklistMan = new BlacklistManager(blacklistsDir);
        modsListMan = new ModsListManager(modsListDir, modsDir);
        syncMan = new SyncManager(configDir, actualBlacklistDir, blacklistsDir);
    }

    /// <summary>
    /// The selection prompt at main menu.
    /// </summary>
    /// <returns>The selected item</returns>
    private string HandleUserSelection()
    {
        const string separatorItem = "";
        const string createProfileItem = "CREATE PROFILE...";
        const string deleteProfileItem = "DELETE PROFILE...";
        const string renameProfileItem = "RENAME PROFILE...";
        const string applyTemplateItem = "APPLY TEMPLATE...";

        string selectedName;

        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split("+")[0];

        while (true)
        {
            var mainChoices = blacklistMan.Blacklists.Keys.ToList();
            mainChoices.Sort();

            AnsiConsole.Clear();
            var panel = new Panel($"[white on CornflowerBlue]{logo}[/]")
                .Header("")
                .Border(BoxBorder.None);

            AnsiConsole.Write(Align.Center(panel));
            AnsiConsole.Write("\n\n");
            AnsiConsole.Write(new Rule($"[yellow] PARNASSUS LAUNCHER {version} [/]").RuleStyle("grey"));
            AnsiConsole.Write("\n");

            var selectPrompt = new SelectionPrompt<string>()
                .AddChoices(mainChoices)
                .PageSize(10)
                .AddChoiceGroup("", new[] { createProfileItem, deleteProfileItem, renameProfileItem, applyTemplateItem })
                .UseConverter(name => name switch
                {
                    createProfileItem => $"[grey]{createProfileItem}[/]",
                    deleteProfileItem => $"[grey]{deleteProfileItem}[/]",
                    renameProfileItem => $"[grey]{renameProfileItem}[/]",
                    applyTemplateItem => $"[grey]{applyTemplateItem}[/]",
                    _ => name
                })
                .WrapAround();

            selectPrompt.HighlightStyle = new Style(foreground: Color.White, background: Color.Blue);

            selectedName = AnsiConsole.Prompt(selectPrompt);

            if (selectedName == separatorItem)
            {
                continue;
            }
            else if (selectedName == createProfileItem)
            {
                HandleCreateProfile();
                continue;
            }
            else if (selectedName == deleteProfileItem)
            {
                HandleDeleteProfile();
                continue;
            }
            else if (selectedName == renameProfileItem)
            {
                HandleRenameProfile();
                continue;
            }
            else if (selectedName == applyTemplateItem)
            {
                HandleApplyTemplate();
                continue;
            }
            
            return selectedName;
        }
    }

    /// <summary>
    /// Profile creation menu.
    /// </summary>
    private void HandleCreateProfile()
    {
        var blacklistManSnapshot = new BlacklistManager(blacklistMan);

        // Create

        var name = AnsiConsole.Ask<string>("[green]Enter new profile name[/] [grey](Enter '~' to cancel)[/]: ");
        if (name.Trim() == "~") return;

        var content = modsListMan.ModsList.Content?.ToDictionary(x => x, _ => true);

        var onCreate = blacklistMan.CreateBlacklist(content, name);
        if (onCreate != 0)
        {
            blacklistMan = blacklistManSnapshot;

            if (onCreate == -1)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid content for '{name}'.");
            }
            else if (onCreate == -2)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{name}' already exists.");
            }

            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }

        // Template

        var templateChoices = blacklistMan.Blacklists.Keys.ToList();
        if (templateChoices.Count == 0) return;
        templateChoices.Sort();

        const string cancelItem = "Cancel";

        var templatePrompt = new SelectionPrompt<string>()
            .Title("[green]Select a profile as template:[/]")
            .AddChoices(templateChoices)
            .AddChoiceGroup("", new[] { cancelItem })
            .UseConverter(name => name switch
            {
                cancelItem => $"[grey]{cancelItem}[/]",
                _ => name
            })
            .WrapAround();

        templatePrompt.HighlightStyle = new Style(foreground: Color.White, background: Color.Blue);
        var selectedTemplate = AnsiConsole.Prompt(templatePrompt);
        if (selectedTemplate == cancelItem)
        {
            blacklistMan = blacklistManSnapshot;
            return;
        }

        var onApplyTemplate = blacklistMan.ApplyTemplate(name, selectedTemplate);

        if (onApplyTemplate == 0)
        {
            blacklistMan.WriteToDisk();
            AnsiConsole.MarkupLine($"[green]Success: Profile '{name}' created.[/]");
            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
        else
        {
            blacklistMan = blacklistManSnapshot;
            
            if (onApplyTemplate == -1)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid content within {name} or {selectedTemplate}.");
            }
            else if (onApplyTemplate == -2)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{name}' not found.");
            }
            else if (onApplyTemplate == -3)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Template '{selectedTemplate}' not found.");
            }
            else if (onApplyTemplate == -4)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Template and target profile cannot be the same.");
            }

            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
    }


    /// <summary>
    /// Profile deletion menu.
    /// </summary>
    private void HandleDeleteProfile()
    {
        var blacklistManSnapshot = new BlacklistManager(blacklistMan);

        var deleteChoices = blacklistMan.Blacklists.Keys.ToList();
        if (deleteChoices.Count == 0) return;
        deleteChoices.Sort();

        const string cancelItem = "Cancel";

        var deletePrompt = new SelectionPrompt<string>()
            .Title("[red]Select a profile to delete:[/]")
            .AddChoices(deleteChoices)
            .AddChoiceGroup("", new[] { cancelItem })
            .UseConverter(name => name switch
            {
                cancelItem => $"[grey]{cancelItem}[/]",
                _ => name
            })
            .WrapAround();

        deletePrompt.HighlightStyle = new Style(foreground: Color.White, background: Color.Red);
        var toDelete = AnsiConsole.Prompt(deletePrompt);

        if (toDelete == cancelItem) return;

        if (!AnsiConsole.Confirm($"[red]Are you sure you want to delete '{toDelete}'?[/]")) return;
        
        var onDelete = blacklistMan.DeleteBlacklist(toDelete);

        if (onDelete == 0)
        {
            blacklistMan.WriteToDisk();
            AnsiConsole.MarkupLine($"[grey]Profile '{toDelete}' deleted.[/]");
            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
        else
        {
            blacklistMan = blacklistManSnapshot;
            
            if (onDelete == -1)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid profile '{toDelete}'.");
            }
            else if (onDelete == -2)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{toDelete}' not found.");
            }

            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
        
    }

    /// <summary>
    /// Profile renaming menu.
    /// </summary>
    private void HandleRenameProfile()
    {
        var blacklistManSnapshot = new BlacklistManager(blacklistMan);

        var renameChoices = blacklistMan.Blacklists.Keys.ToList();
        
        if (renameChoices.Count == 0) return;
        renameChoices.Sort();

        const string cancelItem = "Cancel";

        var renamePrompt = new SelectionPrompt<string>()
            .Title("[yellow]Select a profile to rename:[/]")
            .AddChoices(renameChoices)
            .AddChoiceGroup("", new[] { cancelItem })
            .UseConverter(name => name switch
            {
                cancelItem => $"[grey]{cancelItem}[/]",
                _ => name
            })
            .WrapAround();

        renamePrompt.HighlightStyle = new Style(foreground: Color.White, background: Color.Blue);

        var toRename = AnsiConsole.Prompt(renamePrompt);
        if (toRename == cancelItem) return;

        var newName = AnsiConsole.Ask<string>($"[yellow]Rename profile '{toRename}' to [/][grey](Enter '~' to cancel)[/]: ");
        if (newName.Trim() == "~") return;

        var onRename = blacklistMan.RenameBlacklist(toRename, newName);
        if (onRename == 0)
        {
            blacklistMan.WriteToDisk();
            AnsiConsole.MarkupLine($"[green]Success:[/] Profile '{toRename}' renamed to '{newName}'.");
            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
        else
        {
            blacklistMan = blacklistManSnapshot;

            if (onRename == -1)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid profile '{newName}'.");
            }
            else if (onRename == -2)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{toRename}' not found.");
            }
            else if (onRename == -3)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Profile name '{newName}' already exists.");
            }

            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
            
    }

    private void HandleApplyTemplate()
    {
        var blacklistManSnapshot = new BlacklistManager(blacklistMan);

        var templateChoices = blacklistMan.Blacklists.Keys.ToList();
        if (templateChoices.Count == 0) return;
        templateChoices.Sort();

        const string cancelItem = "Cancel";

        var templatePrompt = new SelectionPrompt<string>()
            .Title("[green]Select a profile as template:[/]")
            .AddChoices(templateChoices)
            .AddChoiceGroup("", new[] { cancelItem })
            .UseConverter(name => name switch
            {
                cancelItem => $"[grey]{cancelItem}[/]",
                _ => name
            })
            .WrapAround();

        templatePrompt.HighlightStyle = new Style(foreground: Color.White, background: Color.Blue);

        var selectedTemplate = AnsiConsole.Prompt(templatePrompt);
        if (selectedTemplate == cancelItem) return;

        var applyToChoices = blacklistMan.Blacklists.Keys.ToList();
        applyToChoices.Sort();

        var applyToPrompt = new SelectionPrompt<string>()
            .Title("[green]Select a profile to apply the template to:[/]")
            .AddChoices(applyToChoices)
            .AddChoiceGroup("", new[] { cancelItem })
            .UseConverter(name => name switch
            {
                cancelItem => $"[grey]{cancelItem}[/]",
                _ => name
            })
            .WrapAround();

        applyToPrompt.HighlightStyle = new Style(foreground: Color.White, background: Color.Blue);

        var selectedApplyTo = AnsiConsole.Prompt(applyToPrompt);
        if (selectedApplyTo == cancelItem) return;

        var onApplyTemplate = blacklistMan.ApplyTemplate(selectedApplyTo, selectedTemplate);

        if (onApplyTemplate == 0)
        {
            blacklistMan.WriteToDisk();
            AnsiConsole.MarkupLine($"[green]Success:[/] Template '{selectedTemplate}' applied to '{selectedApplyTo}'.");
            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
        else
        {
            blacklistMan = blacklistManSnapshot;

            if (onApplyTemplate == -1)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Invalid content within {selectedApplyTo} or {selectedTemplate}.");
            }
            else if (onApplyTemplate == -2)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Profile '{selectedApplyTo}' not found.");
            }
            else if (onApplyTemplate == -3)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Template '{selectedTemplate}' not found.");
            }
            else if (onApplyTemplate == -4)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Template and target profile cannot be the same.");
            }

            AnsiConsole.Ask<string>("[grey]Press ENTER to continue...[/]", "");
        }
    }

    /// <summary>
    /// Main process of launching.
    /// </summary>
    /// <returns>
    /// null: Execution completed successfully;
    /// non-null string: Error message.
    /// </returns>
    public string? Execute()
    {
        // ReadModsList
        try
        {
            modsListMan.LoadModsList();
        }
        catch (DirectoryNotFoundException)
        {
            // CreateEmptyModsList
            Directory.CreateDirectory(Path.GetDirectoryName(modsListDir) ?? string.Empty);
            File.Create(modsListDir).Close();
            modsListMan.LoadModsList();
        }

        // SyncLatestLaunchedBlacklist
        syncMan.SyncLatestLaunchedBlacklist();

        // ReadBlacklists
        try
        {
            blacklistMan.LoadBlacklistObjects();
        }
        catch (DirectoryNotFoundException)
        {
            // CreateBlacklistDirectory
            Directory.CreateDirectory(blacklistsDir);
        }

        // ScanModsDirectory
        try
        {
            modsListMan.ScanMods();
        }
        catch (DirectoryNotFoundException)
        {
            return "Internal Error: Mod directory not found.";
        }

        // RepairBlacklists
        blacklistMan.RepairBlacklists(modsListMan.ModsList);

        // RestoreDefaultProfiles
        
        blacklistMan.CreateBlacklist(modsListMan.ModsList.Content?
            .ToDictionary(x => x, _ => true), "__vanilla__", _forceOverwrite: true);
        blacklistMan.CreateBlacklist(modsListMan.ModsList.Content?
            .ToDictionary(x => x, _ => false), "__all__", _forceOverwrite: true);

        // WriteListsToDisk
        modsListMan.WriteToDisk();
        blacklistMan.WriteToDisk();

        // UserSelectProfile
        
        var selectedName = HandleUserSelection();

        // WriteIntoActualBlacklist
        File.WriteAllText(actualBlacklistDir,
            blacklistMan.Blacklists[selectedName]?.ToString());

        // RecordLaunchedProfile
        syncMan.Record(selectedName);

        // Launch
        if (string.IsNullOrEmpty(launchCommand))
        {
            return "Internal Error: Empty launch command is not allowed.";
        }
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = launchCommand,
            Arguments = "",
            UseShellExecute = true,
            CreateNoWindow = false,
            WorkingDirectory = Path.GetDirectoryName(launchCommand)
        };

        try
        {
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            return $"Internal Error: {ex.Message}";
        }

        // ShowEnabledMods
        var enabledMods = blacklistMan.Blacklists[selectedName]?.Content?
            .Where(x => !x.Value)
            .ToDictionary(x => x.Key, x => x.Value)
            .Keys.ToList();

        if (enabledMods == null || enabledMods.Count == 0)
        {
            enabledMods = new List<string> { "<Vanilla>" };
        }

        var enabledModsOutput = string.Join(Environment.NewLine,
            enabledMods);
        AnsiConsole.Markup($"Launching with the mods below enabled: \n[grey]{enabledModsOutput}[/]\n\n[pink1]Enjoy your Celeste journey![/]\n");
        AnsiConsole.MarkupLine($"[grey](Press any key in 10 seconds to check the whole list)[/]");

        // Enter a countdown which terminates the program when reaching 0.
        // Press any key to stop the countdown.
        bool keyPressed = false;
        Stopwatch sw = Stopwatch.StartNew();
        long totalSeconds = 10, remainingSeconds = totalSeconds;

        AnsiConsole.Markup($"\r{remainingSeconds:D2}");

        while (sw.ElapsedMilliseconds < totalSeconds * 1000)
        {
            long nowRemainingSecond = totalSeconds - sw.ElapsedMilliseconds / 1000;
            if (nowRemainingSecond != remainingSeconds)
            {
                remainingSeconds = nowRemainingSecond;
                AnsiConsole.Markup($"\r{remainingSeconds:D2}");
            }
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
                keyPressed = true;
                break;
            }
            Thread.Sleep(50);
        }

        if (keyPressed)
        {
            AnsiConsole.Markup("\nTimer stopped. You can now check the whole list. [grey](Press ENTER to quit)[/]");
            Console.ReadKey(true);
        }
        return null;
    }
}
