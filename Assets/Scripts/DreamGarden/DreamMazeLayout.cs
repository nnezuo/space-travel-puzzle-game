using System;
using System.Collections.Generic;

// Computes passages and the solution route; DreamGardenGenerator builds the scene geometry.
public sealed class DreamMazeLayout
{
    public struct Cell
    {
        public int Row, Col;
        public Cell(int row, int col) { Row = row; Col = col; }
    }

    public int Size { get; private set; }
    public bool[,,] Openings { get; private set; }
    public List<Cell> Route { get; private set; }
    public int Step => (Route.Count - 1) / 4;

    // North, east, south, west: the generator uses the same direction indices.
    private static readonly int[] RowOffsets = { 1, 0, -1, 0 };
    private static readonly int[] ColumnOffsets = { 0, 1, 0, -1 };

    public DreamMazeLayout(int size, int seed)
    {
        if (size < 7 || size > 13) throw new ArgumentOutOfRangeException(nameof(size));
        Size = size;
        var random = new Random(seed);
        bool[,,] bestOpenings = null;
        List<Cell> bestRoute = null;

        for (int attempt = 0; attempt < 128; attempt++)
        {
            CarveMaze(random);
            Route = FindRoute();
            if (bestRoute == null || Route.Count > bestRoute.Count)
            {
                bestRoute = Route;
                bestOpenings = Openings;
            }

            if (Route.Count >= Math.Max(32, size * 4)) break;
        }

        Openings = bestOpenings;
        Route = bestRoute;
    }

    private void CarveMaze(Random random)
    {
        Openings = new bool[Size, Size, 4];
        var visited = new bool[Size, Size];
        var stack = new Stack<Cell>();
        stack.Push(new Cell(0, 0));
        visited[0, 0] = true;

        // Iterative DFS: visiting only new cells creates a spanning tree without cycles.
        while (stack.Count > 0)
        {
            Cell cell = stack.Peek();
            var choices = new List<int>();
            for (int direction = 0; direction < 4; direction++)
            {
                int row = cell.Row + RowOffsets[direction];
                int col = cell.Col + ColumnOffsets[direction];
                if (row >= 0 && row < Size && col >= 0 && col < Size && !visited[row, col])
                    choices.Add(direction);
            }

            if (choices.Count == 0)
            {
                stack.Pop();
                continue;
            }

            // All available neighbours participate in the draw (at most three).
            // Keep this ordering to preserve the layouts produced by existing seeds.
            choices.Sort((a, b) =>
            {
                int firstRow = cell.Row + RowOffsets[a];
                int firstCol = cell.Col + ColumnOffsets[a];
                int secondRow = cell.Row + RowOffsets[b];
                int secondCol = cell.Col + ColumnOffsets[b];
                int firstDistance = firstRow + firstCol;
                int secondDistance = secondRow + secondCol;
                return secondDistance.CompareTo(firstDistance);
            });

            int chosen = choices[random.Next(Math.Min(choices.Count, 3))];
            Cell next = new Cell(cell.Row + RowOffsets[chosen], cell.Col + ColumnOffsets[chosen]);
            Openings[cell.Row, cell.Col, chosen] = true;
            Openings[next.Row, next.Col, (chosen + 2) % 4] = true;
            visited[next.Row, next.Col] = true;
            stack.Push(next);
        }
    }

    private List<Cell> FindRoute()
    {
        // BFS records parents for the entrance-to-exit route used to place the challenges.
        var queue = new Queue<Cell>();
        var seen = new bool[Size, Size];
        var parent = new Cell[Size, Size];
        queue.Enqueue(new Cell(0, 0));
        seen[0, 0] = true;

        while (queue.Count > 0)
        {
            Cell cell = queue.Dequeue();
            for (int direction = 0; direction < 4; direction++)
            {
                if (!Openings[cell.Row, cell.Col, direction]) continue;
                Cell next = new Cell(cell.Row + RowOffsets[direction], cell.Col + ColumnOffsets[direction]);
                if (seen[next.Row, next.Col]) continue;
                seen[next.Row, next.Col] = true;
                parent[next.Row, next.Col] = cell;
                queue.Enqueue(next);
            }
        }

        var result = new List<Cell>();
        Cell point = new Cell(Size - 1, Size - 1);
        while (point.Row != 0 || point.Col != 0)
        {
            result.Add(point);
            point = parent[point.Row, point.Col];
        }
        result.Add(point);
        result.Reverse();
        return result;
    }
}
