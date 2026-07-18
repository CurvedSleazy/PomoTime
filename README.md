# PomoTime

A local-first Windows focus timer built with C#, WPF, and SQLite.

## v1.1 features

- Dark, light, and custom RGB accent themes
- CSV and JSON session-history export
- Alarm audio stored directly in the local SQLite database
- User-defined focus tags such as Study and Work
- Optional automatic break timer after completed focus sessions
- Completion notification pop-ups
- Local ambient playlist with imported audio stored in SQLite
- Activity calendar with completed-day indicators

## Develop with live updates

Open the repository in VS Code, select **Run and Debug**, then choose **PomoTime (Hot Reload)**. Or run:

```powershell
dotnet watch run --project PomoTime.csproj
```

To create a one-click local launcher that uses Microsoft's trusted .NET host:

```powershell
./scripts/create-dev-launcher.ps1
```

Open `PomoTime Dev.lnk` from the repository whenever you want to test the latest source with Hot Reload. This is a development launcher, not the distributable application.

Compatible C# and XAML edits are applied while the development app is running. A published executable is immutable; republish it after code changes.

## Build the test executable

```powershell
./scripts/build-exe.ps1
```

The self-contained Windows executable is written to `publish/PomoTime.exe` and does not require .NET to be installed on the test computer.

On enterprise-managed Windows devices, Code Integrity may require the executable to be signed. Configure an approved certificate before building:

```powershell
$env:POMOTIME_SIGNING_THUMBPRINT = 'CERTIFICATE_THUMBPRINT_FROM_YOUR_ORGANIZATION'
./scripts/build-exe.ps1
```

The build script signs with SHA-256, applies a trusted timestamp, verifies the signature, and warns when the output remains unsigned. A self-signed certificate may still be rejected by enterprise policy; use a certificate trusted by that policy or request an allow-list entry from the device administrator.

## Local data

Session history is stored outside the repository at `%LOCALAPPDATA%/PomoTime/pomotime.db`. The app reads previous sessions at launch and keeps all timer history on the device. Older development databases under `src/data` are merged automatically when encountered.

## Project structure

```text
PomoTime/
├── UI/
│   ├── Views/       Application windows and their event coordination
│   └── Controls/    Reusable timer ring and activity calendar controls
├── Data/            SQLite persistence and queries
├── Models/          Application data types
├── Services/        Reusable system behavior such as alarm playback
├── scripts/         Build and packaging automation
├── App.xaml         Application resources and startup
└── PomoTime.csproj  .NET project configuration
```
