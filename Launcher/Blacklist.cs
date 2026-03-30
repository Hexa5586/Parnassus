using System;
using System.Collections.Generic;
using System.Text;

namespace Parnassus.Launcher;

public class Blacklist
{
    private Dictionary<string, bool>? content;
    private string name;

    public Blacklist(Dictionary<string, bool>? _content = null, string _name = "")
    {
        this.content = _content;
        this.name = _name;
    }

    public Blacklist(Blacklist? _list)
    {
        if (_list == null)
        {
            this.content = new Dictionary<string, bool>();
            this.name = string.Empty;
            return;
        }

        this.content = _list.content;
        this.name = _list.name;
    }

    public Dictionary<string, bool>? Content { 
        get => (content == null ? null : new(content));
    }

    public string Name { get => name; }

    public override string ToString()
    {
        if (content == null || content.Count == 0)
        {
            return string.Empty;
        }

        var body = string.Join(Environment.NewLine, content
            .Select(item => { 
                if (item.Value)
                {
                    return item.Key;
                }
                else
                {
                    return $"# {item.Key}";
                }
            }));
        return body;
    }

    public void Order(bool _ascending = true)
    {
        if (_ascending)
        {
            content = content?.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value);
        }
        else
        {
            content = content?.OrderByDescending(item => item.Key).ToDictionary(item => item.Key, item => item.Value);
        }
    }

}
