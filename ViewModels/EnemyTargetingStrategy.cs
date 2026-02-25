using Battleship.GameCore;

namespace BattleshipMaui.ViewModels;

public sealed class EnemyTargetingStrategy
{
    private readonly int _size;
    private readonly Queue<BoardCoordinate> _huntQueue;
    private readonly LinkedList<BoardCoordinate> _targetQueue = new();
    private readonly HashSet<BoardCoordinate> _attempted = new();
    private readonly List<BoardCoordinate> _activeHits = new();

    public int PendingTargetCount => _targetQueue.Count;
    public int AttemptedCount => _attempted.Count;
    public int RemainingShots => (_size * _size) - _attempted.Count;

    public EnemyTargetingStrategy(int size, Random random)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Board size must be greater than zero.");

        _size = size;
        random ??= new Random();

        var parityCells = new List<BoardCoordinate>();
        var nonParityCells = new List<BoardCoordinate>();

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var coordinate = new BoardCoordinate(row, col);
                if ((row + col) % 2 == 0)
                    parityCells.Add(coordinate);
                else
                    nonParityCells.Add(coordinate);
            }
        }

        Shuffle(parityCells, random);
        Shuffle(nonParityCells, random);

        _huntQueue = new Queue<BoardCoordinate>(parityCells.Concat(nonParityCells));
    }

    public BoardCoordinate GetNextShot()
    {
        while (_targetQueue.Count > 0)
        {
            var candidate = _targetQueue.First!.Value;
            _targetQueue.RemoveFirst();

            if (_attempted.Add(candidate))
                return candidate;
        }

        while (_huntQueue.Count > 0)
        {
            var candidate = _huntQueue.Dequeue();
            if (_attempted.Add(candidate))
                return candidate;
        }

        throw new InvalidOperationException("No remaining shots.");
    }

    public void RegisterShotOutcome(BoardCoordinate shot, AttackResult result)
    {
        _attempted.Add(shot);

        switch (result)
        {
            case AttackResult.Sunk:
                _activeHits.Clear();
                _targetQueue.Clear();
                return;

            case AttackResult.Hit:
                if (!_activeHits.Contains(shot))
                    _activeHits.Add(shot);

                RebuildTargetQueue();
                return;

            default:
                return;
        }
    }

    private void RebuildTargetQueue()
    {
        _targetQueue.Clear();

        if (_activeHits.Count == 0)
            return;

        if (_activeHits.Count == 1)
        {
            AddAdjacentCandidates(_activeHits[0]);
            return;
        }

        bool sameRow = _activeHits.All(h => h.Row == _activeHits[0].Row);
        bool sameCol = _activeHits.All(h => h.Col == _activeHits[0].Col);

        if (sameRow)
        {
            int row = _activeHits[0].Row;
            int minCol = _activeHits.Min(h => h.Col);
            int maxCol = _activeHits.Max(h => h.Col);

            AddCandidate(row, minCol - 1);
            AddCandidate(row, maxCol + 1);
        }
        else if (sameCol)
        {
            int col = _activeHits[0].Col;
            int minRow = _activeHits.Min(h => h.Row);
            int maxRow = _activeHits.Max(h => h.Row);

            AddCandidate(minRow - 1, col);
            AddCandidate(maxRow + 1, col);
        }
        else
        {
            foreach (var hit in _activeHits)
                AddAdjacentCandidates(hit);
        }

        if (_targetQueue.Count == 0)
        {
            foreach (var hit in _activeHits)
                AddAdjacentCandidates(hit);
        }
    }

    private void AddAdjacentCandidates(BoardCoordinate hit)
    {
        AddCandidate(hit.Row - 1, hit.Col);
        AddCandidate(hit.Row + 1, hit.Col);
        AddCandidate(hit.Row, hit.Col - 1);
        AddCandidate(hit.Row, hit.Col + 1);
    }

    private void AddCandidate(int row, int col)
    {
        var candidate = new BoardCoordinate(row, col);
        if (!InBounds(candidate))
            return;

        if (_attempted.Contains(candidate))
            return;

        if (_targetQueue.Contains(candidate))
            return;

        _targetQueue.AddLast(candidate);
    }

    private bool InBounds(BoardCoordinate coordinate)
    {
        return coordinate.Row >= 0 &&
               coordinate.Row < _size &&
               coordinate.Col >= 0 &&
               coordinate.Col < _size;
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
