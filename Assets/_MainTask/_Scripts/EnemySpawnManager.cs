using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnManager : MonoBehaviour
{
    [Header("Prefabs & Spawn Points")]
    public GameObject enemyPrefab;                // must have NavMeshAgent + EnemyController
    public List<Transform> spawnPoints = new();   // random among these

    [Header("Enemy Brains (JSON)")]
    public List<TextAsset> brainFiles = new(); // drag your .json assets here

    [Header("Counts")]
    public float enemyToFriendlyMultiplier = 1.25f;
    public int minEnemies = 5;
    public int maxEnemies = 50;

    [Header("Refill")]
    public float checkEvery = 5f;

    private float _timer;
    private readonly List<GameObject> _live = new();

    void Start()
    {
        Maintain();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= checkEvery) { _timer = 0f; Maintain(); }

        for (int i = _live.Count - 1; i >= 0; i--)
            if (_live[i] == null) _live.RemoveAt(i);
    }

    void Maintain()
    {
        int villagers = GameObject.FindGameObjectsWithTag("Villager").Length;
        int mages = GameObject.FindGameObjectsWithTag("Mage").Length;
        int desired = Mathf.Clamp(Mathf.CeilToInt((villagers + mages) * enemyToFriendlyMultiplier), minEnemies, maxEnemies);

        int need = desired - _live.Count;
        if (need <= 0) return;

        for (int i = 0; i < need; i++)
            SpawnOne();
    }

    void SpawnOne()
    {
        if (spawnPoints.Count == 0 || enemyPrefab == null) return;

        var sp = spawnPoints[Random.Range(0, spawnPoints.Count)];
        if (!NavMesh.SamplePosition(sp.position, out var hit, 3f, NavMesh.AllAreas)) return;

        var go = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
        var ctrl = go.GetComponent<EnemyNNController>();
        if (ctrl != null && brainFiles.Count > 0)
        {
            ctrl.brainJson = brainFiles[Random.Range(0, brainFiles.Count)];
        }

        _live.Add(go);
    }
}