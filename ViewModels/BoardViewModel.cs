using System.Collections.ObjectModel;
using System.Windows.Input;
using Battleship.GameCore;
using Microsoft.Maui.Graphics;

namespace BattleshipMaui.ViewModels;

public class BoardViewModel : BindableObject
{
    private readonly Random _random = new();
    private readonly List<BoardCoordinate> _enemyShotQueue = new();
    private readonly Dictionary<string, ShipSpriteVm> _playerSpritesByName = new(StringComparer.OrdinalIgnoreCase);

    private GameBoard? _playerBoard;
    private GameBoard? _enemyBoard;

    private bool _isPlayerTurn;
    private bool _isGameOver;
    private string _turnMessage = string.Empty;
    private string _statusMessage = string.Empty;

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

    public ObservableCollection<BoardCellVm> EnemyCells { get; } = new();
    public ObservableCollection<BoardCellVm> PlayerCells { get; } = new();
    public ObservableCollection<ShipSpriteVm> PlayerShipSprites { get; } = new();

    public ICommand EnemyCellTappedCommand { get; }
    public ICommand NewGameCommand { get; }

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
        }
    }

    public bool CanFire => !IsGameOver && IsPlayerTurn;

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
    {
        EnemyCellTappedCommand = new Command<BoardCellVm>(OnEnemyCellTapped);
        NewGameCommand = new Command(StartNewGame);

        InitializeCells(EnemyCells);
        InitializeCells(PlayerCells);
        StartNewGame();
    }

    private void StartNewGame()
    {
        _playerBoard = new GameBoard(Size);
        _enemyBoard = new GameBoard(Size);

        var playerFleet = CreateFleet();
        var enemyFleet = CreateFleet();

        PlaceFleetRandomly(_playerBoard, playerFleet, allowVertical: false);
        PlaceFleetRandomly(_enemyBoard, enemyFleet, allowVertical: true);

        _playerBoard.SetFleet(playerFleet);
        _enemyBoard.SetFleet(enemyFleet);

        ResetCells(EnemyCells);
        ResetCells(PlayerCells);

        BuildPlayerShipSprites(playerFleet);
        BuildEnemyShotQueue();

        IsGameOver = false;
        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        StatusMessage = "Tap a cell on Enemy Waters to fire.";
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

    private void BuildPlayerShipSprites(IEnumerable<Ship> fleet)
    {
        PlayerShipSprites.Clear();
        _playerSpritesByName.Clear();

        foreach (var ship in fleet)
        {
            if (ship.Positions.Count == 0)
                continue;

            int row = ship.Positions.Min(p => p.Row);
            int col = ship.Positions.Min(p => p.Col);

            string imageSource = FleetTemplates
                .First(t => string.Equals(t.Name, ship.Name, StringComparison.OrdinalIgnoreCase))
                .ImageSource;

            var sprite = new ShipSpriteVm(ship.Name, imageSource, row, col, ship.Size);
            PlayerShipSprites.Add(sprite);
            _playerSpritesByName[ship.Name] = sprite;
        }
    }

    private static List<Ship> CreateFleet()
    {
        var fleet = new List<Ship>(FleetTemplates.Length);
        foreach (var template in FleetTemplates)
            fleet.Add(new Ship(template.Name, template.Size));

        return fleet;
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

    private void BuildEnemyShotQueue()
    {
        _enemyShotQueue.Clear();

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                _enemyShotQueue.Add(new BoardCoordinate(row, col));
            }
        }

        for (int i = _enemyShotQueue.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_enemyShotQueue[i], _enemyShotQueue[j]) = (_enemyShotQueue[j], _enemyShotQueue[i]);
        }
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

        ApplyShotResult(EnemyCells, playerShot);
        StatusMessage = $"You fired at {ToBoardCoordinate(playerShot.Row, playerShot.Col)}: {playerShot.Message}";
        OnPropertyChanged(nameof(ScoreLine));

        if (_enemyBoard.AllShipsSunk)
        {
            IsGameOver = true;
            IsPlayerTurn = false;
            TurnMessage = "Victory";
            StatusMessage = "All enemy ships sunk. You win.";
            return;
        }

        IsPlayerTurn = false;
        TurnMessage = "Enemy turn";
        EnemyTakeTurn();
    }

    private void EnemyTakeTurn()
    {
        if (_playerBoard is null)
            return;

        if (_enemyShotQueue.Count == 0)
        {
            IsGameOver = true;
            TurnMessage = "Draw";
            StatusMessage = "No remaining shots.";
            return;
        }

        var target = _enemyShotQueue[^1];
        _enemyShotQueue.RemoveAt(_enemyShotQueue.Count - 1);

        var enemyShot = _playerBoard.Attack(target.Row, target.Col);
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
            StatusMessage = $"Enemy fired at {ToBoardCoordinate(enemyShot.Row, enemyShot.Col)}: {enemyShot.Message} You lose.";
            return;
        }

        IsPlayerTurn = true;
        TurnMessage = "Your turn";
        StatusMessage = $"Enemy fired at {ToBoardCoordinate(enemyShot.Row, enemyShot.Col)}: {enemyShot.Message}";
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

public enum ShotMarkerState
{
    None = 0,
    Miss = 1,
    Hit = 2
}

public class BoardCellVm : BindableObject
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
        }
    }

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

public class ShipSpriteVm : BindableObject
{
    private double _opacity = 0.95;

    public string Name { get; }
    public string ImageSource { get; }
    public int StartRow { get; }
    public int StartCol { get; }
    public int Length { get; }

    public Rect Bounds => new(
        StartCol * BoardViewModel.CellSize,
        StartRow * BoardViewModel.CellSize,
        Length * BoardViewModel.CellSize,
        BoardViewModel.CellSize);

    public double Opacity
    {
        get => _opacity;
        private set
        {
            if (Math.Abs(_opacity - value) < 0.001) return;
            _opacity = value;
            OnPropertyChanged();
        }
    }

    public ShipSpriteVm(string name, string imageSource, int startRow, int startCol, int length)
    {
        Name = name;
        ImageSource = imageSource;
        StartRow = startRow;
        StartCol = startCol;
        Length = length;
    }

    public void MarkSunk()
    {
        Opacity = 0.35;
    }
}

public readonly record struct BoardCoordinate(int Row, int Col);

public sealed record ShipTemplate(string Name, int Size, string ImageSource);
