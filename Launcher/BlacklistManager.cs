using System;
using System.Collections.Generic;
using System.Text;

namespace Parnassus.Launcher;

/// <summary>
/// BlacklistManager: Handles the whole blacklist library.
/// </summary>
public class BlacklistManager
{
    private readonly string blacklistsDir;
    private Dictionary<string, Blacklist?> blacklists;

    public BlacklistManager(string _blacklistsDir)
    {
        blacklistsDir = _blacklistsDir;
        blacklists = new Dictionary<string, Blacklist?>();
    }

    public Dictionary<string, Blacklist?> Blacklists
    {
        get => new Dictionary<string, Blacklist?>(blacklists);
    }

    public Blacklist? GetBlacklistByName(string _name)
    {
        return blacklists.TryGetValue(_name, out var result) ? result : null;
    }

    /// <summary>
    /// Read all blacklist files, and load all the data into the inner dictionary.
    /// </summary>
    public void LoadBlacklistObjects()
    {
        var blacklistsPaths = Directory.GetFiles(blacklistsDir, "*.txt", SearchOption.TopDirectoryOnly);
        foreach (var path in blacklistsPaths)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var modItems = new Dictionary<string, bool>();
            
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var curLine = line.Trim();
                bool blocked = true;

                if (curLine.StartsWith("#"))
                {
                    blocked = false;
                    curLine = curLine.Substring(1).TrimStart();
                }

                if (curLine.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    modItems[curLine] = blocked;
                }
            }

            Blacklist blacklistObj = new Blacklist(modItems, name);
            blacklists[name] = blacklistObj;
        }
    }

    /// <summary>
    /// Remove the invalid mods item, and supplement mod items that are missing from blacklists, 
    /// taking the given mods list as standard.
    /// </summary>
    /// <param name="_modsList">The mods list got from ModsListManager.</param>
    public void RepairBlacklists(ModsList _modsList)
    {
        if (blacklists == null || blacklists.Count == 0)
        {
            return;
        }
        RemoveDeletedMods(_modsList);
        AddMissingMods(_modsList);
    }

    /// <summary>
    /// Delete all the mod items that cannot be found in mods list in the blacklists dictionary.
    /// </summary>
    /// <param name="_modsList">The mods list got from ModsListManager.</param>
    private void RemoveDeletedMods(ModsList _modsList)
    {
        if (_modsList.Content == null)
        {
            return;
        }
        var modsHashSet = _modsList.Content.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var keys = blacklists.Keys.ToList();
        foreach (var name in keys)
        {
            var blacklist = blacklists[name];
            if (blacklist == null)
            {
                continue;
            }

            var content = blacklist.Content;
            if (content == null)
            {
                continue;
            }
            
            content = content.Where(x => modsHashSet.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
            Blacklist newList = new Blacklist(content, name);
            blacklists[name] = newList;
        }
    }

    /// <summary>
    /// Add all the mod items that is in mods list but cannot be found in blacklists in the dictionary.
    /// </summary>
    /// <param name="_modsList">The mods list got from ModsListManager.</param>
    private void AddMissingMods(ModsList _modsList)
    {
        if (_modsList.Content == null)
        {
            return;
        }
        var modsHashSet = _modsList.Content.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var keys = blacklists.Keys.ToList();
        foreach (var name in keys)
        {
            var blacklist = blacklists[name];
            if (blacklist == null)
            {
                continue;
            }

            var content = blacklist.Content;
            
            if (content == null)
            {
                continue;
            }

            var keysHashSet = content.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newItems = modsHashSet.Where(x => !keysHashSet.Contains(x))
                .ToDictionary(key => key, _ => false);
            if (newItems.Count == 0)
            {
                continue;
            }

            content = content.Union(newItems).ToDictionary(x => x.Key, x => x.Value);
            Blacklist newList = new Blacklist(content, name);
            blacklists[name] = newList;
        }
    }

    /// <summary>
    /// Create a new blacklist from a template and add it to the dictionary.
    /// </summary>
    /// <param name="_content">The content of the blacklist</param>
    /// <param name="_name">The name for the new blacklist</param>
    /// /// <param name="_templateName">The name of the template</param>
    /// <returns>
    /// -3: Creation failed: template is null;
    /// -2: Creation failed: template not found, or use itself as template;
    /// -1: Creation failed: content is null or the blacklist with the given name already exists;
    /// 0: Creation success.
    /// </returns>
    public int CreateBlacklist(Dictionary<string, bool>? _content, string _name, string? _templateName = null)
    {
        if (_content == null)
        {
            return -1;
        }
        if (blacklists.ContainsKey(_name))
        {
            return -1;
        }

        Dictionary<string, bool> result;

        if (_templateName != null)
        {
            if (!blacklists.ContainsKey(_templateName) || _name == _templateName)
            {
                return -2;
            }

            var templateContent = blacklists[_templateName]?.Content;
            if (templateContent == null)
            {
                return -3;
            }
            result = templateContent
                .UnionBy(_content, x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);
        }
        else
        {
            result = new(_content);
        }
        
        blacklists[_name] = new Blacklist(result, _name);
        return 0;
    }

    /// <summary>
    /// Delete a certain blacklist from the dictionary.
    /// This will not really delete the key-value pair from the dictionary. Instead, it'll set the value to null.
    /// </summary>
    /// <param name="_name">The name of the blacklist to delete</param>
    /// <returns>
    /// -1: Deletion failed: The target blacklist is null or the name cannot be found as a key;
    /// 0: Deletion success.
    /// </returns>
    public int DeleteBlacklist(string _name)
    {
        if (!blacklists.ContainsKey(_name) || blacklists[_name] == null)
        {
            return -1;
        }
        blacklists[_name] = null;
        return 0;
    }

    /// <summary>
    /// Rename a certain blacklist from the dictionary.
    /// </summary>
    /// <param name="_name">The name of the blacklist to rename</param>
    /// <param name="_newName">The new name for the blacklist</param>
    /// <returns>
    /// -2: Renaming failed: A blacklist with the given new name already exists;
    /// -1: Renaming failed: The target blacklist is null or the name cannot be found as a key;
    /// 0: Renaming success.
    /// </returns>
    public int RenameBlacklist(string _name, string _newName)
    {
        if (!blacklists.ContainsKey(_name) || blacklists[_name] == null)
        {
            return -1;
        }

        if (blacklists.ContainsKey(_newName))
        {
            return -2;
        }

        blacklists[_newName] = new Blacklist(blacklists[_name]);
        blacklists[_name] = null;
        return 0;
    }

    /// <summary>
    /// Read the status of all the blacklists from the dictionary and write them back to Parnassus/blacklists.
    /// If the value of a certain item is null, the original file of it will be deleted if the file exists.
    /// </summary>
    public void WriteToDisk()
    {
        var keys = blacklists.Keys.ToList();
        
        foreach (var key in keys)
        {
            var item = blacklists[key];
            var filename = Path.Combine(blacklistsDir, $"{key}.txt");

            if (item == null)
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                blacklists.Remove(key);
                continue;
            }
            File.WriteAllText(filename, item.ToString());
        }
    }
}
