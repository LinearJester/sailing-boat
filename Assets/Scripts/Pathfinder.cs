using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Create Pathfinder", fileName = "Pathfinder", order = 0)]
public class Pathfinder : ScriptableObject
{
    public async Task<List<PathNode>> FindPath(PathNode start, PathNode destination, CancellationToken token)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var startNode = new Vector3Int(start.X, start.Y, 0);

        //Debug.Log($"Started pathfinding from ({start.X}, {start.Y}) to ({destination.X}, {destination.Y})...");
        Dictionary<PathNode, PathNode> cameFrom = new Dictionary<PathNode, PathNode>();
        Dictionary<PathNode, int> costSoFar = new Dictionary<PathNode, int>() { { start, 0 } };
        SortedSet<PathNodePriority> priorityQueue = new SortedSet<PathNodePriority>(new PathNodePriorityComparer()) { new(start, 0) };


        while (priorityQueue.Any())
        {
            if (token.IsCancellationRequested)
            {
                //Debug.Log("Cancelling task");
                token.ThrowIfCancellationRequested();
            }

            var min = priorityQueue.Min;
            priorityQueue.Remove(min);

            var current = min.node;
            //var current = hexGrid[(Vector2Int)min];

            //var removed = priorityQueue.Remove(current);
            //Debug.Log($"the node: ({current.X}, {current.Y}) was removed: {removed}");

            //Debug.Log($"Calculating node id ({current.X}, {current.Y}, neighbors count: {current.Neighbors.Count})");

            if (current == destination)
                break;

            foreach (var neighbor in current.Neighbors)
            {
                var neighborPriority = new PathNodePriority(neighbor, 0);
                var containedNeighbor = priorityQueue.Contains(neighborPriority);
                if (containedNeighbor)
                    priorityQueue.Remove(neighborPriority);

                int cost = costSoFar[current] + 1;
                if (!costSoFar.ContainsKey(neighbor))
                {
                    if (!costSoFar.TryAdd(neighbor, cost))
                    {
                        costSoFar[neighbor] = cost;
                    }

                    if (!cameFrom.TryAdd(neighbor, current))
                    {
                        cameFrom[neighbor] = current;
                    }

                    neighborPriority.priority = cost + Heuristic(neighbor, destination);
                    priorityQueue.Add(neighborPriority);
                }
                else
                {
                    if (cost < costSoFar[neighbor])
                    {
                        if (!costSoFar.TryAdd(neighbor, cost))
                        {
                            costSoFar[neighbor] = cost;
                        }

                        if (!cameFrom.TryAdd(neighbor, current))
                        {
                            cameFrom[neighbor] = current;
                        }

                        neighborPriority.priority = cost + Heuristic(neighbor, destination);
                        priorityQueue.Add(neighborPriority);
                    }
                }

                if (containedNeighbor)
                    priorityQueue.Add(neighborPriority);
            }

            //var removed = priorityQueue.Remove(current);
            //Debug.Log($"the node: ({current.X}, {current.Y}) was removed: {removed}");
        }

        //Debug.Log("Path Founded!!!!");

        List<PathNode> res = new List<PathNode> { destination };

        var path = destination;

        while (path != start)
        {
            path = cameFrom[path];
            res.Add(path);
        }

        res.Reverse();

        //Debug.Log("Ended");

        stopwatch.Stop();
        Debug.Log($"Execution Time (milliseconds): {stopwatch.ElapsedMilliseconds}");

        return res;
    }

    public int Heuristic(PathNode a, PathNode b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;

        if (Math.Sign(dx) == Math.Sign(dy))
        {
            return Mathf.Abs(dx + dy);
        }
        else
        {
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
        }
    }
}

//public class PathNodeComparer : IComparer<PathNode>
//{
//    public int Compare(PathNode a, PathNode b)
//    {
//        if (a == null || b == null)
//            return 0;
//        if (a.X == b.X && a.Y == b.Y)
//            return 0;

//        var res = a.Priority.CompareTo(b.Priority);

//        if (res != 0)
//            return res;

//        res = a.X.CompareTo(b.X);
//        if (res != 0)
//            return res;
//        return a.Y.CompareTo(b.Y);
//    }
//}

public struct PathNodePriority
{
    public PathNode node;
    public int priority;

    public PathNodePriority(PathNode node, int priority)
    {
        this.node = node;
        this.priority = priority;
    }
}

public class PathNodePriorityComparer : IComparer<PathNodePriority>
{
    public int Compare(PathNodePriority a, PathNodePriority b)
    {
        if (a.node == b.node)
            return 0;

        var res = a.priority.CompareTo(b.priority);

        return res == 0 ? -1 : res;
    }
}