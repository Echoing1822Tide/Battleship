using MauiBattleship.GameCore;
using MauiBattleship.GameCore.Enums;
using MauiBattleship.GameCore.Ships;

namespace MauiBattleship.Services
{
    public sealed class GameService
    {
        private readonly Random _rng = new();

        public event Action? OnChanged;

        public GamePhase Phase { get; private set; } = GamePhase.PlacingShips;
        public ShipOrientation CurrentOrientation { get; private set; } = ShipOrientation.Horizontal;

        public GameBoard PlayerBoard { get; private set; } = new();
        public GameBoard EnemyBoard { get; private set; } = new();

        public List<ShipBase> PlayerShips { get; private set; } = new();
        public List<ShipBase> EnemyShips { get; private set; } = new();

        public int CurrentPlacementIndex { get; private set; } = 0;

        public ShipBase? CurrentShipToPlace =>
            (Phase == GamePhase.PlacingShips && CurrentPlacementIndex < PlayerShips.Count)
                ? PlayerShips[CurrentPlacementIndex]
                : null;

        public void NewGame()
        {
            PlayerBoard = new GameBoard();
            EnemyBoard = new GameBoard();

            PlayerShips = CreateStandardFleet();
            EnemyShips = CreateStandardFleet();

            CurrentPlacementIndex = 0;
            Phase = GamePhase.PlacingShips;
            CurrentOrientation = ShipOrientation.Horizontal;

            PlaceEnemyFleetRandomly();

            NotifyChanged();
        }

        public void ToggleOrientation()
        {
            CurrentOrientation = (CurrentOrientation == ShipOrientation.Horizontal)
                ? ShipOrientation.Vertical
                : ShipOrientation.Horizontal;

            NotifyChanged();
        }

        public bool TryPlaceNextPlayerShip(int row, int col)
        {
            if (Phase != GamePhase.PlacingShips)
                return false;

            var ship = CurrentShipToPlace;
            if (ship == null)
                return false;

            bool placed = PlayerBoard.PlaceShip(ship, row, col, CurrentOrientation);
            if (!placed)
                return false;

            CurrentPlacementIndex++;

            // Done placing -> player turn
            if (CurrentPlacementIndex >= PlayerShips.Count)
            {
                Phase = GamePhase.PlayerTurn;
            }

            NotifyChanged();
            return true;
        }

        public AttackResult PlayerAttack(int row, int col)
        {
            if (Phase != GamePhase.PlayerTurn)
            {
                return new AttackResult { Row = row, Col = col, IsHit = false, Message = "Not your turn." };
            }

            var result = EnemyBoard.Attack(row, col);

            RefreshSunkFlags(EnemyShips);
            bool enemyAllSunk = EnemyShips.All(s => s.IsSunk);

            if (enemyAllSunk)
            {
                Phase = GamePhase.GameOver;
                result = AttackResultExtensions.WithGameOver(result, "Enemy fleet destroyed. Victory!");
                NotifyChanged();
                return result;
            }

            // Switch to enemy turn only if the attack was valid (not 'Already attacked.')
            if (result.Message != "Already attacked.")
            {
                Phase = GamePhase.EnemyTurn;
            }

            NotifyChanged();
            return result;
        }

        public AttackResult EnemyAttackRandom()
        {
            if (Phase != GamePhase.EnemyTurn)
            {
                return new AttackResult { Row = -1, Col = -1, IsHit = false, Message = "Not enemy turn." };
            }

            // Pick a random untargeted cell
            int row, col;
            int safety = 0;

            do
            {
                row = _rng.Next(0, GameBoard.BoardSize);
                col = _rng.Next(0, GameBoard.BoardSize);
                safety++;
                if (safety > 500) break; // ultra safety
            }
            while (PlayerBoard.GetCell(row, col).HasBeenAttacked);

            var result = PlayerBoard.Attack(row, col);

            RefreshSunkFlags(PlayerShips);
            bool playerAllSunk = PlayerShips.All(s => s.IsSunk);

            if (playerAllSunk)
            {
                Phase = GamePhase.GameOver;
                result = AttackResultExtensions.WithGameOver(result, "Your fleet is gone. Defeat.");
                NotifyChanged();
                return result;
            }

            // Switch back to player after a valid attack
            if (result.Message != "Already attacked.")
            {
                Phase = GamePhase.PlayerTurn;
            }

            NotifyChanged();
            return result;
        }

        private void PlaceEnemyFleetRandomly()
        {
            foreach (var ship in EnemyShips)
            {
                bool placed = false;
                int safety = 0;

                while (!placed && safety < 1000)
                {
                    safety++;

                    var orientation = _rng.Next(0, 2) == 0
                        ? ShipOrientation.Horizontal
                        : ShipOrientation.Vertical;

                    int row = _rng.Next(0, GameBoard.BoardSize);
                    int col = _rng.Next(0, GameBoard.BoardSize);

                    placed = EnemyBoard.PlaceShip(ship, row, col, orientation);
                }
            }
        }

        private static List<ShipBase> CreateStandardFleet()
        {
            return new List<ShipBase>
            {
                new Carrier(),
                new Battleship(),
                new Cruiser(),
                new Submarine(),
                new Destroyer()
            };
        }

        private static void RefreshSunkFlags(List<ShipBase> ships)
        {
            foreach (var s in ships)
                s.RecalculateSunk();
        }

        private void NotifyChanged() => OnChanged?.Invoke();

        private class AttackResultExtensions
        {
            public static AttackResult WithGameOver(AttackResult result, string message)
            {
                return new AttackResult
                {
                    Row = result.Row,
                    Col = result.Col,
                    IsHit = result.IsHit,
                    Message = message,
                    // Add any additional properties if AttackResult has them, e.g. IsGameOver = true
                };
            }
        }

        // tiny helper to “clone” with gameover set (keeps your class simple)
        // (using method instead of record "with" to avoid requiring record types)
    }
}
