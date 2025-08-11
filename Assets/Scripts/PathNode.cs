using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PathNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler//, IComparable<PathNode>
{
    public Color highlightColor;

    public Vector2Int Coordinates { get; set; }
    public int X => Coordinates.x;
    public int Y => Coordinates.y;

    public event Action<PathNode> Clicked;
    public List<PathNode> Neighbors { get; private set; } = new List<PathNode>();
    
    private Material _material;
    private Color _defaultColor;
    private bool _highlighted = false;
    public bool KeepHighlighted { get; set; } = false;
    private void Awake()
    {
        _material = GetComponentInChildren<MeshRenderer>().material;
        _defaultColor = _material.color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(this);
    }

    public void Highlight(bool active)
    {
        _material.color = active ? highlightColor : _defaultColor;
        _highlighted = active;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Highlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!KeepHighlighted)
            Highlight(false);
    }
}