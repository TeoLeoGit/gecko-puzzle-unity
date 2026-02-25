using System.Collections.Generic;
using UnityEngine;

// public struct GridPoint
// {
//     public int x;
//     public int y;

//     public GridPoint(int x, int y)
//     {
//         this.x = x;
//         this.y = y;
//     }
// }

class NodeAStar
{
    public Vector2Int point;
    public int g; // cost from start
    public int h; // heuristic
    public int f; // g + h
    public NodeAStar parent;

    public NodeAStar(Vector2Int point, int g = 0, int h = 0, NodeAStar parent = null)
    {
        this.point = point;
        this.g = g;
        this.h = h;
        this.f = g + h;
        this.parent = parent;
    }
}

public class AStar
{
    private int[,] grid;
    private int width;
    private int height;

    public AStar(int[,] grid)
    {
        this.grid = grid;
        height = grid.GetLength(0);
        width = grid.GetLength(1);
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<NodeAStar> openList = new List<NodeAStar>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        NodeAStar startNode = new NodeAStar(start);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // Get node with lowest F
            openList.Sort((a, b) => a.f.CompareTo(b.f));
            NodeAStar current = openList[0];
            openList.RemoveAt(0);

            Vector2Int currentKey = new Vector2Int(current.point.x, current.point.y);

            if (current.point.x == end.x && current.point.y == end.y)
            {
                return ReconstructPath(current);
            }

            closedSet.Add(currentKey);

            foreach (Vector2Int neighbor in GetNeighbors(current.point))
            {
                Vector2Int neighborKey = new Vector2Int(neighbor.x, neighbor.y);
                if (closedSet.Contains(neighborKey))
                    continue;

                int newG = current.g + 1;

                NodeAStar existingNode = openList.Find(n =>
                    n.point.x == neighbor.x && n.point.y == neighbor.y);

                if (existingNode == null)
                {
                    int h = Heuristic(neighbor, end);
                    NodeAStar newNode = new NodeAStar(neighbor, newG, h, current);
                    openList.Add(newNode);
                }
                else if (newG < existingNode.g)
                {
                    existingNode.g = newG;
                    existingNode.f = existingNode.g + existingNode.h;
                    existingNode.parent = current;
                }
            }
        }

        return new List<Vector2Int>(); // no path
    }

    private List<Vector2Int> GetNeighbors(Vector2Int p)
    {
        Vector2Int[] directions =
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        List<Vector2Int> result = new List<Vector2Int>();

        foreach (var dir in directions)
        {
            int nx = p.x + dir.x;
            int ny = p.y + dir.y;

            if (nx >= 0 &&
                ny >= 0 &&
                nx < width &&
                ny < height &&
                grid[ny, nx] == 0)
            {
                result.Add(new Vector2Int(nx, ny));
            }
        }

        return result;
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> ReconstructPath(NodeAStar node)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        NodeAStar current = node;

        while (current != null)
        {
            path.Add(current.point);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }
}