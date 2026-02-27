# Battleship MAUI

Desktop Battleship game built with .NET MAUI.

## Current Release

- `v1.3.0`
- Command-center board-first UI with top-bar controls
- Animated board-view transitions (`Fire Control` / `Fleet Ops`)
- Right-click ship orientation during placement
- First-launch command briefing overlay

## Quick Start

1. Clone the repo.
2. Install .NET 10 SDK and MAUI workload.
3. Run `dotnet build BattleshipMaui.sln`.
4. Run the app from Visual Studio or `dotnet run`.

## Publish Locally

```powershell
dotnet publish BattleshipMaui.csproj -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained false
```

Launch:
`bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\BattleshipMaui.exe`

## Current CI Scope

GitHub Actions runs the `Category=Core9` test subset (9 tests) on each push and pull request.
