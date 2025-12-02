# Battleship - .NET MAUI Game

A classic Battleship game built with .NET MAUI, targeting Windows, Android, iOS, and macOS.

## 🎮 Game Features

- **Classic Battleship Gameplay**: Place your fleet and battle against a computer opponent
- **Smart AI Opponent**: Computer uses intelligent targeting strategy (follows up on hits)
- **Cross-Platform**: Runs on Windows, Android, iOS, and macOS
- **Modern UI**: Clean, responsive design with visual feedback for hits and misses
- **Standard Fleet**: 5 ships - Carrier (5), Battleship (4), Cruiser (3), Submarine (3), Destroyer (2)

## 📁 Project Structure

```
├── MauiBattleship/           # Main MAUI application
│   ├── Views/                # XAML UI pages
│   ├── ViewModels/           # MVVM view models
│   ├── Resources/            # App resources (icons, styles)
│   └── MauiBattleship.csproj
├── MauiBattleship.Core/      # Core game logic library
│   ├── Models/               # Game models (Ship, Cell, GameBoard)
│   ├── Services/             # Game services and AI
│   └── MauiBattleship.Core.csproj
├── MauiBattleship.Tests/     # Unit tests
│   └── MauiBattleship.Tests.csproj
└── MauiBattleship.sln        # Solution file
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.8+ with the .NET MAUI workload
  - Or [Visual Studio 2026](https://visualstudio.microsoft.com/) preview

### Installing MAUI Workload

```bash
dotnet workload install maui
```

### Building the Project

1. Clone the repository:
   ```bash
   git clone https://github.com/Echoing1822Tide/Battleship.git
   cd Battleship
   ```

2. Build the solution:
   ```bash
   dotnet build MauiBattleship.sln
   ```

3. Run tests:
   ```bash
   dotnet test MauiBattleship.Tests/MauiBattleship.Tests.csproj
   ```

### Running the Application

#### Windows
```bash
dotnet build -f net8.0-windows10.0.19041.0 MauiBattleship/MauiBattleship.csproj
dotnet run --project MauiBattleship/MauiBattleship.csproj -f net8.0-windows10.0.19041.0
```

#### Android
```bash
dotnet build -f net8.0-android MauiBattleship/MauiBattleship.csproj
```

#### macOS (Mac Catalyst)
```bash
dotnet build -f net8.0-maccatalyst MauiBattleship/MauiBattleship.csproj
```

## 🎯 How to Play

1. **Start a New Game**: Click the "New Game" button
2. **Place Your Ships**: 
   - Click on your board to place ships
   - Use the orientation button to toggle between horizontal/vertical
   - Place all 5 ships to begin the battle
3. **Attack Phase**:
   - Click on the enemy board to fire
   - Red = Hit, White = Miss
   - Sink all enemy ships to win!
4. **Watch Out**: The computer will attack your fleet each turn

## 🏗️ Architecture

The project follows the **MVVM pattern** with clear separation of concerns:

- **Models** (`MauiBattleship.Core/Models/`): Core game entities
  - `Cell`: Represents a single board cell
  - `Ship`: Ship with size, position, and hit tracking
  - `GameBoard`: 10x10 grid managing ships and attacks
  - `AttackResult`: Attack outcome information

- **Services** (`MauiBattleship.Core/Services/`):
  - `GameService`: Main game orchestration
  - `ComputerAI`: Smart AI opponent with hunt/target modes

- **ViewModels** (`MauiBattleship/ViewModels/`):
  - `GameViewModel`: Main game state and commands
  - `CellViewModel`: Individual cell display state

- **Views** (`MauiBattleship/Views/`):
  - `GamePage`: Main game UI with two 10x10 grids

## 🧪 Testing

The project includes comprehensive unit tests covering:

- Cell behavior
- Ship placement and hits
- GameBoard operations
- Computer AI strategies
- GameService game flow

Run tests:
```bash
dotnet test
```

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [.NET MAUI](https://docs.microsoft.com/dotnet/maui/)
- Uses [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) for MVVM implementation
