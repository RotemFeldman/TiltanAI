using UnityEngine;

public class BrainLoaderFlat : MonoBehaviour
{
    [Header("Brains")]
    public TextAsset[] brainJsons;

    [Header("Enemy Spawning")]
    public EnemyBrain enemyPrefab;
    public Transform[] spawnPoints;

    void Start()
    {
        if (enemyPrefab == null || brainJsons == null || brainJsons.Length == 0)
        {
            Debug.LogError("[BrainLoaderFlat] Missing enemyPrefab or brainJsons");
            return;
        }

        for (int i=0;i<spawnPoints.Length;i++)
        {
            var ta = brainJsons[i % brainJsons.Length];
            var genome = GenomeIO.LoadFlatJson(ta);
            if (genome == null) { Debug.LogError($"[BrainLoaderFlat] Failed to load {ta?.name}"); continue; }

            var e = Instantiate(enemyPrefab, spawnPoints[i].position, Quaternion.identity);
            e.gameObject.AddComponent<SnapToNavMeshOnStart>();

            var h = e.GetComponent<Health>(); if (h && h.currentHP <= 0f) h.currentHP = h.maxHP;
            e.LoadGenome(genome);
        }
    }
}