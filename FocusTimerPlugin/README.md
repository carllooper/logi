# Focus Timer: a Logi Actions C# SDK demo plugin

A small but complete plugin for the [Logi Actions SDK (C#)](https://logitech.github.io/actions-sdk-docs/csharp/plugin-development/introduction/).
It is a Pomodoro-style focus timer that shows the main kinds of action the SDK offers:

| Action | SDK type | What it does |
| --- | --- | --- |
| **Start / Pause** | `PluginDynamicCommand` with a custom image | Press to start or pause. The button draws the live `mm:ss` countdown with `BitmapBuilder` on a green (running), grey (paused) or red (finished) background, and redraws every second through `ActionImageChanged()`. |
| **5 / 15 / 25 / 50 min session** | `PluginDynamicCommand` with parameters | One class registers four actions with `AddParameter`. Pressing one starts a fresh session of that length. They sit in a nested `Focus Timer###Presets` group. |
| **Session Length** | `PluginDynamicAdjustment` with `hasReset: true` | Turn the dial to change the length (1 to 90 min, one minute per tick). Press the dial to stop the timer and go back to 25 min. The value is shown next to the dial. |

It also uses plugin settings (`TryGetPluginSetting` / `SetPluginSetting`) to remember the session length, and `PluginLog` for logging.

## Layout

```
FocusTimerPlugin/
├── FocusTimerPlugin.sln
├── .vscode/                     launch.json (attach debugger), tasks.json (build/pack/install)
└── src/
    ├── FocusTimerPlugin.csproj  references PluginApi.dll from the Logi Plugin Service install
    ├── FocusTimerPlugin.cs      Plugin class: owns the shared timer, persists the duration
    ├── FocusTimerApplication.cs ClientApplication (unused, this is a universal plugin)
    ├── Services/FocusTimer.cs   thread-safe countdown, no SDK dependency
    ├── Actions/
    │   ├── StartPauseCommand.cs
    │   ├── PresetCommand.cs
    │   └── DurationAdjustment.cs
    ├── Helpers/                 PluginLog / PluginResources, same as the SDK template
    └── package/metadata/        LoupedeckPackage.yaml, Icon256x256.png
```

The project layout, `.csproj`, helpers and `.vscode` files follow the template that `logiplugintool generate` produces
(taken from Logitech's [actions-sdk](https://github.com/Logitech/actions-sdk) samples).

## Build and run

Prerequisites, from the docs: Logi Options+ or Loupedeck (this installs the Logi Plugin Service) and the .NET 8 SDK.

```bash
dotnet tool install --global LogiPluginTool   # only needed for packaging
cd FocusTimerPlugin
dotnet build
```

The build writes a `FocusTimerPlugin.link` file into the Logi Plugin Service `Plugins` folder and asks the service to reload the plugin.
Then:

- **Options+:** open the customization view for the Actions Ring, MX Creative Keypad or MX Creative Dialpad, go to *All Actions*, and look for **Focus Timer** under *Installed Plugins*. If it is missing, choose *Restart Logi Plugin Service* in the Options+ settings.
- **Loupedeck:** unhide **Focus Timer** on the *Show and hide plugins* tab, then drag the actions onto buttons and dials.

For hot reload while editing, run `dotnet watch build` in `src/`.

## Package

```bash
dotnet build -c Release
logiplugintool pack ./bin/Release ./FocusTimer.lplug4
logiplugintool install ./FocusTimer.lplug4
```

## Notes

- Turning the dial while the timer runs changes the length of the *next* session. To apply a new length right away, press the dial or use a preset.
- `PluginApi.dll` is not on NuGet. The project references it from `C:\Program Files\Logi\LogiPluginService\` (Windows) or `/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/` (macOS), so you need the Logi Plugin Service installed to build.
