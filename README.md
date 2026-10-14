# Chaotic Cupid (Haotični Kupidon)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)
![SignalR](https://img.shields.io/badge/ASP.NET%20Core-SignalR-5C2D91)
![Architecture](https://img.shields.io/badge/pattern-publish%2Fsubscribe-orange)

A real-time **publish/subscribe** matchmaking system built with **ASP.NET Core SignalR**. Clients register over a persistent WebSocket connection. A background "Cupid" service then periodically pairs users with a weighted scoring algorithm and pushes anonymous love letters to each subscriber.

The project demonstrates **real-time server push**, **hosted background services**, **thread-safe concurrent state**, **dependency injection**, and **interface-driven design** in modern .NET.

> The console UI is in Serbian; the code structure and identifiers are in English.

## How It Works

```
 ┌──────────────┐   InitSinglePerson()     ┌───────────────────────────────┐
 │  Client (N)  │ ───────────────────────► │         KupidonHub            │
 │ Console app  │   AcknowledgeLetter()    │   (SignalR hub /kupidonHub)   │
 │              │   BlockUser()            └──────────────┬────────────────┘
 │              │                                         │ writes
 │              │                                         ▼
 │              │                          ┌───────────────────────────────┐
 │              │                          │  PersonStore (singleton)      │
 │              │                          │  ConcurrentDictionary × 2     │
 │              │                          └──────────────▲────────────────┘
 │              │                                         │ reads
 │              │  ReceiveLoveLetter(dto)  ┌──────────────┴────────────────┐
 │              │ ◄─────────────────────── │ CupidBackgroundService        │
 └──────────────┘    (server push)         │  PeriodicTimer (every 60 s)   │
                                           │   → CupidService              │
                                           │   → MatchingService           │
                                           └───────────────────────────────┘
```

1. **Subscribe.** A client connects to the hub and registers a username, city, age, and phone number.
2. **Publish.** Every 60 seconds the background service runs a delivery cycle. For each subscriber it picks the best-scoring match and pushes a letter to that client only.
3. **Acknowledge.** A user receives no new letters until they confirm the current one. This acts as per-client flow control, so slow readers are never flooded.
4. **Block.** Users can type `/block <username>` to exclude someone from all future matches.

## Features

- **Real-time push** over SignalR with automatic client reconnection (1 s → 3 s → 10 s backoff).
- **Weighted matching algorithm:**

  | Factor                   | Points  |
  |--------------------------|---------|
  | Same city                | +30     |
  | Age difference ≤ 2 years | +20     |
  | Random factor            | 0–100   |

  The large random component is the "chaotic" part: compatibility helps, but it doesn't guarantee a match.
- **Cryptographically secure randomness** via `RandomNumberGenerator` instead of `System.Random`.
- **Privacy-aware payloads**: the sender's phone number is shared only when the message is positive. Rejection letters arrive with the number hidden.
- **Parallel delivery**: letters for all subscribers are sent concurrently with `Task.WhenAll`, with per-recipient error handling and state rollback on failure.
- **Thread-safe in-memory store** using two `ConcurrentDictionary` indexes (by connection ID and by case-insensitive username) with rollback on partial inserts.
- **Input validation** with regex rules for username, city (Latin and Cyrillic), age (18–99), and phone number.
- **Clean lifecycle handling**: users are removed from the store automatically on disconnect, and the background loop shuts down gracefully on cancellation.

## Project Structure

```
HaoticniKupidon.sln
├── Server/                          # ASP.NET Core SignalR server
│   ├── Program.cs                   # DI registration, CORS, hub mapping
│   ├── Hubs/KupidonHub.cs           # Client-callable methods + disconnect handling
│   ├── Background/
│   │   └── CupidBackgroundService.cs  # PeriodicTimer-driven publish loop
│   ├── Services/
│   │   ├── CupidService.cs          # Builds and pushes letters to subscribers
│   │   ├── MatchingService.cs       # Scoring algorithm
│   │   └── PersonStore.cs           # Thread-safe subscriber registry
│   ├── Interfaces/                  # ICupidService, IMatchingService, IPersonStore
│   ├── Models/                      # Person, LoveLetter
│   ├── DTOs/                        # RegisterPersonDto, LoveLetterDto (records)
│   ├── Validation/InputValidator.cs # Regex validation with a result object
│   └── Utilities/SecureRandom.cs    # Crypto-grade random helpers
└── Client/                          # .NET console client
    ├── Program.cs                   # Connection, registration, command loop
    ├── ConsoleHelper.cs             # Thread-safe colored console output
    └── Validators.cs                # Client-side input validation
```

**Design highlights**

- **Interface-based services** registered through ASP.NET Core dependency injection, so each component can be swapped or mocked independently.
- **`IHubContext<T>`** lets a service outside the hub push messages to specific clients.
- **Immutable `record` DTOs** define a clear contract between server and client.
- **Lock-guarded console output** keeps SignalR callbacks and the input loop from interleaving their text.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run the server

```bash
cd Server
dotnet run
```

The server listens on `http://localhost:5000`, and the hub is exposed at `/kupidonHub`.

### Run clients

Open **two or more** terminals, since matching needs at least two registered users:

```bash
cd Client
dotnet run
```

Each client asks for a username, city, age, and phone number, then waits for letters. The first letter arrives within about 60 seconds.

Alternatively, open `HaoticniKupidon.sln` in Visual Studio, set multiple startup projects, and run.

### Client commands

| Command             | Description                                    |
|---------------------|------------------------------------------------|
| `ENTER`             | Acknowledge the current letter                 |
| `/block <username>` | Exclude a user from future matches             |
| `/quit`             | Disconnect and exit                            |

## Example Letter

```
----------------------------------------
|         NOVO LJUBAVNO PISMO          |
----------------------------------------
  Korisnicko ime: marko_99
  Grad:           Novi Sad
  Godine:         24
  Telefon:        +381601234567

  💬 "Želim da se upoznamo."
----------------------------------------
[Pritisnite ENTER da potvrdite prijem pisma...]
```

## Roadmap

- Call the server-side `InputValidator` from the hub, so the server enforces the same rules as the client
- Unit tests for `MatchingService` and `PersonStore` (xUnit + Moq)
- Persistent storage (SQLite / EF Core) instead of in-memory state
- Configurable interval and scoring weights via `appsettings.json`
- Strongly typed hub client interface (`Hub<IKupidonClient>`)
- Browser client using the SignalR JavaScript library
