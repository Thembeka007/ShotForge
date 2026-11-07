# ShotForge — Starter Scaffold

A local-first C# desktop tool for managing AI video shot lists, character
references, and cross-platform generation prompts. No server, no cloud,
no subscription to run it.

## What's actually in this scaffold

- **ShotForge.Core** — domain models: `Project`, `Scene`, `Shot`,
  `Character`, `PlatformCreditLog`. `Shot.BuildPromptText()` is a real,
  working method — it's the automation that replaces writing prompt
  blocks by hand.
- **ShotForge.Data** — EF Core `DbContext` + one repository per entity
  with working async CRUD methods (Create/Read/Update/Delete).
- **ShotForge.App** — the Avalonia desktop UI, FluentAvalonia theme
  (Windows 11-style look), MVVM via CommunityToolkit.Mvvm. Three-column
  layout: Projects → Scenes for the selected Project → Shots for the
  selected Scene, each level's Add/Delete fully wired to the database.
  Deleting a Project cascades all the way down through its Scenes and
  Shots, both in the database and in the UI selection state.
- **Copy Prompt** — the actual payoff feature: click it on any shot and
  `Shot.BuildPromptText()`'s output (description + style lock, formatted
  exactly like the shot lists you've been pasting into Kling by hand)
  goes straight to your clipboard, ready to paste.
- **LicenseService** — Free/Pro tier gate, stored locally as JSON.
  Free tier caps shots per project at 20. The `ValidateAndActivate`
  method has a `TODO` where real Gumroad license-key verification goes
  once you have a Gumroad product listing to check against.

## How to run it

1. Open `ShotForge.sln` in Visual Studio 2022 (17.9+) or JetBrains Rider.
2. Let NuGet restore all packages on first open.
3. Set `ShotForge.App` as the startup project if it isn't already.
4. Press F5 (or `dotnet run --project src/ShotForge.App`).
5. On first launch, EF Core creates the SQLite database automatically
   at `%AppData%/ShotForge/data.db` (Windows) or `~/.config/ShotForge/`
   (Mac/Linux) — there's nothing to configure manually.

## Building an Installer

This produces a real `ShotForge-win-Setup.exe` someone can download and double-click,
with no Visual Studio or .NET SDK required on their machine (the build
is self-contained - it bundles the .NET runtime itself).

**Easiest path - run the script:**

```powershell
.\build-installer.ps1 -Version 1.0.0
```

This chains the two steps below into one command and checks for common
mistakes (wrong directory, missing `vpk` tool) along the way.

**What it's doing under the hood**, if you want to run it manually or
something in the script needs adjusting:

1. **Publish a self-contained build:**
   ```powershell
   dotnet publish src\ShotForge.App\ShotForge.App.csproj -r win-x64 --self-contained -o publish -p:Version=1.0.0
   ```
2. **Install the `vpk` packaging tool** (one-time, only needed again if
   you need a different version later):
   ```powershell
   dotnet tool install -g vpk --version 1.2.0
   ```
3. **Package the installer:**
   ```powershell
   vpk pack --packId ShotForge --packVersion 1.0.0 --packDir publish --mainExe ShotForge.App.exe
   ```

The result lands in a `Releases` folder - look for `ShotForge-win-Setup.exe` in
there, which is the one file to actually share or upload.

**Releasing an update later:** bump the version number and run the
same command again (`-Version 1.0.1`, etc.). Keep the whole `Releases`
folder around between releases, including old versions - Velopack
needs the previous version's files present to generate delta updates,
so people updating from 1.0.0 to 1.0.2 don't have to redownload the
whole app.

**Important version-matching rule:** the `vpk` CLI tool's version and
the `Velopack` NuGet package version (in `ShotForge.App.csproj`) must
match. They're currently both pinned to `1.2.0`. If you ever bump
one, bump the other to match - a mismatch is one of Velopack's most
common setup errors.

## Colours and icons

**Colours** live in one place: `src/ShotForge.App/App.axaml`, in two
variants - a `Dark` and a `Light` dictionary holding the same key names
(`CardBackgroundBrush`, `AccentBrush`, ...). Windows and controls refer
to the names, never raw hex codes, so a new scheme is an edit to those
two blocks. Light mode is a soft warm grey (`#EDEBE8` window), never
pure white.

**Theme toggle:** bottom-left of the main window. The choice is
applied at startup (`App.axaml.cs`) and remembered in
`%AppData%\ShotForge\settings.json` (`ThemeService`). Defaults to Dark.

**Icon** is a front-facing camera (lens looking at the viewer), exactly centred and symmetrical, inside viewfinder corners (amber on
near-black). It is wired in three places:
- `assets\ShotForge.ico` - picked up automatically by `build-installer.ps1`
  for the installer and shortcuts.
- `src\ShotForge.App\Assets\ShotForge.ico` - the exe icon
  (`<ApplicationIcon>` in the csproj) and the icon on both windows
  (`Icon="/Assets/ShotForge.ico"`), bundled via `<AvaloniaResource>`.
- `assets\ShotForge_icon_1024.png` - full-size artwork for a Gumroad
  cover/thumbnail.
The .ico holds seven sizes (16-256 px); the 16-32 px versions are
simplified and bolder on purpose so they stay readable.

**Accent colour:** `CustomAccentColor="#FF9F1C"` on the theme element in
`App.axaml` turns the theme's own highlights (list selection bar, etc.)
amber. If a build ever complains about that attribute, delete it - the
rest of the palette still works without it.

## Licensing (Free vs Pro)

- **Free:** 1 project. **Pro:** unlimited projects. (`LicenseConfig.FreeProjectLimit`)
- Existing projects are never deleted: a Free user who already has several
  keeps them, they just can't add more until they unlock Pro.
- **Unlock Pro** button (top right, Free only) opens a window where the buyer
  pastes the licence key from their Gumroad receipt. The key is verified with
  Gumroad once (this counts as one activation; up to `MaxActivations` = 5, so
  reinstalling or a second PC is fine but wide sharing is not).
- After activation the app works fully **offline**. About once a week, when
  online, it quietly re-checks the key *without* using an activation; if the
  key was refunded/charged back/disabled the app returns to Free. Being
  offline never locks a paying customer out.
- Honest limit: any local licence check stops casual sharing, not a
  determined cracker. That is the right trade-off for a one-time-price tool.

**To switch it on when the Gumroad product exists** (the ONLY code to edit is
`src/ShotForge.App/Services/LicenseConfig.cs`):
1. In Gumroad, create the product and turn on **"Generate a unique license
   key per sale"** (product settings).
2. Copy the **product ID** shown there into `GumroadProductId`.
3. Copy the public product page URL into `PurchaseUrl` (the Buy Pro button).
4. Rebuild the installer with a new version number.

Until `GumroadProductId` is filled in, the Unlock window says licensing isn't
set up yet - nobody can accidentally get Pro for free.

**Testing Pro during development:** only works in a **Debug** build (what
`dotnet run` and Visual Studio's Start button produce - NOT the installer).
Simplest way, from the repo folder in Command Prompt:
```
dotnet run --project src/ShotForge.App -- --force-pro
```
(Visual Studio: project Debug Properties > "Command line arguments" box, type
`--force-pro`, then use the Start button.) The window title tells you what is
running: `[Debug build - Pro override ON]` = working; `override off` = the
argument didn't reach the app; no bracket at all = a Release build (the
override is compiled out by design). The badge shows plain `PRO`.
The environment variable `SHOTFORGE_FORCE_PRO=1` (set on its own line before
`dotnet run`) also works but is easier to get wrong. To test the real Gumroad flow instead, make a test
purchase of your own product and paste the key it gives you.

## How the copied prompt is built

Copy Prompt joins up to five parts, separated by blank lines:
1. the shot **description** (what happens),
2. `Duration: 5 seconds` (checkbox "Include shot duration", on by default),
3. `Characters: Kai - <description>; Axiom - <description>` for the characters
   tagged on the shot (checkbox "Include character descriptions", on by default),
4. `Negative prompt: ...` - the negative prompt, **only** if "Put negative prompt inside
   the prompt" is ticked (off by default),
5. the **Style** line (only if one was entered).

Tools differ, so the two checkboxes (bottom-left of the main window, remembered
between launches) let each user match their tool:
- Negative prompt: tick "Put negative prompt inside the prompt" to have it copied
  as a `Negative prompt: ...` line. Left unticked, it is still shown on each shot card and
  in exports, but Copy Prompt does not include it.
- Duration: turn it off for tools that have their own duration setting.
- Character descriptions: handy for tools that only take text; optional extra for
  tools that take reference images.

The same two settings drive the `.txt` and `.json` exports, so an export shows
what you would actually paste. If the negative prompt is kept separate, the
export still lists it on its own line under each shot so nothing is lost.

The Style box in the Shot form starts empty (no hidden text), is saved with the
shot, and carries over to the next shot in the same scene.

## What this was NOT run through

I don't have a .NET SDK or internet access in the sandbox this was
written in, so **none of this has actually been compiled**. Everything
here follows standard, current EF Core 8 / Avalonia 11 / CommunityToolkit
patterns, but the first time you build it, treat any compiler error as
genuinely useful signal, not a sign something is fundamentally wrong —
it's most likely a small thing like a NuGet version mismatch or a missing
`using`.

Two real bugs were caught and fixed while reviewing before packaging:
1. The project list's `DataTemplate` wasn't bound to the `Project` type,
   so names wouldn't have displayed and Delete wouldn't have worked.
2. `ShotForge.App.csproj` referenced `app.manifest`, which didn't exist
   yet — this would have failed the build immediately.

## What's genuinely NOT built yet (by design, not oversight)

- Real-world testing of the Gumroad licence flow (code is written and follows
  Gumroad's documented API, but it needs your live product ID to be exercised).
- Any packaging/installer step - this is still source code you run
  from Visual Studio, not a file a customer could download and
  double-click.
- Reordering Scenes/Shots (changing SceneNumber/ShotNumber after
  creation) - Edit changes a row's content, not its position in the
  list.

## Suggested build order from here

1. ~~Get it compiling and running as-is first.~~ Done, confirmed to
   persist across app restarts.
2. ~~Add a Scene list view nested under a selected Project.~~ Done.
3. ~~Add a Shot list view nested under a selected Scene.~~ Done,
   including the Copy Prompt button using `BuildPromptText()`.
4. ~~Add the "export project as .txt" command.~~ Done, plus a JSON
   format option alongside it.
5. ~~Reference images.~~ Done — "Manage Characters" opens a standalone
   window per project: add a character with a name, standing
   description, and picked reference image (copied into ShotForge's
   own local storage, not just referenced at its original location),
   with thumbnails shown in the roster.
6. ~~Wire Characters into shot creation, add negative prompt field.~~
   Done — the Shot form has a negative-prompt textbox and a checkbox
   list of the current project's characters; both are included in
   `BuildPromptText()` and the export, and each shot's card shows its
   attached characters as tags plus its negative prompt when present,
   so both fields are visually confirmable right after adding a shot.
7. ~~Edit capability for Project, Scene, Shot, and Character.~~ Done —
   each row now has an Edit button alongside Delete, which loads that
   item back into the same Add form (now labeled "Update ...") with a
   Cancel option. Editing is reference-safe: switching the selected
   Project/Scene mid-edit, or deleting the very row being edited,
   correctly cancels the in-progress edit rather than leaving stale
   state. Shot editing goes through `UpdateDetailsAsync` specifically
   (not the generic `UpdateAsync`) because Characters is a many-to-many
   relationship that needs proper change-tracking to re-diff safely.
8. ~~Packaging a real installer (Velopack).~~ Done — see the
   "Building an Installer" section below. `VelopackApp.Build().Run()`
   is wired into `Program.cs`, the `Velopack` package is referenced,
   and `build-installer.ps1` at the repo root chains the full
   `dotnet publish` → `vpk pack` sequence into one command. **This has
   not actually been run** - I don't have a .NET SDK or internet
   access in the sandbox this was built in, so the script is correct
   per Velopack's current documented API and command syntax, but the
   first real run of it is genuinely untested. Treat any error here as
   useful signal, same as the first build of the app itself was.
9. ~~Gumroad license check.~~ Written - see "Licensing" above. Needs your
   Gumroad product ID in `LicenseConfig.cs` and a real test purchase.
