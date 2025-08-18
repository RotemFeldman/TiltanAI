using UnityEngine;
using UnityEngine.AI;
using GOAP;

public class TrainingArena : MonoBehaviour
{
    [Header("Prefabs (optional)")]
    public EnemyBrain enemyPrefab;
    public GameObject dummyVillagerPrefab;
    public GameObject potionPrefab;
    public GameObject buildLocationPrefab;

    [Header("Use real villagers")]
    public bool useRealVillagerPrefabs = false;
    public GameObject[] realVillagerPrefabs;

    [Header("Counts & Random")]
    public int minVillagers = 1;
    public int maxVillagers = 3;
    public int minPotions = 1;
    public int maxPotions = 3;
    public float arenaRadius = 18f;

    [Header("Layers")]
    public string alliesLayerName  = "Allies";
    public string enemiesLayerName = "Enemies";
    public string potionsLayerName = "Potions";
    public string losBlockersLayerName = "LOS_Blockers";

    [Header("Enemy Startup")]
    [Range(0.5f, 1f)] public float enemyHPMin01 = 0.7f;
    public float enemySpeed = 4.25f;
    public float enemyAccel = 14f;
    public float enemyAngular = 720f;
    public float enemyAttackRange = 2.2f;
    public float enemyAttackCooldown = 0.8f;

    public bool Done { get; private set; }

    EnemyBrain trainee;
    float timer;
    const float MaxTime = 45f;

    public void Evaluate(Genome g)
    {
        Done = false; timer = 0f;
        ResetArena();

        trainee = SpawnEnemy();
        trainee.LoadGenome(g);
        trainee.OnEpisodeEnd = (fit) => { g.fitness = fit; Done = true; };
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

    void ResetArena()
    {
        if (BuildLocation.Instance == null)
        {
            if (buildLocationPrefab) Instantiate(buildLocationPrefab, Vector3.zero, Quaternion.identity);
            else { var go = new GameObject("BuildLocation_Auto"); go.AddComponent<BuildLocation>(); go.transform.position = Vector3.zero; }
        }

        int villCount = Random.Range(minVillagers, maxVillagers + 1);
        for (int i = 0; i < villCount; i++)
        {
            Vector3 pos = SampleNavmesh(RandomInDisc(transform.position, arenaRadius * 0.8f), 10f);
            if (useRealVillagerPrefabs && realVillagerPrefabs != null && realVillagerPrefabs.Length > 0)
            {
                var prefab = realVillagerPrefabs[Random.Range(0, realVillagerPrefabs.Length)];
                if (prefab) Instantiate(prefab, pos, Quaternion.identity, transform);
            }
            else SpawnDummyVillager(pos);
        }

        int potCount = Random.Range(minPotions, maxPotions + 1);
        for (int i = 0; i < potCount; i++)
        {
            Vector3 pos = RandomInDisc(transform.position, arenaRadius * 0.7f);
            SpawnPotion(pos);
        }
    }

    EnemyBrain SpawnEnemy()
    {
        Vector3 pos = SampleNavmesh(RandomInDisc(transform.position, arenaRadius * 0.9f), 10f);
        EnemyBrain eb;

        if (enemyPrefab) eb = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
        else
        {
            var go = new GameObject("Enemy_Trainee");
            go.transform.parent = transform; go.transform.position = pos;

            SetLayerRecursively(go, LayerMask.NameToLayer(enemiesLayerName));
            var tag = go.AddComponent<TeamTag>(); tag.team = TeamTag.Team.Enemies;

            var cc = go.AddComponent<CharacterController>(); cc.center = new Vector3(0,1f,0); cc.height = 2f; cc.radius = 0.4f;
            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = enemySpeed; agent.acceleration = enemyAccel; agent.angularSpeed = enemyAngular;

            go.AddComponent<Health>();
            var sc = go.AddComponent<SimpleCombat>();
            if (sc.weapon){ sc.weapon.attackRange = enemyAttackRange; sc.weapon.cooldown = enemyAttackCooldown; }
            go.AddComponent<SimpleHealer>();
            var sa = go.AddComponent<SimpleAwareness>();
            sa.attackRange = enemyAttackRange; sa.sightRange = 18f; sa.senseRange = 8f;
            sa.allyMask  = LayerMask.GetMask(enemiesLayerName);
            sa.enemyMask = LayerMask.GetMask(alliesLayerName);
            sa.potionMask = LayerMask.GetMask(potionsLayerName);
            sa.losMask   = LayerMask.GetMask(losBlockersLayerName);

            go.AddComponent<NavMovement>();
            eb = go.AddComponent<EnemyBrain>();
        }

        var h = eb.GetComponent<Health>();
        if (h) h.currentHP = Mathf.Lerp(0f, h.maxHP, Random.Range(enemyHPMin01, 1f));

        return eb;
    }

    void SpawnDummyVillager(Vector3 pos)
    {
        if (dummyVillagerPrefab)
        {
            var v = Instantiate(dummyVillagerPrefab, pos, Quaternion.identity, transform);
            SetLayerRecursively(v, LayerMask.NameToLayer(alliesLayerName));
            if (!v.GetComponent<Health>()) v.AddComponent<Health>();
            return;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "DummyVillager"; go.transform.parent = transform; go.transform.position = pos;
        var col = go.GetComponent<CapsuleCollider>(); if (col) col.isTrigger = false;
        SetLayerRecursively(go, LayerMask.NameToLayer(alliesLayerName));
        go.AddComponent<Health>();
    }

    void SpawnPotion(Vector3 pos)
    {
        if (potionPrefab)
        {
            var p = Instantiate(potionPrefab, pos, Quaternion.identity, transform);
            SetLayerRecursively(p, LayerMask.NameToLayer(potionsLayerName));
            if (!p.GetComponent<PotionPickup>()) p.AddComponent<PotionPickup>();
            return;
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Potion"; go.transform.parent = transform; go.transform.localScale = Vector3.one * 0.4f; go.transform.position = pos;
        var col = go.GetComponent<SphereCollider>(); col.isTrigger = true;
        SetLayerRecursively(go, LayerMask.NameToLayer(potionsLayerName));
        go.AddComponent<PotionPickup>();
    }

    static Vector3 RandomInDisc(Vector3 center, float r){ var d = Random.insideUnitCircle * r; return new Vector3(center.x+d.x, center.y, center.z+d.y); }
    static Vector3 SampleNavmesh(Vector3 near, float maxDist){ return NavMesh.SamplePosition(near, out var hit, maxDist, NavMesh.AllAreas) ? hit.position : near; }
    static void SetLayerRecursively(GameObject go, int layer){ if (layer<0) return; go.layer=layer; foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer); }
}
