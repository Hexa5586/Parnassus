using System;
using System.Collections.Generic;
using System.Text;

namespace Parnassus.Launcher;

/// <summary>
/// ModsListManager: Handles modsList.txt.
/// </summary>
public class ModsListManager
{
    private readonly string modsListDir;
    private readonly string modsDir;
    private ModsList modsList;

    public ModsListManager(string _modsListDir, string _modsDir)
    {
        modsListDir = _modsListDir;
        modsDir = _modsDir;
        modsList = new ModsList();
    }

    public ModsList ModsList { get { return modsList; } }

    /// <summary>
    /// Read the content from modsList.txt and load the items into the inner list.
    /// </summary>
    public void LoadModsList()
    {
        modsList = new ModsList(File.ReadLines(modsListDir)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList());
    }

    /// <summary>
    /// Scan the mod directory and load the items into the inner list.
    /// </summary>
    public void ScanMods()
    {
        modsList = new ModsList(Directory.GetFiles(modsDir, "*.zip", SearchOption.TopDirectoryOnly)
            .Select(x => Path.GetFileName(x))
            .ToList());
    }

    /// <summary>
    /// Write the items into modsList.txt.
    /// </summary>
    public void WriteToDisk()
    {
        File.WriteAllText(modsListDir, modsList.ToString());
    }
}
