using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiBattleship.Models;
using MauiBattleship.Services;
using System.Collections.ObjectModel;

namespace MauiBattleship.ViewModels;

/// <summary>
/// Represents a cell in the UI grid.
/// </summary>
public partial class CellViewModel : ObservableObject
{
    [ObservableProperty]
    private CellState _state;

    [ObservableProperty]
    private bool _isPlayerBoard;

    [ObservableProperty]
    private int _row;

    [ObservableProperty]
    private int _column;

    [ObservableProperty]
    private bool _showShip;

    public Color CellColor => GetCellColor();

    private Color GetCellColor()
    {
        return State switch
        {
            CellState.Empty => IsPlayerBoard && ShowShip ? Colors.LightBlue : Colors.DodgerBlue,
            CellState.Ship => IsPlayerBoard && ShowShip ? Colors.Gray : Colors.DodgerBlue,
            CellState.Miss => Colors.White,
            CellState.Hit => Colors.Red,
            _ => Colors.DodgerBlue
        };
    }

    public string CellText => State switch
    {
        CellState.Miss => "○",
        CellState.Hit => "✕",
        _ => ""
    };

    public void Refresh()
    {
        OnPropertyChanged(nameof(CellColor));
        OnPropertyChanged(nameof(CellText));
    }
}

/// <summary>
/// Main view model for the Battleship game.
/// </summary>
public partial class GameViewModel : ObservableObject
{
    private readonly GameService _gameService;

    [ObservableProperty]
    private string _statusMessage = "Welcome to Battleship! Press 'New Game' to start.";

    [ObservableProperty]
    private string _currentPhaseText = "Not Started";

    [ObservableProperty]
    private bool _isPlacingShips;

    [ObservableProperty]
    private bool _isPlayerTurn;

    [ObservableProperty]
    private string _currentShipToPlace = "";

    [ObservableProperty]
    private ShipOrientation _currentOrientation = ShipOrientation.Horizontal;

    [ObservableProperty]
    private string _orientationText = "Horizontal";

    [ObservableProperty]
    private int _playerShipsSunk;

    [ObservableProperty]
    private int _computerShipsSunk;

    [ObservableProperty]
    private bool _isGameOver;

    [ObservableProperty]
    private string _gameResultText = "";

    public ObservableCollection<CellViewModel> PlayerCells { get; } = new();
    public ObservableCollection<CellViewModel> ComputerCells { get; } = new();

    public GameViewModel()
    {
        _gameService = new GameService();
        _gameService.GameStateChanged += OnGameStateChanged;
        _gameService.MessageReceived += OnMessageReceived;
        
        InitializeCells();
    }

    private void InitializeCells()
    {
        PlayerCells.Clear();
        ComputerCells.Clear();

        for (int row = 0; row < GameBoard.BoardSize; row++)
        {
            for (int col = 0; col < GameBoard.BoardSize; col++)
            {
                PlayerCells.Add(new CellViewModel
                {
                    Row = row,
                    Column = col,
                    State = CellState.Empty,
                    IsPlayerBoard = true,
                    ShowShip = true
                });

                ComputerCells.Add(new CellViewModel
                {
                    Row = row,
                    Column = col,
                    State = CellState.Empty,
                    IsPlayerBoard = false,
                    ShowShip = false
                });
            }
        }
    }

    [RelayCommand]
    private void NewGame()
    {
        InitializeCells();
        _gameService.StartNewGame();
        UpdateUI();
    }

    [RelayCommand]
    private void ToggleOrientation()
    {
        CurrentOrientation = CurrentOrientation == ShipOrientation.Horizontal
            ? ShipOrientation.Vertical
            : ShipOrientation.Horizontal;
        
        OrientationText = CurrentOrientation == ShipOrientation.Horizontal ? "Horizontal" : "Vertical";
    }

    [RelayCommand]
    private void PlayerBoardClick(CellViewModel cell)
    {
        if (_gameService.CurrentPhase != GamePhase.PlacingShips)
            return;

        var ship = _gameService.GetNextShipToPlace();
        if (ship == null)
            return;

        bool success = _gameService.PlacePlayerShip(ship, cell.Row, cell.Column, CurrentOrientation);
        
        if (success)
        {
            UpdatePlayerBoard();
            UpdateUI();
        }
        else
        {
            StatusMessage = "Cannot place ship there. Try a different position.";
        }
    }

    [RelayCommand]
    private async Task ComputerBoardClick(CellViewModel cell)
    {
        if (_gameService.CurrentPhase != GamePhase.PlayerTurn)
            return;

        var result = _gameService.PlayerAttack(cell.Row, cell.Column);
        
        if (result == null || result.AlreadyAttacked)
            return;

        UpdateComputerBoard();
        UpdateUI();

        if (_gameService.IsGameOver)
        {
            return;
        }

        // Computer's turn with a small delay
        await Task.Delay(500);
        _gameService.ComputerAttack();
        UpdatePlayerBoard();
        UpdateUI();
    }

    private void UpdatePlayerBoard()
    {
        for (int row = 0; row < GameBoard.BoardSize; row++)
        {
            for (int col = 0; col < GameBoard.BoardSize; col++)
            {
                int index = row * GameBoard.BoardSize + col;
                var cell = _gameService.PlayerBoard.GetCell(row, col);
                var cellVm = PlayerCells[index];
                cellVm.State = cell.State;
                cellVm.ShowShip = true;
                cellVm.Refresh();
            }
        }
    }

    private void UpdateComputerBoard()
    {
        for (int row = 0; row < GameBoard.BoardSize; row++)
        {
            for (int col = 0; col < GameBoard.BoardSize; col++)
            {
                int index = row * GameBoard.BoardSize + col;
                var cell = _gameService.ComputerBoard.GetCell(row, col);
                var cellVm = ComputerCells[index];
                
                // Only show hit/miss, not the ship locations
                if (cell.State == CellState.Hit || cell.State == CellState.Miss)
                {
                    cellVm.State = cell.State;
                }
                cellVm.Refresh();
            }
        }
    }

    private void UpdateUI()
    {
        IsPlacingShips = _gameService.CurrentPhase == GamePhase.PlacingShips;
        IsPlayerTurn = _gameService.CurrentPhase == GamePhase.PlayerTurn;
        IsGameOver = _gameService.IsGameOver;

        CurrentPhaseText = _gameService.CurrentPhase switch
        {
            GamePhase.NotStarted => "Not Started",
            GamePhase.PlacingShips => "Placing Ships",
            GamePhase.PlayerTurn => "Your Turn",
            GamePhase.ComputerTurn => "Computer's Turn",
            GamePhase.GameOver => "Game Over",
            _ => ""
        };

        var nextShip = _gameService.GetNextShipToPlace();
        CurrentShipToPlace = nextShip != null ? $"{nextShip.Name} ({nextShip.Size} cells)" : "";

        PlayerShipsSunk = _gameService.PlayerBoard.SunkShipCount;
        ComputerShipsSunk = _gameService.ComputerBoard.SunkShipCount;

        if (IsGameOver)
        {
            GameResultText = _gameService.PlayerWon ? "You Won!" : "Computer Won!";
        }
    }

    private void OnGameStateChanged(object? sender, EventArgs e)
    {
        UpdateUI();
    }

    private void OnMessageReceived(object? sender, string message)
    {
        StatusMessage = message;
    }
}
