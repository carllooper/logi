# Console Deck: an Avalonia app driven by the MX Creative Console

The Logi Plugin Service owns the MX Creative Console, so no other program can open the device directly.
Console Deck gets around that with two parts:

| Part | What it does |
| --- | --- |
| **ConsoleDeckPlugin** (Logi Actions C# plugin) | Runs inside the Logi Plugin Service. Forwards every key, button, dial and roller input to the app, and draws the app's feedback (label and color) on the keypad keys. |
| **ConsoleDeckApp** (Avalonia desktop app, `ConsoleDeck.exe`) | An on-screen keypad and dialpad. Keys toggle and flash, the dial needle and arc turn, the roller bar moves, the buttons pick an accent color, and every event is logged. Clicking a tile in the window does the same as pressing the key, and the hardware key follows. |

They talk over TCP on `127.0.0.1:47800`, one JSON message per line (see `Shared/DeckMessage.cs`).
The app listens, and the plugin connects to it and reconnects every two seconds while the app is closed.
While the app is closed, the keys show "no app".

```
MX Creative Console ──► Logi Plugin Service ──► ConsoleDeckPlugin ──TCP──► ConsoleDeckApp (Avalonia)
        keys  ◄──────────────── images ◄──────────────── state ◄──────────────┘
```

## Controls

| Control | Plugin action (Options+: *Console Deck*) | In the app |
| --- | --- | --- |
| Keypad keys 1-9 | *Keypad* › Key 1 … Key 9 | Toggle on/off, each key has its own color; press count |
| Dialpad dial | *Dialpad* › Dial | 0-100, one per tick; pressing (if the control supports it) resets to 50 |
| Dialpad roller | *Dialpad* › Roller | 0-100, one per tick |
| Dialpad buttons 1-4 | *Dialpad* › Button 1 … Button 4 | Selects the accent color of the dial and roller; press count |

The keypad's two page buttons are reserved by Options+ for switching pages, so plugins never see them.

## Build and run

Prerequisites: Logi Options+ (installs the Logi Plugin Service) and the .NET 10 SDK.

```bash
dotnet build ConsoleDeck.sln
```

Building the plugin writes `ConsoleDeckPlugin.link` into the Logi Plugin Service `Plugins` folder and reloads it.
Then start the app with `dotnet run --project ConsoleDeckApp`.

### Assign the actions once in Logi Options+

1. Open the **MX Creative Keypad**, choose **Customize keys**, and select the general profile (the 2×2 dots icon at the start of the app bar).
2. Click **All actions**, open **Console Deck** › **Keypad**, and drag Key 1 … Key 9 onto the nine keys, in order.
3. Do the same for the **MX Creative Dialpad**: Dial onto the dial, Roller onto the roller, Button 1 … Button 4 onto the four buttons.

From then on the keys show the app's state, and every input appears in the app.
