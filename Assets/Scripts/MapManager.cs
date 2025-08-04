using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class MapManager : MonoBehaviour
{
    public TextAsset mapTextAsset;
    public Vector2 TileSize;

    public List<string> TileKeys;

    private Dictionary<int, List<Vector3>> _map = new Dictionary<int, List<Vector3>>();
    private async void Start()
    {
        await LoadMap();
        await LoadAssets();
    }

    private async Awaitable LoadMap()
    {
        _map.Clear();

        for (int i = 0; i < TileKeys.Count; ++i)
        {
            _map.Add(i, new List<Vector3>());
        }

        float offsetX = 0;
        float offsetZ = 0;
        bool hexagonalOffset = false;

        foreach (var pos in mapTextAsset.text)
        {
            if (pos == '\n')
            {
                offsetX = hexagonalOffset ? 0 : TileSize.y / 2.0f;
                offsetZ -= TileSize.y;
                hexagonalOffset = !hexagonalOffset;
                continue;
            }

            int tileType = 0;
            if (pos == '1')
            {
                tileType = Random.Range(1, TileKeys.Count);
            }

            _map[tileType].Add(new Vector3(offsetX, 0, offsetZ));

            offsetX += TileSize.x;
        }
    }

    private async Awaitable LoadAssets()
    {
        for (int i = 0; i < TileKeys.Count; i++)
        {
            int current = i;
            var handle = Addressables.LoadAssetAsync<GameObject>(TileKeys[i]);
            handle.Completed += (h) =>
            {
                foreach (var pos in _map[current])
                {
                    var tile = h.Result;
                    Instantiate(tile, pos, tile.transform.rotation, transform);
                }

                Debug.Log($"Loaded asset nr: {current}");

            };
            handle.ReleaseHandleOnCompletion();
        }
    }
}
