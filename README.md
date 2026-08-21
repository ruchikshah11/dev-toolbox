# DevToolbox

A single-exe Windows desktop toolbox of 60+ everyday developer utilities - formatters,
validators, converters, encoders, string escapers, reference lookups, and a set of
SharePoint-specific tools - all in one app with no installer and no loose DLLs.

## What it does

Pick a tool from the sidebar (or press **Ctrl+K** and start typing to search) and it loads
into the content area on the right. Every tool is self-contained: paste or type input, see the
result live or via a button, copy it back out.

## Tool categories

| Category | Examples |
|---|---|
| Formatters | XML, JSON, HTML, SQL |
| Validators | XML/JSON/HTML validators, XPath Tester, Regex Testers (.NET and Java), Cron Expression Generator, Credit Card Generator & Validator |
| Converters | XSD Generator, XSLT Transformer, XML/JSON/CSV/YAML conversions, Epoch Timestamp, **Timezone Converter**, Color Converter/Picker, **Number Base Converter** |
| PDF Tools | PDF Password Remover, Word to PDF, PDF to Word, PDF to Markdown, **Merge PDFs**, **Split PDF**, **Rotate PDF Pages**, **Add Page Numbers**, **Add/Remove Watermark**, **Protect PDF (Add Password)**, **Compress PDF** |
| Code Runner | Runs code with a locally-installed PowerShell, Python, Node.js, cmd.exe, Java, R, or GCC/G++ (C/C++) toolchain, or opens HTML in your browser - no bundled compiler/runtime |
| Encoders / Cryptography | URL, Base64, **Base32**, file encoding conversion, Message Digester (MD5/SHA-256/SHA-512), HMAC Generator, JWT Decoder/**Encoder**, **Certificate Decoder**, QR Code, GUID, Password Generator, **Compress Image** |
| Code Minifiers / Beautifier | JS and CSS |
| String Escaper & Utilities | String Utilities (case conversion incl. camelCase/PascalCase/snake_case/kebab-case/**URL Slug**, stats, etc.), HTML/XML/Java/.NET/JavaScript/JSON/CSV/SQL escaping, Diff Viewer |
| Web Resources | Lorem Ipsum, HTML Viewer, Markdown Previewer, MIME Types, HTML Entities, URL Parser, I18N/Locale Codes, HTTP Status Codes |
| SharePoint | Internal Name Encoder/Decoder, Claims Identity Encoder/Decoder, CAML Query Formatter, REST API Query Reference |

The in-app **Documentation** button (top right) lists every tool with a fuller description and
a clickable table of contents - open that for the complete, current list rather than relying on
this README staying in sync with every future addition.

## Features

- **Theme: Light / Dark / System** - chosen from the sliders-icon **Settings** dialog (top
  right), applied instantly across the whole app, and remembered across restarts. System follows
  Windows' own light/dark app setting (Settings > Personalization > Colors on Windows 10/11).
- **Settings dialog** (sliders icon, top right) also controls:
  - **Remember last-opened tool** - reopens to whichever tool you last had selected; when off,
    always opens the **Default landing tool** below instead
  - **Default landing tool** - which tool opens on first launch, or whenever there's no
    remembered tool to fall back to
  - **Start maximized** - always open the window maximized
  - **Ctrl+K search scope** - whether search also matches tool descriptions, not just names
  - **Password Generator**: clipboard auto-clear delay (default 30s, 0 = off), history entry
    limit (default 50, 0 = off), and max password length (default 99, hard cap 128)
  - **Reset to Defaults** - restores every setting above to its factory default (after a
    confirmation prompt) without touching your saved Password Generator history
- **Ctrl+K search** across all tools by name (and descriptions, per the setting above)
- **Pin favorites** - click the star next to any tool to pin it to its own section at the top
  of the sidebar; pins persist across restarts
- **Color Picker** can sample a color directly off a loaded image or anywhere on your screen
  (eyedropper), not just from the gradient/hue picker - plus a **Harmony** dropdown
  (Complementary, Analogous, Triadic, Split-Complementary, Tetradic) that generates related
  colors from the current hue as a clickable swatch strip
- **Password Generator history** - explicitly-generated values (not every live options tweak),
  encrypted at rest with Windows DPAPI tied to your login
  (`%LocalAppData%\DevToolbox\password-history.dat`) - separate from the app's other settings,
  since it holds actual secrets rather than preferences. Copying a password/passphrase also
  auto-clears the clipboard after a delay, per the Settings option above.
- Every other tool is deliberately stateless - no other settings are saved anywhere

## Requirements

- Windows

## Running it

Two ways to get it onto a machine:

- **Just the exe**: download `DevToolbox.exe` and run it - that's the whole distribution. Every
  dependency (Newtonsoft.Json, YamlDotNet, NUglify, QRCoder, HtmlAgilityPack, Markdig) is
  embedded in the exe itself via Costura.Fody, so there are no DLLs to ship alongside it and
  nothing to install.
- **Installer**: hand out `DevToolboxSetup.exe` instead (see "Building an installer" below) for
  a Start Menu shortcut, an optional desktop icon, and a proper uninstaller listed in Windows'
  Add/Remove Programs. It's a per-user install - no admin rights or UAC prompt needed.

## Building from source

Requires the .NET SDK (targets classic .NET Framework 4.7.2, built via the modern SDK-style
tooling).

```
dotnet build -c Release
```

Output: `bin\Release\DevToolbox.exe` - self-contained, single file.

## Building an installer

`Installer\DevToolbox.iss` is an [Inno Setup](https://jrsoftware.org/isinfo.php) script that
wraps the Release exe into `DevToolboxSetup.exe` - a real installer with a Start Menu shortcut,
an optional desktop icon (unchecked by default), and an uninstaller, installed per-user under
`%LocalAppData%\Programs\DevToolbox` (no admin rights required).

1. `dotnet build -c Release` first, so `bin\Release\DevToolbox.exe` is up to date.
2. Install Inno Setup if it isn't already (`winget install JRSoftware.InnoSetup`).
3. Compile the script:
   ```
   ISCC.exe Installer\DevToolbox.iss
   ```
   (`ISCC.exe` is wherever Inno Setup installed it - typically
   `%LocalAppData%\Programs\Inno Setup 6\ISCC.exe` or
   `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` - or open the `.iss` file in the Inno Setup IDE
   and press Compile.)

Output: `Installer\Output\DevToolboxSetup.exe`.

Bump `MyAppVersion` at the top of the `.iss` file before cutting a new installer - `AppId` is a
fixed GUID so re-running a newer installer upgrades an existing install in place rather than
creating a duplicate Add/Remove Programs entry.

## Project structure

```
Core/       ITool, ToolRegistry (the master list of every tool), ToolHighlights (Documentation
            page copy), AppSettings (all persisted preferences - theme, pins, last/default
            tool), category icon catalog
Tools/      One folder per tool - typically a <Name>Tool.cs (registers it), a
            <Name>Service.cs (the actual logic, no UI dependency), and a <Name>Control.cs
            (the WinForms UI), though simple text-in/text-out tools often just supply
            transform functions to the shared TextTransformControl instead of a custom UI
UI/         MainForm (sidebar/search/header shell), SettingsForm (dark mode, remember-last-tool,
            default landing tool), Theme (colors/fonts), BufferedPanel (double-buffered Panel,
            used where content swaps at runtime to avoid flicker), and shared building blocks
            used across tools: CardPanel, TextTransformControl, ReferenceTableControl
            (searchable lookup tables), CategoryIcons
Assets/     Application icon and the embedded SharePoint category icon
Program.cs  Entry point
```

## Adding a new tool

1. Create `Tools/<Name>/` with a `<Name>Tool.cs` implementing `ITool` (`Category`, `Name`,
   `Description`, `CreateView()`).
2. If it's a simple text-in/text-out transform, point `CreateView()` at a
   `TextTransformControl` with your transform functions (see `Base32EncoderTool` for the
   simplest example). Otherwise build a dedicated `<Name>Control.cs` (see `JwtEncoderTool` /
   `TimezoneConverterTool` for richer examples).
3. Register the new `Tool` instance in `Core/ToolRegistry.cs`, under the right category
   comment block.
4. Optionally add a fuller entry to `Core/ToolHighlights.cs` for the Documentation page - falls
   back to the plain `Description` above if you skip this.

## Why these specific technical choices

- **.NET Framework 4.7.2, not .NET 8**: keeps the WinForms/Costura.Fody single-exe packaging
  approach working exactly as it does across this project's sibling tools.
- **Costura.Fody** embeds every dependency DLL into `DevToolbox.exe` as a resource (unpacked at
  runtime), so distribution is a single exe file.
- **App-level preferences (dark mode, pinned tools, last-opened tool, remember-last-tool,
  default landing tool) live in one shared `AppSettings` instance**, persisted to
  `%LocalAppData%\DevToolbox\settings.json`. `AppSettings.Load()` caches a single object rather
  than re-reading the file per call - `Theme` and `MainForm` both mutate and save that same
  instance, so toggling the theme can't silently clobber an unsaved pin change (or vice versa)
  the way two independently-loaded copies would. Every other tool in the app is intentionally
  stateless between launches.
- **Password Generator history is DPAPI-encrypted, in its own file, not in `settings.json`**:
  that file holds real secrets (the generated passwords/passphrases themselves), unlike the
  other preferences, so it's encrypted at rest via `System.Security.Cryptography.ProtectedData`
  (`DataProtectionScope.CurrentUser`) and kept separate so a corrupt/shared settings file can't
  expose it.
- **The screen eyedropper takes one screenshot up front rather than a live global mouse hook**:
  a `ScreenColorPickerForm` covering the whole virtual desktop displays that frozen screenshot as
  its own background and samples pixels from it locally - avoids needing low-level Win32 hooking
  just to track the cursor outside the app's own window.
- **HTML Viewer / Markdown Previewer use the legacy `WebBrowser` control, not WebView2**: WebView2
  was tried (it's Chromium-based and properly DPI-aware) but its native `WebView2Loader.dll`
  couldn't be made to travel inside the single portable exe via Costura, so it was reverted.
  Tradeoff: wide preview content can occasionally run past the pane's edge under certain DPI
  scaling, since `WebBrowser` (IE/Trident) isn't itself per-monitor DPI aware.
- **`DpiAwareness=System` (not `PerMonitorV2`) in `App.config`**: fixes blurry/misread UI text on
  scaled displays (the original bug) without the `WebBrowser` control desyncing its layout width
  from its container, which is what `PerMonitorV2` caused.
- **System theme detection reads the registry directly**
  (`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`) rather
  than a public WinForms API, since .NET Framework 4.7.2 has no built-in accessor for Windows'
  light/dark app-theme setting. Read once when System mode is (re-)selected, not watched live -
  changing your Windows theme while the app is already running won't flip it until you reopen
  Settings or restart the app.
- **Code Runner shells out to locally-installed interpreters/compilers rather than bundling a
  scripting engine**: it runs PowerShell (`pwsh.exe`, falling back to Windows PowerShell),
  Python, Node.js, cmd.exe/Batch, Java (JDK 11+'s single-file source-launcher, no separate
  `javac` step), R (`Rscript.exe`), and C/C++ (via `gcc`/`g++` - MSVC's `cl.exe` isn't supported,
  since it needs a Visual Studio developer environment rather than a plain PATH executable) -
  whichever of those it can actually find on this machine, with no sandboxing. C/C++ compile
  first (their own captured build output and timeout) and only run the resulting `.exe` if that
  succeeds; HTML just opens in your default browser instead of being executed as a process.
  Every run is killed if it exceeds its configurable timeout - but .NET Framework 4.7.2's
  `Process.Kill()` has no "kill entire process tree" option (added later, in .NET Core 3.0+), so
  a timed-out script that spawned its own child processes can leave those still running.
