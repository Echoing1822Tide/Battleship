using System;
using System.Collections.Generic;
using System.Linq;
using MauiBattleship.Models;

namespace MauiBattleship.Services
{
    public sealed class GameService
    {
        private readonly Random _random = new();

        private List<Ship> _playerFleet = new();
        private List<Ship> _computerFleet = new();

        public GameBoard PlayerBoard { get; private set; } = new();
        public GameBoard ComputerBoard { get; private set; } = new();

        public IReadOnlyList<Ship> PlayerFleet => _playerFleet;
        public IReadOnlyList<Ship> ComputerFleet => _computerFleet;
        public GamePhase CurrentPhase { get; private set; } = GamePhase.NotStarted;
        public bool IsGameOver { get; private set; }

        public PlayerType Winner { get; private set; } = PlayerType.None;

        public event EventHandler? GameStateChanged;
        public event EventHandler<string>? MessageReceived;

        public GameService()
        {
        }

        // ----------------------------------------------------
        // Public API used by Home.razor
        // ----------------------------------------------------

        public void StartNewGame()
        {
            IsGameOver = false;
            Winner = PlayerType.None;
            CurrentPhase = GamePhase.PlacingShips;

            PlayerBoard = new GameBoard();
            ComputerBoard = new GameBoard();

            _playerFleet = CreateStandardFleet();
            _computerFleet = CreateStandardFleet();

            PlaceComputerFleetRandomly();

            OnMessage("New game started. Place your ships on the board.");
            OnGameStateChanged();
        }

        public Ship? GetNextShipToPlace()
            => _playerFleet.FirstOrDefault(s => !s.IsPlaced);

        // ----------------------------------------------------
        // Compatibility aliases for older UI code
        // ----------------------------------------------------

        public IReadOnlyList<Ship> GetPlayerFleet() => PlayerFleet;

        public Ship? GetNextPlayerShipToPlace() => GetNextShipToPlace();

        public bool PlacePlayerShip(Ship ship, int row, int col, bool isVertical)
            => PlacePlayerShip(ship, row, col, isVertical ? ShipOrientation.Vertical : ShipOrientation.Horizontal);

        public bool PlacePlayerShip(Ship ship, int row, int col, ShipOrientation orientation)
        {
            if (CurrentPhase != GamePhase.PlacingShips)
                return false;

            var placed = PlayerBoard.PlaceShip(ship, row, col, orientation);
            if (!placed)
                return false;

            var next = GetNextShipToPlace();

            if (next is null)
            {
                CurrentPhase = GamePhase.PlayerTurn;
                OnMessage("All ships placed. Your turn to attack.");
            }
            else
            {
                OnMessage($"Placed {ship.Name} (size {ship.Size}). Next ship: {next.Name} (size {next.Size}).");
            }

            OnGameStateChanged();
            return true;
        }

        /// <summary>
        /// Called when the user clicks an enemy cell.
        /// Returns null if they already fired there.
        /// </summary>
        public AttackResult? PlayerAttack(int row, int col)
        {
            if (IsGameOver || CurrentPhase != GamePhase.PlayerTurn)
                return AttackResult.Invalid;

            var result = ComputerBoard.FireAt(row, col);

            if (result == AttackResult.AlreadyTried)
            {
                OnMessage("You already fired at that cell.");
                return null;
            }

            switch (result)
            {
                case AttackResult.Miss:
                    OnMessage("Miss! Enemy's turn.");
                    CurrentPhase = GamePhase.ComputerTurn;
                    break;

                case AttackResult.Hit:
                    OnMessage("Hit!");
                    break;

                case AttackResult.Sunk:
                    OnMessage("You sunk an enemy ship!");
                    break;
            }

            if (ComputerBoard.AllShipsSunk)
            {
                IsGameOver = true;
                CurrentPhase = GamePhase.GameOver;
                Winner = PlayerType.Human;
                OnMessage("All enemy ships sunk. You win!");
            }

            OnGameStateChanged();
            return result;
        }

        public void ComputerAttack()
        {
            if (IsGameOver || CurrentPhase != GamePhase.ComputerTurn)
                return;

            AttackResult result;
            int row, col;

            do
            {
                row = _random.Next(GameBoard.BoardSize);
                col = _random.Next(GameBoard.BoardSize);

                result = PlayerBoard.FireAt(row, col);
            }
            while (result == AttackResult.AlreadyTried);

            switch (result)
            {
                case AttackResult.Miss:
                    OnMessage("Enemy missed! Your turn.");
                    CurrentPhase = GamePhase.PlayerTurn;
                    break;

                case AttackResult.Hit:
                    OnMessage("Enemy hit one of your ships!");
                    break;

                case AttackResult.Sunk:
                    OnMessage("Enemy sunk one of your ships!");
                    break;
            }

            if (PlayerBoard.AllShipsSunk)
            {
                IsGameOver = true;
                CurrentPhase = GamePhase.GameOver;
                Winner = PlayerType.Computer;
                OnMessage("All your ships are sunk. You lose.");
            }

            OnGameStateChanged();
        }

        private static List<Ship> CreateStandardFleet()
        {
            return new List<Ship>
            {
                new Ship("Carrier",     5),
                new Ship("Battleship",  4),
                new Ship("Cruiser",     3),
                new Ship("Submarine",   3),
                new Ship("Destroyer",   2)
            };
        }

        private void PlaceComputerFleetRandomly()
        {
            foreach (var ship in _computerFleet)
            {
                bool placed = false;

                while (!placed)
                {
                    int row = _random.Next(GameBoard.BoardSize);
                    int col = _random.Next(GameBoard.BoardSize);

                    var orientation = _random.Next(2) == 0
                        ? ShipOrientation.Horizontal
                        : ShipOrientation.Vertical;

                    placed = ComputerBoard.PlaceShip(ship, row, col, orientation);
                }
            }
        }

        private void OnGameStateChanged()
            => GameStateChanged?.Invoke(this, EventArgs.Empty);

        private void OnMessage(string message)
            => MessageReceived?.Invoke(this, message);
    }
}
