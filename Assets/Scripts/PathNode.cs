using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PathNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler//, IComparable<PathNode>
{
    public TextMeshProUGUI TextMeshPro;
    public Color highlightColor;
    public int X { get; set; }
    public int Y { get; set; }

    public List<PathNode> Neighbors { get; private set; } = new List<PathNode>();
    public int Priority { get; set; } = 0;

    private Material _material;
    private Color _defaultColor;
    private bool _highlighted = false;
    private void Awake()
    {
        _material = GetComponentInChildren<MeshRenderer>().material;
        _defaultColor = _material.color;
    }

    private void Update()
    {
        //TextMeshPro.text = $"({Priority})";
        TextMeshPro.text = $"({X}, {Y}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Clicked on: ({X}, {Y})");
        FindFirstObjectByType<PathDebug>().Click(this);
    }

    public void Highlight()
    {
        _material.color = _highlighted ? _defaultColor : highlightColor;
        _highlighted = !_highlighted;
    }

    //public int CompareTo(PathNode other)
    //{
    //    if (X == other.X && Y == other.Y) return 0;

    //    var res = Priority.CompareTo(other.Priority);
    //    if (res != 0)
    //        return res;
    //    res = X.CompareTo(other.X);
    //    if (res != 0)
    //        return res;
    //    return Y.CompareTo(other.Y);
    //}
    public void OnPointerEnter(PointerEventData eventData)
    {
        Highlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Highlight();
    }
}