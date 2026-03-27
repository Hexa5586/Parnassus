# Parnassus (Everest Blacklist Manager)

## Idea

I love diving into different Collabs at the same time, but the massive amount of mods and dependencies makes Everest take forever to load, which is a real headache. So, I built this lightweight CLI tool to help me partition my mods into different profiles and pick exactly what I want to load when I start the game.

## Usage

1. Download `Parnassus.Launcher.exe` from below *(Gamebanana)* or from Release *(Github)*.
1. Put `Parnassus.Launcher.exe` in your Celeste root directory.
1. Open your Steam library and right-click on Celeste, then open Properties.
1. Go to General -> Launch Options, set it to `path/to/parnassus/launcher %command%`. *(e.g. `"D:\SteamLibrary\steamapps\common\Celeste\Parnassus.Launcher.exe" %command%`)*.
1. Launch Celeste, and you're able to use it! (Of course you need Everest installed).

## Functions

* **Profile**: A custom-defined collection of mod states (enabled or disabled) within Parnassus, allowing you to switch between different gameplay environments quickly.
* **Template**: An existing profile used as a initial base for creating a new one, ensuring that certain "dependency" mods remain enabled by default. When creating a new profile you can select an existing profile as the template.

## P.S.

* Although this program don't modify your SAVES, this application is **UNSTABLE** now. **Make sure you have backed up your saves if you want to use Parnassus**.
* I'm just a college student who is not good at programming, and I probably won't have the ability to treat with Pull Requests *(I don't think who would send Pull Request to this project XD)*. If you want to make it better, just fork and modify this project by yourself.