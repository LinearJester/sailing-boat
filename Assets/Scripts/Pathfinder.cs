using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

[CreateAssetMenu(menuName = "Create Pathfinder", fileName = "Pathfinder", order = 0)]
public class Pathfinder : ScriptableObject
{

    public async Task<List<PathNode>> FindPath(PathNode start, PathNode destination, CancellationToken token)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        //Debug.Log($"Started pathfinding from ({start.X}, {start.Y}) to ({destination.X}, {destination.Y})...");
        Dictionary<PathNode, PathNode> cameFrom = new Dictionary<PathNode, PathNode>();
        Dictionary<PathNode, int> costSoFar = new Dictionary<PathNode, int>() { { start, 0 } };
        SortedSet<PathNode> priorityQueue = new SortedSet<PathNode>(new PathNodeComparer()) { start };


        while (priorityQueue.Any())
        {
            if (token.IsCancellationRequested)
            {
                //Debug.Log("Cancelling task");
                token.ThrowIfCancellationRequested();
            }


            //var current = priorityQueue.First();
            var current = priorityQueue.Min;

            priorityQueue.Remove(current);
            //var removed = priorityQueue.Remove(current);
            //Debug.Log($"the node: ({current.X}, {current.Y}) was removed: {removed}");

            //Debug.Log($"Calculating node id ({current.X}, {current.Y}, neighbors count: {current.Neighbors.Count})");

            if (current == destination)
                break;

            foreach (var neighbor in current.Neighbors)
            {
                var cointainedNeighbor = priorityQueue.Contains(neighbor);
                if (cointainedNeighbor)
                    priorityQueue.Remove(neighbor);

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

                    neighbor.Priority = cost + Heuristic(neighbor, destination);
                    priorityQueue.Add(neighbor);
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

                        neighbor.Priority = cost + Heuristic(neighbor, destination);
                        priorityQueue.Add(neighbor);
                    }
                }

                if (cointainedNeighbor)
                    priorityQueue.Add(neighbor);
            }

            //var removed = priorityQueue.Remove(current);
            //Debug.Log($"the node: ({current.X}, {current.Y}) was removed: {removed}");
        }

        //Debug.Log("Path Founded!!!!");

        List<PathNode> res = new List<PathNode>();
        res.Add(destination);

        var path = destination;

        while (path != start)
        {
            path = cameFrom[path];
            res.Add(path);
        }

        res.Reverse();

        //Debug.Log("Ended");

        var time = stopwatch.ElapsedTicks;
        stopwatch.Stop();
        Debug.Log($"Execution Time: {stopwatch.ElapsedMilliseconds}");

        stopwatch.Stop();

        return res;
    }

    public void DebugDraw(List<PathNode> path)
    {

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

public class PathNodeComparer : IComparer<PathNode>
{
    public int Compare(PathNode a, PathNode b)
    {
        if (a == null || b == null)
            return 0;
        if (a.X == b.X && a.Y == b.Y)
            return 0;

        var res = a.Priority.CompareTo(b.Priority);

        if (res != 0)
            return res;

        res = a.X.CompareTo(b.X);
        if (res != 0)
            return res;
        return a.Y.CompareTo(b.Y);
    }
}

public class PathNodePairComparer : IComparer<KeyValuePair<PathNode, int>>
{
    public int Compare(KeyValuePair<PathNode, int> a, KeyValuePair<PathNode, int> b)
    {
        if (a.Key.X == b.Key.X && a.Key.Y == b.Key.Y)
            return 0;

        var res = a.Value.CompareTo(b.Value);

        if (res != 0)
            return res;

        res = a.Key.X.CompareTo(b.Key.X);
        if (res != 0)
            return res;
        return a.Key.Y.CompareTo(b.Key.Y);
    }
}