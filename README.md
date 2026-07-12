# PomoTime

A local-first Windows focus timer built with C#, WPF, and SQLite.

## Develop with live updates

Open the repository in VS Code, select **Run and Debug**, then choose **PomoTime (Hot Reload)**. Or run:

```powershell
dotnet watch run --project PomoTime.csproj
```

Compatible C# and XAML edits are applied while the development app is running. A published executable is immutable; republish it after code changes.

## Build the test executable

```powershell
./scripts/build-exe.ps1
```

The self-contained Windows executable is written to `publish/PomoTime.exe` and does not require .NET to be installed on the test computer.

## Local data

In development, session history is stored in `src/data/pomotime.db`. Published builds use `%LOCALAPPDATA%/PomoTime/pomotime.db`. The app reads previous sessions at launch and keeps all timer history on the device.

