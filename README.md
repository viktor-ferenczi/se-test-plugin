# Test Plugin for Pulsar and Magnetar

## Prerequisites

- [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/)
- [Python 3.12](https://python.org) (requires 3.12 or newer)
- [Pulsar](https://github.com/SpaceGT/Pulsar) — plugin loader for Space Engineers (game client)
- [Magnetar](https://magnetar.se) — the Space Engineers server with plugin support
- [.NET Framework 4.8.1 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481) and 
  [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

## Usage

This is a test plugin for internal development purposes.

- Developers: Use it for examples
- Players: Leave it alone

## Building

`dotnet build TestPlugin.sln` finds the game, the Dedicated Server and Magnetar's `PluginSdk.dll`
on its own. If it does not, run `setup.py` or set the folders in `Directory.Build.props.user`, see
`Directory.Build.props` for the list.

To test a working copy, load it through a loader development folder: start Pulsar or Magnetar with
`-sources` and add the repository with the Sources button. Builds deploy only if `Pulsar` or
`MagnetarData` is set in `Directory.Build.props.user`, or passed as `-p:Pulsar=...` or
`-p:MagnetarData=...`.
