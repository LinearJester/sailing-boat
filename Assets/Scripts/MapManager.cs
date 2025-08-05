using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

public class MapManager : MonoBehaviour
{
    public TextAsset mapTextAsset;
    public Vector2 TileSize;

    public string TilesKey;
    public string WaterKey;
    public string BackgroundKey;

    private List<GameObject> _tilesPrefabs = new List<GameObject>();
    private List<GameObject> _waterPrefabs = new List<GameObject>();
    private List<GameObject> _backgroundPrefabs = new List<GameObject>();

    private List<Vector3> _waterPositions = new List<Vector3>();
    private List<GameObject> _water = new List<GameObject>();
    private List<Vector3> _tilesPositions = new List<Vector3>();
    private List<GameObject> _tiles = new List<GameObject>();
    private List<Vector3> _backgroundPositions = new List<Vector3>();
    private List<GameObject> _background = new List<GameObject>();
    private Vector2 _mapSize;
    private async void Start()
    {
        await Task.WhenAll(LoadAssets(), LoadMap());

        await GenerateMap();
    }

    private async Task LoadMap()
    {
        _waterPositions.Clear();
        _tilesPositions.Clear();
        _backgroundPositions.Clear();

        _mapSize = Vector2.zero;

        float offsetX = 0;
        float offsetZ = 0;
        bool hexagonalOffset = false;

        foreach (var pos in mapTextAsset.text)
        {
            if (pos == '\n')
            {
                if (offsetX > _mapSize.x)
                {
                    _mapSize.x = offsetX;
                }
                offsetX = hexagonalOffset ? 0 : TileSize.y / 2.0f;
                offsetZ -= TileSize.y;
                hexagonalOffset = !hexagonalOffset;
                continue;
            }

            if (pos == '0')
            {
                _waterPositions.Add(new Vector3(offsetX, 0, offsetZ));
            }
            else
            {
                _tilesPositions.Add(new Vector3(offsetX, 0, offsetZ));
            }

            offsetX += TileSize.x;
        }

        _mapSize.y = Mathf.Abs(offsetZ);
    }

    private async Task LoadAssets()
    {
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

        await Task.WhenAll(waterHandle.Task, backgroundHandle.Task, tileHandle.Task);

    }

    private async Awaitable GenerateMap()
    {
        await Task.WhenAll(SpawnTilesOnPositions(_waterPrefabs, _waterPositions),
            SpawnTilesOnPositions(_tilesPrefabs, _tilesPositions));

    }

    private async Task SpawnTilesOnPositions(List<GameObject> prefabs, List<Vector3> positions)
    {
        if (prefabs.Count == 1)
        {
            var handle = InstantiateAsync(prefabs[0], count: positions.Count, new ReadOnlySpan<Vector3>(positions.ToArray()), ReadOnlySpan<Quaternion>.Empty, new InstantiateParameters(){parent = transform});
            await handle;
            _water = handle.Result.ToList();
        }
        else
        {
            foreach (var position in positions)
            {
                int prefab = Random.Range(0, prefabs.Count);

                _water.Add(Instantiate(prefabs[prefab], position, prefabs[prefab].transform.rotation, transform));
            }
        }
    }
}
