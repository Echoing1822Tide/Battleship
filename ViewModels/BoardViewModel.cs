using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Battleship.GameCore;
using Microsoft.Maui.Graphics;

namespace BattleshipMaui.ViewModels;

public class BoardViewModel : ObservableObject
{
    private readonly Random _random;
    private readonly IGameStatsStore _statsStore;
    private readonly Dictionary<string, Ship> _playerShipsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ShipSpriteVm> _playerSpritesByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ShipSpriteVm> _enemySpritesByName = new(StringComparer.OrdinalIgnoreCase);

    private GameBoard? _playerBoard;
    private GameBoard? _enemyBoard;
    private EnemyTargetingStrategy? _enemyTargetingStrategy;
    private PlacementShipVm? _selectedPlacementShip;

    private bool _isPlayerTurn;
    private bool _isGameOver;
    private bool _isPlacementPhase;
    private bool _isVerticalPlacement;
    private string _turnMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private string _playerLastShotMessage = string.Empty;
    private string _enemyLastShotMessage = string.Empty;
    private bool _showEnemyFleet;
    private int _wins;
    private int _losses;
    private int _draws;
    private int _totalTurns;
    private int _totalShots;
    private int _totalHits;
    private int _currentGameTurns;
    private int _currentGameShots;
    private int _currentGameHits;
    private string _lastGameSummary = "No completed games yet.";

    public const int Size = 10;
    public const double CellSize = 32;

    private static readonly ShipTemplate[] FleetTemplates =
    {
        new("Aircraft Carrier", 5, "Aircraft_Carrier_5_Pegs.png"),
        new("Battleship", 4, "Battleship_4_Pegs.png"),
        new("Cruiser", 3, "Cruiser_3_Pegs.png"),
        new("Submarine", 3, "Submarine_3_Pegs.png"),
        new("Destroyer", 2, "Destroyer_2_Pegs.png")
    };

    public double BoardPixelSize => Size * CellSize;
    public IReadOnlyList<string> RowLabels { get; } = Enumerable.Range(0, Size)
        .Select(i => ((char)('A' + i)).ToString())
        .ToArray();
    public IReadOnlyList<string> ColumnLabels { get; } = Enumerable.Range(1, Size)
        .Select(i => i.ToString())
        .ToArray();

    public ObservableCollection<BoardCellVm> EnemyCells { get; } = new();
    public ObservableCollection<BoardCellVm> PlayerCells { get; } = new();
    public ObservableCollection<ShipSpriteVm> PlayerShipSprites { get; } = new();
    public ObservableCollection<ShipSpriteVm> EnemyShipSprites { get; } = new();
    public ObservableCollection<PlacementShipVm> PlacementShips { get; } = new();

    public ICommand EnemyCellTappedCommand { get; }
    public ICommand PlayerCellTappedCommand { get; }
    public ICommand SelectPlacementShipCommand { get; }
    public ICommand RotatePlacementCommand { get; }
    public ICommand NewGameCommand { get; }
    public ICommand ResetStatsCommand { get; }

    public bool IsPlayerTurn
    {
        get => _isPlayerTurn;
        private set
        {
            if (_isPlayerTurn == value) return;
            _isPlayerTurn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFire));
        }
    }

    public bool IsGameOver
    {
        get => _isGameOver;
        private set
        {
            if (_isGameOver == value) return;
            _isGameOver = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFire));
            OnPropertyChanged(nameof(CanPlaceShips));
            OnPropertyChanged(nameof(CanRotatePlacement));
        }
    }

    public bool IsPlacementPhase
    {
        get => _isPlacementPhase;
        private set
        {
            if (_isPlacementPhase == value) return;
            _isPlacementPhase = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanFire));
            OnPropertyChanged(nameof(CanPlaceShips));
            OnPropertyChanged(nameof(CanRotatePlacement));
            OnPropertyChanged(nameof(PlacementSelectionMessage));
        }
    }

    public bool IsVerticalPlacement
    {
        get => _isVerticalPlacement;
        private set
        {
            if (_isVerticalPlacement == value) return;
            _isVerticalPlacement = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlacementOrientationText));
        }
    }

    public bool CanFire => !IsGameOver && !IsPlacementPhase && IsPlayerTurn;
    public bool CanPlaceShips => !IsGameOver && IsPlacementPhase;
    public bool CanRotatePlacement => CanPlaceShips && _selectedPlacementShip is not null;

    public string TurnMessage
    {
        get => _turnMessage;
        private set
        {
            if (_turnMessage == value) return;
            _turnMessage = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string PlayerLastShotMessage
    {
        get => _playerLastShotMessage;
        private set
        {
            if (_playerLastShotMessage == value) return;
            _playerLastShotMessage = value;
            OnPropertyChanged();
        }
    }

    public string EnemyLastShotMessage
    {
        get => _enemyLastShotMessage;
        private set
        {
            if (_enemyLastShotMessage == value) return;
            _enemyLastShotMessage = value;
            OnPropertyChanged();
        }
    }

    public bool ShowEnemyFleet
    {
        get => _showEnemyFleet;
        private set
        {
            if (_showEnemyFleet == value) return;
            _showEnemyFleet = value;
            OnPropertyChanged();
        }
    }

    public int Wins
    {
        get => _wins;
        private set
        {
            if (_wins == value) return;
            _wins = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int Losses
    {
        get => _losses;
        private set
        {
            if (_losses == value) return;
            _losses = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int Draws
    {
        get => _draws;
        private set
        {
            if (_draws == value) return;
            _draws = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int TotalTurns
    {
        get => _totalTurns;
        private set
        {
            if (_totalTurns == value) return;
            _totalTurns = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int TotalShots
    {
        get => _totalShots;
        private set
        {
            if (_totalShots == value) return;
            _totalShots = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HitRate));
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public int TotalHits
    {
        get => _totalHits;
        private set
        {
            if (_totalHits == value) return;
            _totalHits = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HitRate));
            OnPropertyChanged(nameof(StatsLine));
        }
    }

    public double HitRate => TotalShots == 0 ? 0 : (double)TotalHits / TotalShots;

    public string StatsLine =>
        $"Record: {Wins}-{Losses}" +
        (Draws > 0 ? $" ({Draws} draws)" : string.Empty) +
        $"   Turns: {TotalTurns}   Hit rate: {HitRate:P0}";

    public int CurrentGameTurns
    {
        get => _currentGameTurns;
        private set
        {
            if (_currentGameTurns == value) return;
            _currentGameTurns = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentGameHitRate));
            OnPropertyChanged(nameof(CurrentGameStatsLine));
        }
    }

    public int CurrentGameShots
    {
        get => _currentGameShots;
        private set
        {
            if (_currentGameShots == value) return;
            _currentGameShots = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentGameHitRate));
            OnPropertyChanged(nameof(CurrentGameStatsLine));
        }
    }

    public int CurrentGameHits
    {
        get => _currentGameHits;
        private set
        {
            if (_currentGameHits == value) return;
            _currentGameHits = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentGameHitRate));
            OnPropertyChanged(nameof(CurrentGameStatsLine));
        }
    }

    public double CurrentGameHitRate => CurrentGameShots == 0 ? 0 : (double)CurrentGameHits / CurrentGameShots;

    public string CurrentGameStatsLine =>
        $"Current game: turns {CurrentGameTurns}, shots {CurrentGameShots}, hit rate {CurrentGameHitRate:P0}";

    public string LastGameSummary
    {
        get => _lastGameSummary;
        private set
        {
            if (_lastGameSummary == value) return;
            _lastGameSummary = value;
            OnPropertyChanged();
        }
    }

    public string PlacementOrientationText =>
        IsVerticalPlacement ? "Orientation: Vertical" : "Orientation: Horizontal";

    public string PlacementSelectionMessage
    {
        get
        {
            if (!IsPlacementPhase)
                return "Battle in progress.";

            if (_selectedPlacementShip is null)
                return "All ships are placed.";

            return $"Selected ship: {_selectedPlacementShip.Name} ({_selectedPlacementShip.Size})";
        }
    }

    public string ScoreLine
    {
        get
        {
            int enemySunk = _enemyBoard?.ShipsSunk ?? 0;
            int enemyTotal = _enemyBoard?.TotalShips ?? 0;
            int playerSunk = _playerBoard?.ShipsSunk ?? 0;
            int playerTotal = _playerBoard?.TotalShips ?? 0;
            return $"Enemy sunk: {enemySunk}/{enemyTotal}   Your ships sunk: {playerSunk}/{playerTotal}";
        }
    }

    public BoardViewModel()
        : this(new Random(), new JsonFileGameStatsStore())
    {
    }

    public BoardViewModel(Random random)
        : this(random, new JsonFileGameStatsStore())
    {
    }

    public BoardViewModel(Random random, IGameStatsStore statsStore)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _statsStore = statsStore ?? throw new ArgumentNullException(nameof(statsStore));

        EnemyCellTappedCommand = new Command<BoardCellVm>(OnEnemyCellTapped);
        PlayerCellTappedCommand = new Command<BoardCellVm>(OnPlayerCellTapped);
        SelectPlacementShipCommand = new Command<PlacementShipVm>(OnSelectPlacementShip);
        RotatePlacementCommand = new Command(TogglePlacementOrientation);
        NewGameCommand = new Command(StartNewGame);
        ResetStatsCommand = new Command(ResetStats);

        LoadStats();
        InitializeCells(EnemyCells);
        InitializeCells(PlayerCells);
        StartNewGame();
    }

    private void LoadStats()
    {
        var snapshot = _statsStore.Load();
        Wins = snapshot.Wins;
        Losses = snapshot.Losses;
        Draws = snapshot.Draws;
        TotalTurns = snapshot.TotalTurns;
        TotalShots = snapshot.TotalShots;
        TotalHits = snapshot.TotalHits;
    }

    private void ResetCurrentGameStats()
    {
        CurrentGameTurns = 0;
        CurrentGameShots = 0;
        CurrentGameHits = 0;
    }

    private void ResetStats()
    {
        Wins = 0;
        Losses = 0;
        Draws = 0;
        TotalTurns = 0;
        TotalShots = 0;
        TotalHits = 0;
        ResetCurrentGameStats();
        LastGameSummary = "Stats reset.";
        SaveStats();
        StatusMessage = "Saved stats reset.";
    }

    private void SaveStats()
    {
        _statsStore.Save(new GameStatsSnapshot(
            Wins,
            Losses,
            Draws,
            TotalTurns,
            TotalShots,
            TotalHits));
    }

    private void RecordPlayerShot(ShotInfo shot)
    {
        TotalTurns++;
        TotalShots++;
        CurrentGameTurns++;
        CurrentGameShots++;

        if (shot.IsHit)
        {
            TotalHits++;
            CurrentGameHits++;
        }

        SaveStats();
    }

    private void RecordGameOutcome(GameOutcome outcome)
    {
        switch (outcome)
        {
            case GameOutcome.Win:
                Wins++;
                break;
            case GameOutcome.Loss:
                Losses++;
                break;
            case GameOutcome.Draw:
                Draws++;
                break;
        }

        LastGameSummary =
            $"{outcome} - turns {CurrentGameTurns}, shots {CurrentGameShots}, hits {CurrentGameHits}, hit rate {CurrentGameHitRate:P0}";

        SaveStats();
    }

    private void StartNewGame()
    {
        ResetCurrentGameStats();
        _playerBoard = new GameBoard(Size);
        _enemyBoard = new GameBoard(Size);

        var playerFleet = CreateFleet();
        var enemyFleet = CreateFleet();

        PlaceFleetRandomly(_enemyBoard, enemyFleet, allowVertical: true);

        _playerBoard.SetFleet(playerFleet);
        _enemyBoard.SetFleet(enemyFleet);

        ResetCells(EnemyCells);
        ResetCells(PlayerCells);
        PlayerShipSprites.Clear();
        EnemyShipSprites.Clear();
        _playerSpritesByName.Clear();
        _enemySpritesByName.Clear();

        InitializePlacementShips(playerFleet);
        BuildEnemyShipSprites(enemyFleet);
        InitializeEnemyTargeting();

        IsGameOver = false;
        IsPlacementPhase = true;
        IsVerticalPlacement = false;
        IsPlayerTurn = false;
        ShowEnemyFleet = false;

        TurnMessage = "Placement phase";
        StatusMessage = "Select a ship and tap Your Fleet board to place it.";
        PlayerLastShotMessage = "Your last shot: --";
        EnemyLastShotMessage = "Enemy last shot: --";

        OnPropertyChanged(nameof(PlacementOrientationText));
        OnPropertyChanged(nameof(PlacementSelectionMessage));
        OnPropertyChanged(nameof(ScoreLine));
    }

    private static void InitializeCells(ObservableCollection<BoardCellVm> cells)
    {
        cells.Clear();
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                cells.Add(new BoardCellVm(row, col));
            }
        }
    }

    private static void ResetCells(ObservableCollection<BoardCellVm> cells)
    {
        foreach (var cell in cells)
            cell.Reset();
    }

    private static List<Ship> CreateFleet()
    {
        var fleet = new List<Ship>(FleetTemplates.Length);
        foreach (var template in FleetTemplates)
            fleet.Add(new Ship(template.Name, template.Size));

        return fleet;
    }

    private void InitializePlacementShips(IEnumerable<Ship> playerFleet)
    {
        PlacementShips.Clear();
        _playerShipsByName.Clear();

        foreach (var template in FleetTemplates)
        {
            var ship = playerFleet.First(s => string.Equals(s.Name, template.Name, StringComparison.OrdinalIgnoreCase));
            _playerShipsByName[ship.Name] = ship;
            PlacementShips.Add(new PlacementShipVm(ship.Name, ship.Size, template.ImageSource));
        }

        SetSelectedPlacementShip(PlacementShips.FirstOrDefault());
    }

    private void SetSelectedPlacementShip(PlacementShipVm? ship)
    {
        foreach (var placementShip in PlacementShips)
            placementShip.IsSelected = false;

        _selectedPlacementShip = ship is not null && !ship.IsPlaced ? ship : null;
        if (_selectedPlacementShip is not null)
            _selectedPlacementShip.IsSelected = true;

        OnPropertyChanged(nameof(CanRotatePlacement));
        OnPropertyChanged(nameof(PlacementSelectionMessage));
    }

    private void OnSelectPlacementShip(PlacementShipVm? ship)
    {
        if (!CanPlaceShips || ship is null)
            return;

        if (ship.IsPlaced)
        {
            StatusMessage = $"{ship.Name} is already placed.";
            return;
        }

        SetSelectedPlacementShip(ship);
        StatusMessage = $"Placing {ship.Name} ({ship.Size}). Tap a starting cell on Your Fleet board.";
    }

    private void TogglePlacementOrientation()
    {
        if (!CanPlaceShips)
            return;

        IsVerticalPlacement = !IsVerticalPlacement;
        StatusMessage = $"{PlacementOrientationText}.";
    }

    private void OnPlayerCellTapped(BoardCellVm? targetCell)
    {
        if (!CanPlaceShips || targetCell is null || _playerBoard is null)
            return;

        if (_selectedPlacementShip is null)
        {
            StatusMessage = "Select a ship to place.";
            return;
        }

        if (!_playerShipsByName.TryGetValue(_selectedPlacementShip.Name, out var ship))
        {
            StatusMessage = $"Could not find ship data for {_selectedPlacementShip.Name}.";
            return;
        }

        ShipOrientation orientation = IsVerticalPlacement ? ShipOrientation.Vertical : ShipOrientation.Horizontal;
        bool placed = _playerBoard.TryPlaceShip(ship, targetCell.Row, targetCell.Col, orientation);

        if (!placed)
        {
            StatusMessage = $"Cannot place {_selectedPlacementShip.Name} at {ToBoardCoordinate(targetCell.Row, targetCell.Col)}. Try another cell or rotate.";
            return;
        }

        _selectedPlacementShip.IsPlaced = true;
        AddPlayerShipSprite(ship, _selectedPlacementShip.ImageSource);

        var coordinate = ToBoardCoordinate(targetCell.Row, targetCell.Col);
        StatusMessage = $"Placed {_selectedPlacementShip.Name} at {coordinate}.";

        var next = PlacementShips.FirstOrDefault(s => !s.IsPlaced);
        if (next is null)
        {
            CompletePlacementPhase();
            return;
        }

        SetSelectedPlacementShip(next);
    }

    private void CompletePlacementPhase()
    {
        SetSelectedPlacementShip(null);
        IsPlacementPhase = false;
        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        StatusMessage = "All ships placed. Tap a cell on Enemy Waters to fire.";
    }

    private void AddPlayerShipSprite(Ship ship, string imageSource)
    {
        if (ship.Positions.Count == 0)
            return;

        int row = ship.Positions.Min(p => p.Row);
        int col = ship.Positions.Min(p => p.Col);
        bool isVertical = ship.Positions.Select(p => p.Col).Distinct().Count() == 1;

        var sprite = new ShipSpriteVm(
            ship.Name,
            imageSource,
            row,
            col,
            ship.Size,
            isVertical ? ShipAxis.Vertical : ShipAxis.Horizontal,
            isEnemy: false,
            isRevealed: true);

        PlayerShipSprites.Add(sprite);
        _playerSpritesByName[ship.Name] = sprite;
    }

    private void BuildEnemyShipSprites(IEnumerable<Ship> enemyFleet)
    {
        EnemyShipSprites.Clear();
        _enemySpritesByName.Clear();

        foreach (var ship in enemyFleet)
        {
            if (ship.Positions.Count == 0)
                continue;

            int row = ship.Positions.Min(p => p.Row);
            int col = ship.Positions.Min(p => p.Col);
            bool isVertical = ship.Positions.Select(p => p.Col).Distinct().Count() == 1;

            string imageSource = FleetTemplates
                .First(t => string.Equals(t.Name, ship.Name, StringComparison.OrdinalIgnoreCase))
                .ImageSource;

            var sprite = new ShipSpriteVm(
                ship.Name,
                imageSource,
                row,
                col,
                ship.Size,
                isVertical ? ShipAxis.Vertical : ShipAxis.Horizontal,
                isEnemy: true,
                isRevealed: false);

            EnemyShipSprites.Add(sprite);
            _enemySpritesByName[ship.Name] = sprite;
        }
    }

    private void PlaceFleetRandomly(GameBoard board, IEnumerable<Ship> fleet, bool allowVertical)
    {
        foreach (var ship in fleet)
        {
            bool placed = false;

            for (int attempts = 0; attempts < 500 && !placed; attempts++)
            {
                int row = _random.Next(Size);
                int col = _random.Next(Size);
                ShipOrientation orientation = allowVertical && _random.Next(2) == 0
                    ? ShipOrientation.Vertical
                    : ShipOrientation.Horizontal;

                placed = board.TryPlaceShip(ship, row, col, orientation);
            }

            if (!placed)
                throw new InvalidOperationException($"Could not place ship: {ship.Name}");
        }
    }

    private void InitializeEnemyTargeting()
    {
        _enemyTargetingStrategy = new EnemyTargetingStrategy(Size, _random);
    }

    private void RevealEnemyFleet()
    {
        ShowEnemyFleet = true;
        foreach (var sprite in EnemyShipSprites)
            sprite.Reveal();
    }

    private void OnEnemyCellTapped(BoardCellVm? targetCell)
    {
        if (targetCell is null || _enemyBoard is null || _playerBoard is null)
            return;

        if (IsGameOver)
        {
            StatusMessage = "Game over. Press New Game to play again.";
            return;
        }

        if (IsPlacementPhase)
        {
            StatusMessage = "Place all ships on Your Fleet board before firing.";
            return;
        }

        if (!IsPlayerTurn)
        {
            StatusMessage = "Enemy turn in progress.";
            return;
        }

        var playerShot = _enemyBoard.Attack(targetCell.Row, targetCell.Col);

        if (playerShot.Result == AttackResult.AlreadyTried)
        {
            StatusMessage = "You already fired at that cell.";
            return;
        }

        RecordPlayerShot(playerShot);
        ApplyShotResult(EnemyCells, playerShot);
        PlayerLastShotMessage = $"Your last shot: {ToBoardCoordinate(playerShot.Row, playerShot.Col)} - {playerShot.Message}";
        StatusMessage = "Enemy is firing...";
        OnPropertyChanged(nameof(ScoreLine));

        if (playerShot.Result == AttackResult.Sunk &&
            playerShot.SunkShipName is not null &&
            _enemySpritesByName.TryGetValue(playerShot.SunkShipName, out var enemySprite))
        {
            enemySprite.MarkSunk();
        }

        if (_enemyBoard.AllShipsSunk)
        {
            IsGameOver = true;
            IsPlayerTurn = false;
            TurnMessage = "Victory";
            StatusMessage = "All enemy ships sunk. You win.";
            EnemyLastShotMessage = "Enemy last shot: --";
            RecordGameOutcome(GameOutcome.Win);
            RevealEnemyFleet();
            return;
        }

        IsPlayerTurn = false;
        TurnMessage = "Enemy turn";
        EnemyTakeTurn();
    }

    private void EnemyTakeTurn()
    {
        if (_playerBoard is null || _enemyTargetingStrategy is null)
            return;

        BoardCoordinate target;
        try
        {
            target = _enemyTargetingStrategy.GetNextShot();
        }
        catch (InvalidOperationException)
        {
            IsGameOver = true;
            TurnMessage = "Draw";
            StatusMessage = "No remaining shots.";
            EnemyLastShotMessage = "Enemy last shot: --";
            RecordGameOutcome(GameOutcome.Draw);
            RevealEnemyFleet();
            return;
        }

        var enemyShot = _playerBoard.Attack(target.Row, target.Col);
        _enemyTargetingStrategy.RegisterShotOutcome(target, enemyShot.Result);
        ApplyShotResult(PlayerCells, enemyShot);

        if (enemyShot.Result == AttackResult.Sunk &&
            enemyShot.SunkShipName is not null &&
            _playerSpritesByName.TryGetValue(enemyShot.SunkShipName, out var sprite))
        {
            sprite.MarkSunk();
        }

        OnPropertyChanged(nameof(ScoreLine));

        if (_playerBoard.AllShipsSunk)
        {
            IsGameOver = true;
            TurnMessage = "Defeat";
            EnemyLastShotMessage = $"Enemy last shot: {ToBoardCoordinate(enemyShot.Row, enemyShot.Col)} - {enemyShot.Message}";
            StatusMessage = "All your ships have been sunk. You lose.";
            RecordGameOutcome(GameOutcome.Loss);
            RevealEnemyFleet();
            return;
        }

        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        EnemyLastShotMessage = $"Enemy last shot: {ToBoardCoordinate(enemyShot.Row, enemyShot.Col)} - {enemyShot.Message}";
        StatusMessage = "Tap a cell on Enemy Waters to fire.";
    }

    private static void ApplyShotResult(ObservableCollection<BoardCellVm> cells, ShotInfo shot)
    {
        int index = shot.Row * Size + shot.Col;
        if (index < 0 || index >= cells.Count)
            return;

        cells[index].ApplyShot(shot);
    }

    private static string ToBoardCoordinate(int row, int col)
    {
        char letter = (char)('A' + row);
        return $"{letter}{col + 1}";
    }
}

public enum GameOutcome
{
    Win = 0,
    Loss = 1,
    Draw = 2
}

public enum ShotMarkerState
{
    None = 0,
    Miss = 1,
    Hit = 2
}

public class BoardCellVm : ObservableObject
{
    private ShotMarkerState _markerState;

    public int Row { get; }
    public int Col { get; }

    public ShotMarkerState MarkerState
    {
        get => _markerState;
        private set
        {
            if (_markerState == value) return;
            _markerState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MarkerText));
            OnPropertyChanged(nameof(MarkerColor));
            OnPropertyChanged(nameof(MarkerImage));
            OnPropertyChanged(nameof(MarkerStateText));
            OnPropertyChanged(nameof(AccessibilityText));
        }
    }

    public string CoordinateText => $"{(char)('A' + Row)}{Col + 1}";

    public string MarkerText => MarkerState switch
    {
        ShotMarkerState.Miss => "•",
        ShotMarkerState.Hit => "X",
        _ => string.Empty
    };

    public Color MarkerColor => MarkerState switch
    {
        ShotMarkerState.Hit => Colors.OrangeRed,
        ShotMarkerState.Miss => Colors.WhiteSmoke,
        _ => Colors.Transparent
    };

    public string? MarkerImage => MarkerState == ShotMarkerState.Hit ? "Explosion.png" : null;

    public string MarkerStateText => MarkerState switch
    {
        ShotMarkerState.Hit => "hit",
        ShotMarkerState.Miss => "miss",
        _ => "untargeted"
    };

    public string AccessibilityText => $"{CoordinateText}, {MarkerStateText}";

    public BoardCellVm(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public void ApplyShot(ShotInfo shot)
    {
        MarkerState = shot.IsHit ? ShotMarkerState.Hit : ShotMarkerState.Miss;
    }

    public void Reset()
    {
        MarkerState = ShotMarkerState.None;
    }
}

public class ShipSpriteVm : ObservableObject
{
    private bool _isSunk;
    private bool _isRevealed;

    public string Name { get; }
    public string ImageSource { get; }
    public int StartRow { get; }
    public int StartCol { get; }
    public int Length { get; }
    public ShipAxis Axis { get; }
    public bool IsEnemy { get; }

    public Rect Bounds => new(
        StartCol * BoardViewModel.CellSize,
        StartRow * BoardViewModel.CellSize,
        Axis == ShipAxis.Horizontal ? Length * BoardViewModel.CellSize : BoardViewModel.CellSize,
        Axis == ShipAxis.Horizontal ? BoardViewModel.CellSize : Length * BoardViewModel.CellSize);

    public double Rotation => Axis == ShipAxis.Vertical ? 90 : 0;

    public bool IsSunk
    {
        get => _isSunk;
        private set
        {
            if (_isSunk == value) return;
            _isSunk = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opacity));
            OnPropertyChanged(nameof(StrokeColor));
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    public bool IsRevealed
    {
        get => _isRevealed;
        private set
        {
            if (_isRevealed == value) return;
            _isRevealed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Opacity));
        }
    }

    public double Opacity
    {
        get
        {
            if (!IsRevealed)
                return 0;

            return IsSunk ? 0.4 : 0.95;
        }
    }

    public Color StrokeColor => IsSunk
        ? Color.FromArgb("#ff8a6b")
        : IsEnemy
            ? Color.FromArgb("#7c8ea6")
            : Color.FromArgb("#70839a");

    public Color BackgroundColor => IsSunk
        ? Color.FromArgb("#442018")
        : IsEnemy
            ? Color.FromArgb("#1a2836")
            : Color.FromArgb("#1d2734");

    public ShipSpriteVm(
        string name,
        string imageSource,
        int startRow,
        int startCol,
        int length,
        ShipAxis axis,
        bool isEnemy = false,
        bool isRevealed = true)
    {
        Name = name;
        ImageSource = imageSource;
        StartRow = startRow;
        StartCol = startCol;
        Length = length;
        Axis = axis;
        IsEnemy = isEnemy;
        _isRevealed = isRevealed;
    }

    public void MarkSunk()
    {
        IsSunk = true;
    }

    public void Reveal()
    {
        IsRevealed = true;
    }
}

public class PlacementShipVm : ObservableObject
{
    private bool _isPlaced;
    private bool _isSelected;

    public string Name { get; }
    public int Size { get; }
    public string ImageSource { get; }

    public bool IsPlaced
    {
        get => _isPlaced;
        set
        {
            if (_isPlaced == value) return;
            _isPlaced = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(CardBackground));
            OnPropertyChanged(nameof(CardStroke));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CardBackground));
            OnPropertyChanged(nameof(CardStroke));
        }
    }

    public string DisplayName => IsPlaced ? $"{Name} ({Size}) - Placed" : $"{Name} ({Size})";

    public Color CardBackground => IsPlaced
        ? Color.FromArgb("#23553e")
        : IsSelected
            ? Color.FromArgb("#2a4f87")
            : Color.FromArgb("#1c2735");

    public Color CardStroke => IsPlaced
        ? Color.FromArgb("#7fe3ab")
        : IsSelected
            ? Color.FromArgb("#9fc3ff")
            : Color.FromArgb("#4f6178");

    public PlacementShipVm(string name, int size, string imageSource)
    {
        Name = name;
        Size = size;
        ImageSource = imageSource;
    }
}

public readonly record struct BoardCoordinate(int Row, int Col);

public sealed record ShipTemplate(string Name, int Size, string ImageSource);

public enum ShipAxis
{
    Horizontal = 0,
    Vertical = 1
}

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null)
            return;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
