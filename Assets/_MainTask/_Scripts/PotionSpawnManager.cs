using System.Collections.Generic;
using UnityEngine;

public class PotionSpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class Entry { public GameObject prefab; [Range(0,1)] public float weight = 1f; }

    public List<Transform> spawnPoints = new();
    public List<Entry> potions = new();
    public int maxOnMap = 25;
    public float every = 6f;
    public float chancePerPoint = 0.35f;

    private float _t;
    private int _live;

    void Update()
    {
        _t += Time.deltaTime;
        if (_t >= every) { _t = 0; TrySpawn(); }
    }

    void TrySpawn()
    {
        if (potions.Count == 0 || spawnPoints.Count == 0) return;

        foreach (var sp in spawnPoints)
        {
            if (_live >= maxOnMap) break;
            if (Random.value > chancePerPoint) continue;

            var e = Pick();
            if (e?.prefab == null) continue;

            var go = Instantiate(e.prefab, sp.position, Quaternion.identity);
            _live++;
            var tr = go.AddComponent<SpawnedItemTracker>();
            tr.onDestroyed += () => _live--;
        }
    }

    Entry Pick()
    {
        float sum = 0; foreach (var e in potions) sum += e.weight;
        float r = Random.value * sum;
        foreach (var e in potions) { r -= e.weight; if (r <= 0) return e; }
        return potions[potions.Count - 1];
    }
}

public class SpawnedItemTracker : MonoBehaviour
{
    public System.Action onDestroyed;
    void OnDestroy() => onDestroyed?.Invoke();
}