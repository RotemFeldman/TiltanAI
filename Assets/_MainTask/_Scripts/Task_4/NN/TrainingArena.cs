using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using GOAP; // for BuildLocation singleton

public class TrainingArena : MonoBehaviour
{
    [Header("Prefabs (optional)")]
    public EnemyBrain enemyPrefab;            // If null, the enemy is built procedurally
    public GameObject dummyVillagerPrefab;    // If null, dummies are built procedurally
    public GameObject potionPrefab;           // If null, potions are built procedurally
    public GameObject buildLocationPrefab;    // If null, BuildLocation is built procedurally

    [Header("Use real GOAP villagers instead of dummies")]
    public bool useRealVillagerPrefabs = false;
    public GameObject[] realVillagerPrefabs;  // your existing Villager/Mage prefabs

    [Header("Counts & Randomization")]
    public int minVillagers = 1;
    public int maxVillagers = 3;
    public int minPotions = 1;
    public int maxPotions = 3;
    public float arenaRadius = 18f;

    [Header("Layers (must exist in Project Settings → Tags & Layers)")]
    public string alliesLayerName  = "Allies";
    public string enemiesLayerName = "Enemies";
    public string potionsLayerName = "Potions";
    public string losBlockersLayerName = "LOS_Blockers"; // optional

    [Header("Enemy Startup")]
    [Range(0.5f, 1f)] public float enemyHPMin01 = 0.7f;  // randomize starting HP (min..1)
    public float enemySpeed = 4.25f;
    public float enemyAccel = 14f;
    public float enemyAngular = 720f;
    public float enemyAttackRange = 2.2f;
    public float enemyAttackCooldown = 0.8f;

    public bool Done { get; private set; }

    EnemyBrain trainee;
    float timer;
    const float MaxTime = 45f;

    // === Public API used by EvolutionManager ===
    public void Evaluate(Genome g)
    {
        Done = false;
        timer = 0f;
        ResetArena();

        trainee = SpawnEnemy();
        trainee.LoadGenome(g);
        trainee.OnEpisodeEnd = (fitness) => { g.fitness = fitness; Done = true; };
    }

    void Update()
    {
        if (!Done && (trainee == null || timer >= MaxTime))
        {
            trainee?.ForceEndEpisode("timeout");
            Done = true;
        }
        timer += Time.deltaTime;
    }

    public void Cleanup()
    {
        foreach (Transform c in transform) Destroy(c.gameObject);
        trainee = null;
    }

    // === Core arena building ===
    void ResetArena()
    {
        // Ensure BuildLocation exists ONCE in the scene
        if (BuildLocation.Instance == null)
        {
            if (buildLocationPrefab)
                Instantiate(buildLocationPrefab, Vector3.zero, Quaternion.identity);
            else
            {
                var go = new GameObject("BuildLocation_Auto");
                go.AddComponent<BuildLocation>();
                go.transform.position = Vector3.zero;
            }
        }

        // Random villagers (targets)
        int villCount = Random.Range(minVillagers, maxVillagers + 1);
        for (int i = 0; i < villCount; i++)
        {
            Vector3 pos = SampleNavmeshPoint(RandomInDisc(transform.position, arenaRadius * 0.8f), 10f);
            if (useRealVillagerPrefabs && realVillagerPrefabs != null && realVillagerPrefabs.Length > 0)
            {
                var prefab = realVillagerPrefabs[Random.Range(0, realVillagerPrefabs.Length)];
                if (prefab) Instantiate(prefab, pos, Quaternion.identity, transform);
            }
            else
            {
                SpawnDummyVillager(pos);
            }
        }

        // Potions
        int potCount = Random.Range(minPotions, maxPotions + 1);
        for (int i = 0; i < potCount; i++)
        {
            Vector3 pos = RandomInDisc(transform.position, arenaRadius * 0.7f);
            SpawnPotion(pos);
        }
    }

    EnemyBrain SpawnEnemy()
    {
        Vector3 pos = SampleNavmeshPoint(RandomInDisc(transform.position, arenaRadius * 0.9f), 10f);
        EnemyBrain eb;

        if (enemyPrefab != null)
        {
            eb = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
        }
        else
        {
            // Build procedurally
            var go = new GameObject("Enemy_Trainee");
            go.transform.parent = transform;
            go.transform.position = pos;

            // Team/layers
            SetLayerRecursively(go, LayerMask.NameToLayer(enemiesLayerName));
            var tag = go.AddComponent<TeamTag>(); tag.team = TeamTag.Team.Enemies;

            // Controller + Agent
            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1f, 0); cc.height = 2f; cc.radius = 0.4f;
            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = enemySpeed; agent.acceleration = enemyAccel; agent.angularSpeed = enemyAngular;

            // Core ability set
            go.AddComponent<Health>();
            var sc = go.AddComponent<SimpleCombat>();
            if (sc.weapon)
            {
                sc.weapon.attackRange = enemyAttackRange;
                sc.weapon.cooldown = enemyAttackCooldown;
            }
            go.AddComponent<SimpleHealer>();
            var sa = go.AddComponent<SimpleAwareness>();
            sa.attackRange = enemyAttackRange;
            sa.sightRange = 18f; sa.senseRange = 8f;

            // Masks
            sa.allyMask  = LayerMask.GetMask(enemiesLayerName);
            sa.enemyMask = LayerMask.GetMask(alliesLayerName);
            sa.potionMask = LayerMask.GetMask(potionsLayerName);
            sa.losMask   = LayerMask.GetMask(losBlockersLayerName); // 0 if not found; that's fine

            go.AddComponent<NavMovement>();

            // Brain last (so all ports exist)
            eb = go.AddComponent<EnemyBrain>();
        }

        // Randomize starting HP
        var h = eb.GetComponent<Health>();
        if (h != null)
        {
            float minHP = Mathf.Clamp01(enemyHPMin01);
            h.currentHP = Mathf.Lerp(0f, h.maxHP, Random.Range(minHP, 1f));
        }

        return eb;
    }

    void SpawnDummyVillager(Vector3 pos)
    {
        if (dummyVillagerPrefab != null)
        {
            var v = Instantiate(dummyVillagerPrefab, pos, Quaternion.identity, transform);
            SetLayerRecursively(v, LayerMask.NameToLayer(alliesLayerName));
            return;
        }

        // Procedural capsule dummy with Health
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "DummyVillager";
        go.transform.parent = transform;
        go.transform.position = pos;

        // Collider already added by primitive; ensure not trigger
        var col = go.GetComponent<CapsuleCollider>(); if (col) col.isTrigger = false;

        SetLayerRecursively(go, LayerMask.NameToLayer(alliesLayerName));
        var tag = go.AddComponent<TeamTag>(); tag.team = TeamTag.Team.Villagers;
        go.AddComponent<Health>();
        // (Optional) give them SimpleCombat if you want them to fight back:
        // go.AddComponent<SimpleCombat>();
    }

    void SpawnPotion(Vector3 pos)
    {
        if (potionPrefab != null)
        {
            var p = Instantiate(potionPrefab, pos, Quaternion.identity, transform);
            SetLayerRecursively(p, LayerMask.NameToLayer(potionsLayerName));
            if (!p.GetComponent<PotionPickup>()) p.AddComponent<PotionPickup>();
            return;
        }

        // Procedural small sphere with trigger that heals on pickup
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Potion";
        go.transform.parent = transform;
        go.transform.localScale = Vector3.one * 0.4f;
        go.transform.position = pos;

        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        SetLayerRecursively(go, LayerMask.NameToLayer(potionsLayerName));
        go.AddComponent<PotionPickup>();
    }

    // === helpers ===
    static Vector3 RandomInDisc(Vector3 center, float radius)
    {
        var r = Random.insideUnitCircle * radius;
        return new Vector3(center.x + r.x, center.y, center.z + r.y);
    }

    static Vector3 SampleNavmeshPoint(Vector3 near, float maxDistance)
    {
        Vector3 p = near;
        if (NavMesh.SamplePosition(near, out var hit, maxDistance, NavMesh.AllAreas))
            p = hit.position;
        return p;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (layer < 0) return;
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
    }
}
