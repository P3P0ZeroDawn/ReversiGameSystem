# ReversiGameSystem

ReversiGameSystem is a multiplayer desktop application built with WPF, WCF, .NET
Framework, and SQL Server. The repository contains a graphical client and an authoritative
server for accounts, sessions, social features, matches, and chat.

## Current status

The server supports registration, verified login, player and guest sessions, profiles,
leaderboards, friend presence, human-match lobbies, and match chat. Gameplay after the
lobby starts is still under construction. The server is being simplified and verified one
capability at a time before the same review begins on the client.

## Solutions

- `ReversiGameSystem.Server.slnx`: server contracts and implementation.
- `ReversiGameSystem.Client.slnx`: WPF client and client tests.

The usual server execution path is:

```text
WPF client -> WCF contract -> ServiceApi -> ApplicationService -> IRepository -> Repository
```

See [Server Structure](docs/ServerStructure.md) for the responsibility of each component.

## Requirements

- Windows with Visual Studio 2022.
- The .NET desktop development workload.
- .NET Framework 4.7.2 Developer Pack.
- SQL Server or LocalDB.

## Configuration

1. Copy `.env.example` to `.env`.
2. Set `REVERSI_DB_CONNECTION_STRING`.
3. Set the `REVERSI_MAIL_*` variables and `REVERSI_VERIFICATION_SECRET`.
4. Set `REVERSI_SERVER_HOST` to the server address when the client runs on another machine.

The `.env` file contains local secrets and must not be committed.

## Security scope

WCF transport security is disabled for the current academic deployment so that the client
and server can connect without certificates. Network traffic is not encrypted or protected
against modification. Run both applications only on a trusted development network and do
not reuse real credentials.

## Running and testing

Open the required solution in Visual Studio, restore packages, and build it. Start
`ReversiGameSystem.Server.Host` before `ReversiGameSystem.Client.Presentation`.

Server tests are located in `tests/ReversiGameSystem.Server.Tests`. Running the executable
without arguments executes the isolated suite. Database and host options require a
configured environment with disposable test data.

## Documentation

The [documentation index](docs/DocumentationIndex.md) separates current sources from
historical project material.
