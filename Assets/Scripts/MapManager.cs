using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

public class MapManager : MonoBehaviour
{
    public Vector3 tileSize;
    public Vector2 backgroundSize; [Range(0, 100)]
    public float environmentDensity;

    public Vector2Int boatStartPos;
    public CameraFollow cameraFollow;
    public Pathfinder pathfinder;
    public HexGridBoatController gridBoatController;

    public string mapKey;
    public string tilesKey;
    public string boatKey;
    public string waterKey;
    public string backgroundKey;
    public string environmentKey;

    private TextAsset _mapTextAsset;

    private List<GameObject> _tilesPrefabs = new List<GameObject>();
    //private List<GameObject> _waterPrefabs = new List<GameObject>();
    private GameObject _waterPrefab;
    private GameObject _boatPrefab;
    private List<GameObject> _backgroundPrefabs = new List<GameObject>();
    private List<GameObject> _environmentPrefabs = new List<GameObject>();

    private List<Vector3> _tilesPositions = new List<Vector3>();
    private List<Vector3> _backgroundPositions = new List<Vector3>();
    private Vector2 _mapSize;

    private Dictionary<Vector2Int, Vector3> _hexMapPositions = new Dictionary<Vector2Int, Vector3>();
    private async void Start()
    {
        var loadTask = LoadAssets();

        Debug.Log("started loading map");
        await LoadMap();
        Debug.Log("ended loading map");

        await loadTask;

        loadTask.Dispose();

        GenerateMap();
    }
    private async Awaitable LoadMap()
    {
        _hexMapPositions.Clear();
        _tilesPositions.Clear();
        _backgroundPositions.Clear();

        _mapSize = Vector2.zero;

        var handle = Addressables.LoadAssetAsync<TextAsset>(mapKey);
        await handle.Task;

        _mapTextAsset = handle.Result;
        handle.Release();

        float offsetX = 0;
        float offsetZ = 0;
        bool hexagonalOffset = false;

        int gridHeightX = 0;
        int gridHeightY = 0;

        var map = _mapTextAsset.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        gridHeightX = map[0].Length;
        gridHeightY = map.Length;

        int middleIndexX = gridHeightX / 2;
        int middleIndexY = gridHeightY / 2;

        int hexRowStartingIndex = -middleIndexX - ((middleIndexY - 1) / 2);
        int hexColumnStartingIndex = -middleIndexY;

        for (int i = 0; i < map.Length; ++i)
        {
            hexRowStartingIndex = (-middleIndexX + ((middleIndexY - 1) / 2)) - (i / 2);
            for (int j = 0; j < map[i].Length; ++j)
            {
                Vector3 pos = new Vector3(offsetX, 0, offsetZ);
                if (map[i][j] == '0')
                {
                    Vector2Int hexPos = new Vector2Int(hexRowStartingIndex + j, hexColumnStartingIndex + i);

                    _hexMapPositions.Add(hexPos, pos);
                }
                else
                {
                    _tilesPositions.Add(pos);
                }
                offsetX += tileSize.x;
            }

            if (offsetX > _mapSize.x)
            {
                _mapSize.x = offsetX;
            }
            offsetX = hexagonalOffset ? 0 : tileSize.x / 2.0f;
            offsetZ -= tileSize.z;
            hexagonalOffset = !hexagonalOffset;


        }

        _mapSize.y = Mathf.Abs(offsetZ);

        Vector2 backgroundTilesAmount = new Vector2((_mapSize.x / backgroundSize.x) + 1, (_mapSize.y / backgroundSize.y) + 1);

        offsetX = 0;
        offsetZ = 0;

        for (int i = 0; i < backgroundTilesAmount.x; i++)
        {
            for (int j = 0; j < backgroundTilesAmount.y; j++)
            {
                _backgroundPositions.Add(new Vector3(offsetX + (i * backgroundSize.x), 0, offsetZ - (j * backgroundSize.y)));
            }
        }

        _mapTextAsset = null;
    }

    private async Task LoadAssets()
    {
        Debug.Log("started loading assets");

        var waterHandle = Addressables.LoadAssetAsync<GameObject>(waterKey);
        var boatHandle = Addressables.LoadAssetAsync<GameObject>(boatKey);
        var tileHandle = Addressables.LoadAssetsAsync<GameObject>(tilesKey);
        var backgroundHandle = Addressables.LoadAssetsAsync<GameObject>(backgroundKey);
        var environmentHandle = Addressables.LoadAssetsAsync<GameObject>(environmentKey);

        await Task.WhenAll(waterHandle.Task, boatHandle.Task, backgroundHandle.Task, tileHandle.Task, environmentHandle.Task);

        _waterPrefab = waterHandle.Result;
        _boatPrefab = boatHandle.Result;
        _tilesPrefabs = tileHandle.Result.ToList();
        _backgroundPrefabs = backgroundHandle.Result.ToList();
        _environmentPrefabs = environmentHandle.Result.ToList();

        waterHandle.Release();
        boatHandle.Release();
        backgroundHandle.Release();
        tileHandle.Release();
        environmentHandle.Release();

        Debug.Log("ended loading assets");
    }

    private async void GenerateMap()
    {
        List<AsyncInstantiateOperation<GameObject>> operations = new List<AsyncInstantiateOperation<GameObject>>();

        Dictionary<int, List<Vector3>> positions = new Dictionary<int, List<Vector3>>();

        var waterHandle = InstantiateAsync(_waterPrefab, _hexMapPositions.Count, transform);

        if (!_hexMapPositions.ContainsKey(boatStartPos))
            boatStartPos = _hexMapPositions.Keys.First();

        var boatHandle = InstantiateAsync(_boatPrefab, _hexMapPositions[boatStartPos], Quaternion.identity);

        for (int i = 0; i < _tilesPrefabs.Count; i++)
        {
            if (!positions.TryAdd(i, new List<Vector3>()))
            {
                positions[i].Clear();
            }
        }

        foreach (var tilesPosition in _tilesPositions)
        {
            var random = Random.Range(0, _tilesPrefabs.Count);
            positions[random].Add(tilesPosition);
        }

        for (int i = 0; i < _tilesPrefabs.Count; i++)
        {
            var handle = InstantiateAsync(_tilesPrefabs[i], positions[i].Count, transform,
                new ReadOnlySpan<Vector3>(positions[i].ToArray()), ReadOnlySpan<Quaternion>.Empty);
            operations.Add(handle);
        }

        for (int i = 0; i < _backgroundPrefabs.Count; i++)
        {
            if (!positions.TryAdd(i, new List<Vector3>()))
            {
                positions[i].Clear();
            }
        }

        foreach (var backgroundPosition in _backgroundPositions)
        {
            var random = Random.Range(0, _backgroundPrefabs.Count);
            positions[random].Add(backgroundPosition);
        }


        for (int i = 0; i < _backgroundPrefabs.Count; i++)
        {
            var handle = InstantiateAsync(_backgroundPrefabs[i], positions[i].Count, transform,
                new ReadOnlySpan<Vector3>(positions[i].ToArray()), ReadOnlySpan<Quaternion>.Empty);
            operations.Add(handle);
        }


        for (int i = 0; i < _environmentPrefabs.Count; i++)
        {
            if (!positions.TryAdd(i, new List<Vector3>()))
            {
                positions[i].Clear();
            }
        }

        foreach (var tilesPosition in _tilesPositions)
        {
            var spawn = Random.Range(0, 100);
            if (spawn < environmentDensity)
            {
                var random = Random.Range(0, _environmentPrefabs.Count);
                positions[random].Add(tilesPosition + new Vector3(0, tileSize.y, 0));
            }
        }

        for (int i = 0; i < _environmentPrefabs.Count; i++)
        {
            var handle = InstantiateAsync(_environmentPrefabs[i], positions[i].Count, transform,
                new ReadOnlySpan<Vector3>(positions[i].ToArray()), ReadOnlySpan<Quaternion>.Empty);
            operations.Add(handle);
        }

        await waterHandle;

        var water = waterHandle.Result;

        Dictionary<Vector2Int, PathNode> hexMap = new Dictionary<Vector2Int, PathNode>();

        int k = 0;
        foreach (var tile in _hexMapPositions)
        {
            var pathNode = water[k++].GetComponent<PathNode>();

            pathNode.Coordinates = tile.Key;
            pathNode.transform.position = tile.Value;
            hexMap.Add(pathNode.Coordinates, pathNode);
        }

        foreach (var tile in water)
        {
            var pathNode = tile.GetComponent<PathNode>();

            Vector2Int potentialNeighbor = Vector2Int.zero;
            potentialNeighbor.x = pathNode.X;
            potentialNeighbor.y = pathNode.Y - 1;

            if (hexMap.ContainsKey(potentialNeighbor))
            {
                pathNode.Neighbors.Add(hexMap[potentialNeighbor]);
            }

            potentialNeighbor.x = pathNode.X + 1;
            if (hexMap.ContainsKey(potentialNeighbor))
            {
                pathNode.Neighbors.Add(hexMap[potentialNeighbor]);
            }

            potentialNeighbor.x = pathNode.X - 1;
            potentialNeighbor.y = pathNode.Y;
            if (hexMap.ContainsKey(potentialNeighbor))
            {
                pathNode.Neighbors.Add(hexMap[potentialNeighbor]);
            }

            potentialNeighbor.x = pathNode.X + 1;
            if (hexMap.ContainsKey(potentialNeighbor))
            {
                pathNode.Neighbors.Add(hexMap[potentialNeighbor]);
            }

            potentialNeighbor.y = pathNode.Y + 1;
            potentialNeighbor.x = pathNode.X;
            if (hexMap.ContainsKey(potentialNeighbor))
            {
                pathNode.Neighbors.Add(hexMap[potentialNeighbor]);
            }

            potentialNeighbor.x = pathNode.X - 1;
            if (hexMap.ContainsKey(potentialNeighbor))
            {
                pathNode.Neighbors.Add(hexMap[potentialNeighbor]);
            }
        }

        await boatHandle;
        var boat = boatHandle.Result;

        cameraFollow.Follow = boat[0].transform;
        gridBoatController.Boat = boat[0].GetComponent<Boat>();
        gridBoatController.RegisterNodes(hexMap.Values.ToList());

        foreach (var asyncInstantiateOperation in operations)
        {
            await asyncInstantiateOperation;
        }

        _waterPrefab = null;
        _boatPrefab = null;
        _tilesPrefabs.Clear();
        _backgroundPrefabs.Clear();
        _environmentPrefabs.Clear();

        Debug.Log("Spawned");
    }
}