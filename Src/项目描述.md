# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

《极世界》(Extreme World) — an MMORPG built with a Unity client and .NET Framework 4.8 game server, communicating via protobuf-net over TCP. All code is C#.

## Repository Structure

```
Src/
├── Client/          Unity 2022+ game client
├── Server/          .NET Framework 4.8 game server
├── Lib/             Shared libraries (Common + Protocol) used by both client and server
├── Data/            Game data: Excel spreadsheets (Tables/) → JSON (Data/)
```

## Build & Run

**Server:** Open `Server/GameServer/GameServer.sln` in Visual Studio. Build and run. Server listens on `127.0.0.1:8000` (configured in `App.config`). Requires SQL Server instance `ZHY\MMORPG` with database `ExtremeWorld` (Entity Framework 6).

**Client:** Open `Client/` in Unity 2022+. The client solution is at `Client/Client.sln`.

**Data pipeline:** Run `Data/Excel2Json.cmd` to convert Excel tables in `Data/Tables/` to JSON `.txt` files in `Data/Data/`, then copies them to both `Client/Data/` and `Server/GameServer/GameServer/bin/Debug/Data/`.

## Architecture

### Three-Layer Structure

- **Lib/Common/** — Shared code: data definitions (`*Define.cs`), network message distribution (`MessageDistributer`, `PackageHandler`), `Singleton<T>` base class, utilities
- **Lib/Protocol/message.cs** — Auto-generated protobuf message definitions (~70KB), the contract between client and server
- **Server** and **Client** both reference Lib

### Server Architecture (Service → Manager pattern)

**Services** (`Server/GameServer/GameServer/Services/`) handle network messages by subscribing to message types in their constructors via `MessageDistributer`. Each service is a `Singleton<T>`. Key services: `UserService`, `MapService`, `ItemService`, `QuestService`, `FriendService`, `TeamService`, `GuildService`, `ChatService`, `BagService`.

**Managers** (`Server/GameServer/GameServer/Managers/`) contain game logic, also singletons. Key managers: `MapManager` (ticked every 100ms in the game loop), `CharacterManager`, `MonsterManager`, `SpawnManager`, `EntityManager`.

**DBService** wraps Entity Framework 6 with `ExtremeWorldEntities` context. Database models live in `Entities/`.

**Game loop:** `GameServer.Update()` runs on a background thread at ~10 ticks/second (100ms sleep), calling `MapManager.Update()`.

### Client Architecture

**Managers** (`Client/Assets/Scripts/Managers/`) mirror server-side functionality: `CharacterManager`, `BagManager`, `ChatManager`, `QuestManager`, `EntityManager`, `EquipManager`, `FriendManager`, `GuildManager`, `TeamManager`, `ItemManager`, `NPCManager`, `DataManager`, `InputManager`.

**Services** (`Client/Assets/Scripts/Services/`) handle server communication.

**GameObject controllers** (`Client/Assets/Scripts/GameObject/`) — `EntityController`, `PlayerInputController`, `MainPlayerCamera`, `MapController`, `NpcController`, `RideController`.

**UI** (`Client/Assets/Scripts/UI/`) — UI panels and widgets.

### Communication Flow

Client sends protobuf request → Server Service receives via `MessageDistributer` subscription → Service processes using Managers → Service sends protobuf response back to client.

### Data System

Game data is defined in Excel spreadsheets, converted to JSON via `json-excel` tool, then deserialized into `Dictionary` collections by `DataManager.Load()` on both client and server. Define classes: `CharacterDefine`, `ItemDefine`, `EquipDefine`, `QuestDefine`, `MapDefine`, `NpcDefine`, `RideDefine`, `ShopDefine`, `SpawnRuleDefine`, `SkillDefine`, `BuffDefine`.

## Key Conventions

- All major services and managers inherit from `Singleton<T>` (defined in `Lib/Common/Singleton.cs`)
- Server services subscribe to network messages in their constructors
- Protocol messages are defined via protobuf-net attributes in `Lib/Protocol/message.cs` — do not edit manually
- Chinese comments are used throughout the codebase
- Git branch `MMO` is the active development branch

## Safety & Workflow Rules (must follow)

- Default to **read-only exploration**. Before any edits, first provide:
  1) a short plan, 2) list of files to change, 3) risks, 4) how to verify.  
  Do not apply changes until I confirm.

- Avoid large refactors, broad renames, and repository-wide formatting unless explicitly requested.

- **Do not modify protocol contracts casually.**
  If any network message / field / enum changes are required, ensure **Client + Server** stay consistent and maintain backward compatibility.

- **Do NOT edit auto-generated or IDE noise files**, including:
  - `**/Logs/**`, `**/*.log`
  - Unity/VS layout/cache/user settings files (e.g., `*.csproj.user`, `.vs/`, etc.)
  - `开发日志/` (unless explicitly requested)

## Git Safety Rules (must follow)

- Do NOT run git commands that change history or remote state unless explicitly asked.
  This includes: `git commit`, `git push`, `git reset`, `git rebase`, `git merge`, `git clean`, `git checkout -f`.

- Before proposing any commit:
  1) show `git status` and `git diff --stat`,
  2) summarize changes by category (code vs noise),
  3) wait for my confirmation.

- Never stage/commit auto-generated or IDE noise files (Logs, *.log, *.csproj.user, .vs/, Unity layout/cache, etc.).
- Prefer working on a new branch for each task; keep changes minimal and reversible.

## Verification Expectations

- After server changes: ensure the server project builds successfully (no new compile errors).
- After client changes: ensure Unity C# scripts compile (no new compile errors).
- After data/protocol changes: run the data pipeline and ensure both sides load data/messages correctly.
