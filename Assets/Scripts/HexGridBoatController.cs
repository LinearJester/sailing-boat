using System.Collections.Generic;
using UnityEngine;

public class HexGridBoatController : MonoBehaviour
{
    private List<PathNode> _registeredNodes = new List<PathNode>();
    public Boat Boat { get; set; }
    public Pathfinder pathfinder;

    public void RegisterNodes(List<PathNode> pathNodes)
    {
        foreach (var pathNode in pathNodes)
        {
            pathNode.Clicked += OnPathNodeClicked;
        }

        _registeredNodes = pathNodes;
    }

    public void UnregisterNodes()
    {
        foreach (var pathNode in _registeredNodes)
        {
            pathNode.Clicked -= OnPathNodeClicked;
        }
    }

    private void OnPathNodeClicked(PathNode node)
    {
        if (Boat != null)
        {
            Boat.GoTo(node);
        }
    }

    private void OnDestroy()
    {
        UnregisterNodes();
    }
}