using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

public class MapManager : MonoBehaviour
{
    public TextAsset mapTextAsset;
    public Vector3 TileSize;
    public Vector2 BackgroundSize;
    [Range(0, 100)]
    public float EnvironmentDensity;

    public string TilesKey;
    public string WaterKey;
    public string BackgroundKey;
    public string EnvironmentKey;

    private List<GameObject> _tilesPrefabs = new List<GameObject>();
    private List<GameObject> _waterPrefabs = new List<GameObject>();
    private List<GameObject> _backgroundPrefabs = new List<GameObject>();
    private List<GameObject> _environmentPrefabs = new List<GameObject>();

    private List<Vector3> _waterPositions = new List<Vector3>();
    private List<GameObject> _water = new List<GameObject>();
    private List<Vector3> _tilesPositions = new List<Vector3>();
    private List<GameObject> _tiles = new List<GameObject>();
    private List<Vector3> _backgroundPositions = new List<Vector3>();
    private List<GameObject> _background = new List<GameObject>();
    private Vector2 _mapSize;
    private Vector2Int[,] _grid = new Vector2Int[0, 0];

    private Dictionary<Vector3, Vector2Int> _hexMap = new Dictionary<Vector3, Vector2Int>();
    private async void Start()
    {
        var loadTask = LoadAssets();

        Debug.Log("started loading map");
        LoadMap();
        Debug.Log("ended loading map");

        await loadTask;

        GenerateMap();
    }

    private void LoadMap()
    {
        _waterPositions.Clear();
        _tilesPositions.Clear();
        _backgroundPositions.Clear();

        _mapSize = Vector2.zero;

        float offsetX = 0;
        float offsetZ = 0;
        bool hexagonalOffset = false;

        int gridHeightX = 0;
        int gridHeightY = 0;

        var map = mapTextAsset.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        gridHeightX = map[0].Length;
        gridHeightY = map.Length;

        _grid = new Vector2Int[gridHeightX, gridHeightY];

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
                    _waterPositions.Add(pos);
                }
                else
                {
                    _tilesPositions.Add(pos);
                }
                offsetX += TileSize.x;

                Vector2Int hexPos = new Vector2Int(hexRowStartingIndex + j, hexColumnStartingIndex + i);

                _grid[j, i] = hexPos;
                _hexMap.Add(pos, hexPos);
            }

            if (offsetX > _mapSize.x)
            {
                _mapSize.x = offsetX;
            }
            offsetX = hexagonalOffset ? 0 : TileSize.x / 2.0f;
            offsetZ -= TileSize.z;
            hexagonalOffset = !hexagonalOffset;


        }

        _mapSize.y = Mathf.Abs(offsetZ);

        Vector2 backgroundTilesAmount = new Vector2(_mapSize.x / BackgroundSize.x, _mapSize.y / BackgroundSize.y);

        offsetX = 0;
        offsetZ = 0;

        for (int i = 0; i < backgroundTilesAmount.x; i++)
        {
            for (int j = 0; j < backgroundTilesAmount.y; j++)
            {
                _backgroundPositions.Add(new Vector3(offsetX + (i * BackgroundSize.x), 0, offsetZ - (j * BackgroundSize.y)));
            }
        }
    }

    private async Task LoadAssets()
    {
        Debug.Log("started loading assets");
        //for (int i = 0; i < TileKeys.Count; i++)
        //{
        //    int current = i;
        //    var handle = Addressables.LoadAssetAsync<GameObject>(TileKeys[i]);
        //    handle.Completed += (h) =>
        //    {
        //        foreach (var pos in _map[current])
        //        {
        //            var tile = h.Result;
        //            Instantiate(tile, pos, tile.transform.rotation, transform);
        //        }

        //        Debug.Log($"Loaded asset nr: {current}");

        //    };
        //    handle.ReleaseHandleOnCompletion();
        //}

        var waterHandle = Addressables.LoadAssetsAsync<GameObject>(WaterKey,
            addressable => { _waterPrefabs.Add(addressable); }, Addressables.MergeMode.Union);
        var tileHandle = Addressables.LoadAssetsAsync<GameObject>(TilesKey,
            addressable => { _tilesPrefabs.Add(addressable); }, Addressables.MergeMode.Union);
        var backgroundHandle = Addressables.LoadAssetsAsync<GameObject>(BackgroundKey,
            addressable => { _backgroundPrefabs.Add(addressable); }, Addressables.MergeMode.Union);
        var environmentHandle = Addressables.LoadAssetsAsync<GameObject>(EnvironmentKey,
            addressable => { _environmentPrefabs.Add(addressable); }, Addressables.MergeMode.Union);

        await Task.WhenAll(waterHandle.Task, backgroundHandle.Task, tileHandle.Task, environmentHandle.Task);

        waterHandle.Release();
        backgroundHandle.Release();
        tileHandle.Release();
        environmentHandle.Release();

        Debug.Log("ended loading assets");
    }

    private async void GenerateMap()
    {
        List<AsyncInstantiateOperation<GameObject>> operations = new List<AsyncInstantiateOperation<GameObject>>();

        Dictionary<int, List<Vector3>> positions = new Dictionary<int, List<Vector3>>();

        for (int i = 0; i < _waterPrefabs.Count; i++)
        {
            positions.TryAdd(i, new List<Vector3>());
        }
        foreach (var waterPosition in _waterPositions)
        {
            var random = Random.Range(0, _waterPrefabs.Count);
            positions[random].Add(waterPosition);
        }

        for (int i = 0; i < _waterPrefabs.Count; i++)
        {
            var handle = InstantiateAsync(_waterPrefabs[i], positions[i].Count,
                new ReadOnlySpan<Vector3>(positions[i].ToArray()), ReadOnlySpan<Quaternion>.Empty,
                new InstantiateParameters() { parent = transform });
            operations.Add(handle);
        }

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
            var handle = InstantiateAsync(_tilesPrefabs[i], positions[i].Count,
                new ReadOnlySpan<Vector3>(positions[i].ToArray()), ReadOnlySpan<Quaternion>.Empty,
                new InstantiateParameters() { parent = transform });
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
            var handle = InstantiateAsync(_backgroundPrefabs[i], positions[i].Count,
                new ReadOnlySpan<Vector3>(positions[i].ToArray()), ReadOnlySpan<Quaternion>.Empty,
                new InstantiateParameters() { parent = transform });
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
            if (spawn < EnvironmentDensity)
            {
                var random = Random.Range(0, _environmentPrefabs.Count);
                positions[random].Add(tilesPosition + new Vector3(0, TileSize.y, 0));
            }
        }

        for (int i = 0; i < _environmentPrefabs.Count; i++)
        {
            var handle = InstantiateAsync(_environmentPrefabs[i], positions[i].Count,
                new ReadOnlySpan<Vector3>(positions[i].ToArray()), ReadOnlySpan<Quaternion>.Empty,
                new InstantiateParameters() { parent = transform });
            operations.Add(handle);
        }


        await operations[0];

        _water = operations[0].Result.ToList();


        foreach (var tile in _water)
        {
            var pathNode = tile.GetComponent<PathNode>();

            pathNode.X = _hexMap[tile.transform.position].x;
            pathNode.Y = _hexMap[tile.transform.position].y;

        }

        foreach (var tile in _water)
        {
            var pathNode = tile.GetComponent<PathNode>();
            foreach (var tile2 in _water)
            {
                var pathNode2 = tile2.GetComponent<PathNode>();

                if (Heuristic(pathNode, pathNode2) == 1)
                {
                    pathNode.Neighbors.Add(pathNode2);
                }
            }
        }

        operations.ForEach(x => x.WaitForCompletion());



        Debug.Log("Spawned");

        //await Task.WhenAll(SpawnTilesOnPositions(_waterPrefabs, _waterPositions),
        //    SpawnTilesOnPositions(_tilesPrefabs, _tilesPositions));

    }

    private async Task SpawnTilesOnPositions(GameObject prefab, List<Vector3> positions)
    {
        var handle = InstantiateAsync(prefab, count: positions.Count, new ReadOnlySpan<Vector3>(positions.ToArray()), ReadOnlySpan<Quaternion>.Empty, new InstantiateParameters() { parent = transform });
        await handle;
        _water = handle.Result.ToList();
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
