using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Boat : MonoBehaviour
{
    public Pathfinder pathfinder;
    public float speed;
    public float rotationSpeed;
    public LayerMask waterLayer;
    public bool showPath;
    private bool _moveToTarget;
    private bool _waitForMovementCancelation;
    private CancellationTokenSource _cts = new CancellationTokenSource();

    private void Awake()
    {
        _moveToTarget = false;
        _waitForMovementCancelation = false;
    }

    public async Task GoTo(PathNode destination)
    {
        StopMovement();

        if (_waitForMovementCancelation)
            return;
        var origin = transform.position + Vector3.up * 5;
        var ray = new Ray(origin, Vector3.down);
        //var hit = Physics.Raycast(ray, out var hitInfo, 7, LayerMask.NameToLayer("Water"), QueryTriggerInteraction.Collide);
        var hit = Physics.RaycastAll(origin, Vector3.down, 7, waterLayer, QueryTriggerInteraction.Collide);
        //Debug.Log($"Hitted: {hit}");
        var current = hit[0].transform.GetComponent<PathNode>();
        var handle = Task.Run(() => pathfinder.FindPath(current, destination, _cts.Token));
        await handle;

        var path = handle.Result;
        handle.Dispose();

        if (showPath)
        {
            foreach (var pathNode in path)
            {
                pathNode.KeepHighlighted = true;
                pathNode.Highlight(true);
            }
        }

        _moveToTarget = true;
        await Move(path);

        foreach (var pathNode in path)
        {
            pathNode.KeepHighlighted = false;
            pathNode.Highlight(false);
        }
        _waitForMovementCancelation = false;
        _moveToTarget = false;
    }

    public void StopMovement()
    {
        if (!_moveToTarget)
            return;
        _moveToTarget = false;
        _waitForMovementCancelation = true;
    }

    public async Awaitable Move(List<PathNode> path)
    {
        int i = 0;

        while (i < path.Count)
        {
            var targetPos = path[i].transform.position;
            var position = transform.position;
            targetPos.y = position.y;

            var targetRotation = Quaternion.LookRotation(targetPos - position, Vector3.up);
            while (Quaternion.Angle(targetRotation, transform.rotation) > 0.1f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                await Awaitable.EndOfFrameAsync();
            }

            position = Vector3.MoveTowards(position, targetPos, speed * Time.deltaTime);
            transform.position = position;

            await Awaitable.EndOfFrameAsync();

            if (Vector3.SqrMagnitude(transform.position - targetPos) < 0.001f)
            {
                path[i].KeepHighlighted = false;
                path[i].Highlight(false);
                ++i;

                if (_waitForMovementCancelation)
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        _cts.Cancel();
    }
}