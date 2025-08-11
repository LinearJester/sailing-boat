using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public class PathDebug : MonoBehaviour
{
    public Pathfinder pathfinder;

    private PathNode _a;
    private PathNode _b;

    private CancellationTokenSource _cts = new CancellationTokenSource();

    public void Click(PathNode node)
    {
        //FindFirstObjectByType<Boat>().GoTo(node);
        //if (_a == null)
        //    _a = node;
        //else if (_b == null)
        //{
        //    _b = node;
        //    //await DisplayPath();
        //    FindFirstObjectByType<Boat>().GoTo(node);
        //}
        //else
        //{
        //    _a = null;
        //    _b = null;
        //}
    }

    public async Awaitable DisplayPath()
    {
        if (_a == null || _b == null)
            return;

        var task = Task.Run(() => pathfinder.FindPath(_a, _b, _cts.Token));

        await task;

        var path = task.Result;

        foreach (var pathNode in path)
        {
            pathNode.Highlight(true);
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawLine(path[i].transform.position, path[i + 1].transform.position, Color.green, 10);
        }
    }

    private async Awaitable Delay(float time)
    {
        await Awaitable.WaitForSecondsAsync(10);
    }

    private void OnDestroy()
    {
        _cts.Cancel();
    }
}