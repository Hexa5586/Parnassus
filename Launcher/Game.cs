using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Parnassus.Launcher;

public class Game
{
    private Blacklist profile;

    public Game()
    {
        this.profile = new Blacklist();
    }

    public Blacklist Profile
    {
        get => new Blacklist(this.profile);
        set => this.profile = new Blacklist(value);
    }
}
