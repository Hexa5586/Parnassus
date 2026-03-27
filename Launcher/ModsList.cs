using System;
using System.Collections.Generic;
using System.Text;

namespace Parnassus.Launcher;

public class ModsList
{
    private List<string>? content;

    public ModsList(List<string>? _content = null)
    {
        this.content = _content;
    }

    public List<string>? Content
    { 
        get => (content == null ? null : new(content));
    }

    public override string ToString()
    {
        if (content == null || content.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(Environment.NewLine, content);
    }
}
