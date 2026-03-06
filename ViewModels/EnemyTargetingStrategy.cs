using Battleship.GameCore;

namespace BattleshipMaui.ViewModels;

public sealed class EnemyTargetingStrategy
{
    private static readonly int[] DefaultShipLengths = [5, 4, 3, 3, 2];

    private readonly int _size;
    private readonly Random _random;
    private readonly CpuDifficulty _difficulty;
    private readonly LinkedList<BoardCoordinate> _targetQueue = new();
    private readonly HashSet<BoardCoordinate> _attempted = new();
    private readonly HashSet<BoardCoordinate> _misses = new();
    private readonly HashSet<BoardCoordinate> _confirmedSunkCells = new();
    private readonly HashSet<BoardCoordinate> _activeHitSet = new();
    private readonly List<BoardCoordinate> _activeHits = new();
    private readonly List<int> _remainingShipLengths;
    private int _easyFocusTurnsRemaining;

    public int PendingTargetCount => _targetQueue.Count;
    public int AttemptedCount => _attempted.Count;
    public int RemainingShots => (_size * _size) - _attempted.Count;

    public EnemyTargetingStrategy(
        int size,
        Random random,
        CpuDifficulty difficulty = CpuDifficulty.Standard,
        IEnumerable<int>? remainingShipLengths = null)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Board size must be greater than zero.");

        _size = size;
        _difficulty = difficulty;
        _random = random ?? new Random();
        _remainingShipLengths = (remainingShipLengths ?? DefaultShipLengths)
            .Where(length => length > 1)
            .OrderByDescending(length => length)
            .ToList();
    }

    public void PrimeFromBoard(GameBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        _attempted.Clear();
        _misses.Clear();
        _confirmedSunkCells.Clear();
        _activeHitSet.Clear();
        _activeHits.Clear();
        _targetQueue.Clear();
        _easyFocusTurnsRemaining = 0;

        _remainingShipLengths.Clear();
        _remainingShipLengths.AddRange(board.Fleet
            .Where(ship => !ship.IsSunk)
            .Select(ship => ship.Size)
            .OrderByDescending(length => length));

        if (_remainingShipLengths.Count == 0 && !board.AllShipsSunk)
            _remainingShipLengths.AddRange(DefaultShipLengths);

        for (int row = 0; row < board.Size; row++)
        {
            for (int col = 0; col < board.Size; col++)
            {
                var cell = board.Cells[row, col];
                if (!cell.HasBeenAttacked)
                    continue;

                var coordinate = new BoardCoordinate(row, col);
                _attempted.Add(coordinate);

                if (cell.Ship is null)
                    _misses.Add(coordinate);
            }
        }

        foreach (var ship in board.Fleet)
        {
            if (!ship.IsPlaced)
                continue;

            if (ship.IsSunk)
            {
                foreach (var position in ship.Positions)
                    _confirmedSunkCells.Add(new BoardCoordinate(position.Row, position.Col));

                continue;
            }

            foreach (var position in ship.Positions)
            {
                var cell = board.Cells[position.Row, position.Col];
                if (!cell.HasBeenAttacked)
                    continue;

                AddActiveHit(new BoardCoordinate(position.Row, position.Col));
            }
        }

        if (_difficulty == CpuDifficulty.Easy && _activeHits.Count > 0)
            _easyFocusTurnsRemaining = 2;

        RebuildTargetQueue();
    }

    public BoardCoordinate GetNextShot()
    {
        bool mustUseTargetQueue = CountUnattemptedCells() == _targetQueue.Count && _targetQueue.Count > 0;
        if ((mustUseTargetQueue || ShouldUseTargetQueue()) && TryDequeueTargetShot(out var target))
        {
            if (_difficulty == CpuDifficulty.Easy && _easyFocusTurnsRemaining > 0)
                _easyFocusTurnsRemaining--;

            return target;
        }

        if (TryGetBestHuntShot(out var huntShot))
        {
            _attempted.Add(huntShot);
            return huntShot;
        }

        if (TryDequeueTargetShot(out target))
            return target;

        throw new InvalidOperationException("No remaining shots.");
    }

    public void RegisterShotOutcome(BoardCoordinate shot, AttackResult result, int? sunkShipSize = null)
    {
        _attempted.Add(shot);

        switch (result)
        {
            case AttackResult.Miss:
            case AttackResult.Invalid:
            case AttackResult.AlreadyTried:
                _misses.Add(shot);
                return;

            case AttackResult.Hit:
                AddActiveHit(shot);
                if (_difficulty == CpuDifficulty.Easy)
                    _easyFocusTurnsRemaining = 2;

                RebuildTargetQueue();
                return;

            case AttackResult.Sunk:
                AddActiveHit(shot);
                MarkResolvedClusterAsSunk(shot, sunkShipSize);
                _easyFocusTurnsRemaining = 0;
                RebuildTargetQueue();
                return;
        }
    }

    private bool TryGetBestHuntShot(out BoardCoordinate shot)
    {
        var scores = BuildHuntScores();
        if (scores.Count == 0)
        {
            for (int row = 0; row < _size; row++)
            {
                for (int col = 0; col < _size; col++)
                {
                    var fallback = new BoardCoordinate(row, col);
                    if (_attempted.Contains(fallback))
                        continue;

                    shot = fallback;
                    return true;
                }
            }

            shot = default;
            return false;
        }

        double bestScore = scores.Values.Max();
        var orderedCandidates = scores
            .OrderByDescending(pair => pair.Value)
            .ThenByDescending(pair => ComputeCenterBias(pair.Key))
            .Select(pair => pair.Key)
            .ToList();

        var topCandidates = orderedCandidates
            .Where(candidate => Math.Abs(scores[candidate] - bestScore) < 0.0001)
            .ToList();

        if (_difficulty == CpuDifficulty.Hard)
        {
            shot = topCandidates[_random.Next(topCandidates.Count)];
            return true;
        }

        int easyWindow = Math.Min(8, orderedCandidates.Count);
        int standardWindow = Math.Min(4, orderedCandidates.Count);
        int selectionWindow = _difficulty == CpuDifficulty.Easy ? easyWindow : standardWindow;
        selectionWindow = Math.Max(selectionWindow, topCandidates.Count);

        shot = orderedCandidates[_random.Next(selectionWindow)];
        return true;
    }

    private Dictionary<BoardCoordinate, double> BuildHuntScores()
    {
        var scores = new Dictionary<BoardCoordinate, double>();
        IEnumerable<int> shipLengths = _remainingShipLengths.Count > 0
            ? _remainingShipLengths
            : DefaultShipLengths;

        foreach (int shipLength in shipLengths)
        {
            for (int row = 0; row < _size; row++)
            {
                for (int col = 0; col <= _size - shipLength; col++)
                {
                    if (!TryScorePlacement(row, col, 0, 1, shipLength, scores))
                        continue;
                }
            }

            for (int row = 0; row <= _size - shipLength; row++)
            {
                for (int col = 0; col < _size; col++)
                {
                    if (!TryScorePlacement(row, col, 1, 0, shipLength, scores))
                        continue;
                }
            }
        }

        return scores;
    }

    private bool TryScorePlacement(
        int startRow,
        int startCol,
        int rowDelta,
        int colDelta,
        int shipLength,
        Dictionary<BoardCoordinate, double> scores)
    {
        Span<BoardCoordinate> placement = stackalloc BoardCoordinate[shipLength];
        for (int i = 0; i < shipLength; i++)
        {
            var coordinate = new BoardCoordinate(startRow + (rowDelta * i), startCol + (colDelta * i));
            if (!InBounds(coordinate) || _attempted.Contains(coordinate) || _confirmedSunkCells.Contains(coordinate))
                return false;

            placement[i] = coordinate;
        }

        double placementWeight = shipLength + (_difficulty == CpuDifficulty.Hard ? 0.8 : 0.35);
        foreach (var coordinate in placement)
        {
            double score = placementWeight + ComputeCenterBias(coordinate);
            if (_difficulty == CpuDifficulty.Hard && IsParityCell(coordinate))
                score += 0.35;
            else if (_difficulty == CpuDifficulty.Easy && IsParityCell(coordinate))
                score += 0.08;

            scores[coordinate] = scores.TryGetValue(coordinate, out var existing)
                ? existing + score
                : score;
        }

        return true;
    }

    private void RebuildTargetQueue()
    {
        _targetQueue.Clear();

        var clusters = BuildActiveHitClusters();
        foreach (var cluster in clusters.OrderByDescending(candidate => candidate.Count))
            AddClusterCandidates(cluster);
    }

    private IReadOnlyList<List<BoardCoordinate>> BuildActiveHitClusters()
    {
        var clusters = new List<List<BoardCoordinate>>();
        var visited = new HashSet<BoardCoordinate>();

        foreach (var hit in _activeHits)
        {
            if (!visited.Add(hit))
                continue;

            var cluster = new List<BoardCoordinate>();
            var queue = new Queue<BoardCoordinate>();
            queue.Enqueue(hit);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cluster.Add(current);

                foreach (var neighbor in GetOrthogonalNeighbors(current))
                {
                    if (!_activeHitSet.Contains(neighbor) || !visited.Add(neighbor))
                        continue;

                    queue.Enqueue(neighbor);
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    private void AddClusterCandidates(List<BoardCoordinate> cluster)
    {
        if (cluster.Count == 0)
            return;

        if (cluster.Count == 1)
        {
            if (_difficulty == CpuDifficulty.Hard)
                AddAdjacentCandidatesByReach(cluster[0]);
            else
                AddAdjacentCandidates(cluster[0]);

            return;
        }

        bool sameRow = cluster.All(hit => hit.Row == cluster[0].Row);
        bool sameCol = cluster.All(hit => hit.Col == cluster[0].Col);
        var clusterSet = cluster.ToHashSet();

        if (sameRow)
        {
            int row = cluster[0].Row;
            int minCol = cluster.Min(hit => hit.Col);
            int maxCol = cluster.Max(hit => hit.Col);
            AddLineExtensionCandidates(new BoardCoordinate(row, minCol), 0, -1, clusterSet);
            AddLineExtensionCandidates(new BoardCoordinate(row, maxCol), 0, 1, clusterSet);
            return;
        }

        if (sameCol)
        {
            int col = cluster[0].Col;
            int minRow = cluster.Min(hit => hit.Row);
            int maxRow = cluster.Max(hit => hit.Row);
            AddLineExtensionCandidates(new BoardCoordinate(minRow, col), -1, 0, clusterSet);
            AddLineExtensionCandidates(new BoardCoordinate(maxRow, col), 1, 0, clusterSet);
            return;
        }

        foreach (var hit in cluster)
        {
            if (_difficulty == CpuDifficulty.Hard)
                AddAdjacentCandidatesByReach(hit);
            else
                AddAdjacentCandidates(hit);
        }
    }

    private void AddLineExtensionCandidates(
        BoardCoordinate anchor,
        int rowDelta,
        int colDelta,
        HashSet<BoardCoordinate> cluster)
    {
        var candidate = BuildLineExtensionCandidate(anchor, rowDelta, colDelta, cluster);
        if (candidate.Reach < 0)
            return;

        AddCandidate(candidate.Row, candidate.Col);
    }

    private void AddAdjacentCandidates(BoardCoordinate hit)
    {
        var candidates = GetOrthogonalNeighbors(hit).ToList();
        Shuffle(candidates, _random);

        foreach (var candidate in candidates)
            AddCandidate(candidate.Row, candidate.Col);
    }

    private IEnumerable<BoardCoordinate> GetOrthogonalNeighbors(BoardCoordinate coordinate)
    {
        yield return new BoardCoordinate(coordinate.Row - 1, coordinate.Col);
        yield return new BoardCoordinate(coordinate.Row + 1, coordinate.Col);
        yield return new BoardCoordinate(coordinate.Row, coordinate.Col - 1);
        yield return new BoardCoordinate(coordinate.Row, coordinate.Col + 1);
    }

    private void AddCandidate(int row, int col)
    {
        var candidate = new BoardCoordinate(row, col);
        if (!InBounds(candidate))
            return;

        if (_attempted.Contains(candidate))
            return;

        if (_confirmedSunkCells.Contains(candidate))
            return;

        if (_targetQueue.Contains(candidate))
            return;

        _targetQueue.AddLast(candidate);
    }

    private void AddAdjacentCandidatesByReach(BoardCoordinate hit)
    {
        var rankedDirections = new List<(int Row, int Col, int Reach)>
        {
            BuildDirectionalCandidate(hit, -1, 0),
            BuildDirectionalCandidate(hit, 1, 0),
            BuildDirectionalCandidate(hit, 0, -1),
            BuildDirectionalCandidate(hit, 0, 1)
        };

        foreach (var candidate in rankedDirections
                     .Where(direction => direction.Reach >= 0)
                     .OrderByDescending(direction => direction.Reach)
                     .ThenBy(_ => _random.Next()))
        {
            AddCandidate(candidate.Row, candidate.Col);
        }
    }

    private (int Row, int Col, int Reach) BuildDirectionalCandidate(BoardCoordinate hit, int rowDelta, int colDelta)
    {
        int row = hit.Row + rowDelta;
        int col = hit.Col + colDelta;
        var candidate = new BoardCoordinate(row, col);
        if (!InBounds(candidate) || _attempted.Contains(candidate) || _confirmedSunkCells.Contains(candidate))
            return (row, col, -1);

        int reach = 0;
        int scanRow = row;
        int scanCol = col;
        while (InBounds(new BoardCoordinate(scanRow, scanCol)))
        {
            var scan = new BoardCoordinate(scanRow, scanCol);
            if (_attempted.Contains(scan) || _confirmedSunkCells.Contains(scan))
                break;

            reach++;
            scanRow += rowDelta;
            scanCol += colDelta;
        }

        return (row, col, reach);
    }

    private (int Row, int Col, int Reach) BuildLineExtensionCandidate(
        BoardCoordinate anchor,
        int rowDelta,
        int colDelta,
        HashSet<BoardCoordinate> cluster)
    {
        int row = anchor.Row + rowDelta;
        int col = anchor.Col + colDelta;
        var candidate = new BoardCoordinate(row, col);
        if (!InBounds(candidate) || IsBlockedForLineExtension(candidate, cluster))
            return (row, col, -1);

        int reach = 0;
        int scanRow = row;
        int scanCol = col;
        while (InBounds(new BoardCoordinate(scanRow, scanCol)))
        {
            var scan = new BoardCoordinate(scanRow, scanCol);
            if (IsBlockedForLineExtension(scan, cluster))
                break;

            reach++;
            scanRow += rowDelta;
            scanCol += colDelta;
        }

        return (row, col, reach);
    }

    private bool IsBlockedForLineExtension(BoardCoordinate coordinate, HashSet<BoardCoordinate> cluster)
    {
        return _confirmedSunkCells.Contains(coordinate) ||
               (_attempted.Contains(coordinate) && !cluster.Contains(coordinate));
    }

    private void AddActiveHit(BoardCoordinate shot)
    {
        if (_confirmedSunkCells.Contains(shot) || !_activeHitSet.Add(shot))
            return;

        _activeHits.Add(shot);
    }

    private void MarkResolvedClusterAsSunk(BoardCoordinate shot, int? sunkShipSize)
    {
        var cluster = FindConnectedActiveCluster(shot);
        foreach (var coordinate in cluster)
        {
            _activeHitSet.Remove(coordinate);
            _activeHits.Remove(coordinate);
            _confirmedSunkCells.Add(coordinate);
        }

        int resolvedLength = sunkShipSize ?? cluster.Count;
        if (resolvedLength > 1)
        {
            int index = _remainingShipLengths.IndexOf(resolvedLength);
            if (index >= 0)
                _remainingShipLengths.RemoveAt(index);
        }
    }

    private List<BoardCoordinate> FindConnectedActiveCluster(BoardCoordinate origin)
    {
        var cluster = new List<BoardCoordinate>();
        if (!_activeHitSet.Contains(origin))
            return cluster;

        var visited = new HashSet<BoardCoordinate> { origin };
        var queue = new Queue<BoardCoordinate>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            cluster.Add(current);

            foreach (var neighbor in GetOrthogonalNeighbors(current))
            {
                if (!_activeHitSet.Contains(neighbor) || !visited.Add(neighbor))
                    continue;

                queue.Enqueue(neighbor);
            }
        }

        return cluster;
    }

    private bool ShouldUseTargetQueue()
    {
        if (_targetQueue.Count == 0)
            return false;

        if (_difficulty == CpuDifficulty.Hard)
            return true;

        if (_difficulty == CpuDifficulty.Standard)
            return _targetQueue.Count >= 2 || _activeHits.Count > 0 || _random.NextDouble() < 0.92;

        if (_easyFocusTurnsRemaining > 0)
            return true;

        return _random.NextDouble() < 0.55;
    }

    private bool TryDequeueTargetShot(out BoardCoordinate shot)
    {
        while (_targetQueue.Count > 0)
        {
            var candidate = _targetQueue.First!.Value;
            _targetQueue.RemoveFirst();

            if (_attempted.Add(candidate))
            {
                shot = candidate;
                return true;
            }
        }

        shot = default;
        return false;
    }

    private double ComputeCenterBias(BoardCoordinate coordinate)
    {
        double center = (_size - 1) / 2d;
        double distance = Math.Abs(coordinate.Row - center) + Math.Abs(coordinate.Col - center);
        double maxDistance = center * 2d;
        double normalized = maxDistance <= 0 ? 1d : 1d - (distance / maxDistance);

        return _difficulty switch
        {
            CpuDifficulty.Hard => 0.95 + (normalized * 1.8),
            CpuDifficulty.Easy => 0.3 + (normalized * 0.65),
            _ => 0.55 + (normalized * 1.15)
        };
    }

    private bool IsParityCell(BoardCoordinate coordinate) => ((coordinate.Row + coordinate.Col) & 1) == 0;

    private int CountUnattemptedCells() => (_size * _size) - _attempted.Count;

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
