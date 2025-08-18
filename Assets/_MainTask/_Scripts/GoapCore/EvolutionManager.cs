using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public class EvolutionManager : MonoBehaviour
{
    [Header("Population")]
    public int population = 40;
    public int generations = 20;
    [Range(0f, 0.5f)] public float eliteFraction = 0.10f;

    [Header("Network Shape")]
    [Tooltip("Match EnemySensors.GetInputs output, typically 7 in this project.")]
    public int inputSize = 7;
    public int hiddenSize = 16;
    public int outputSize = 5;

    [Header("Mutation")]
    [Range(0f, 1f)] public float mutationRate = 0.08f;
    public float mutationSigma = 0.25f;

    [Header("Parallelism / Arenas")]
    [Tooltip("How many agents to evaluate at the same time (spawned side-by-side).")]
    public int parallelArenas = 7;
    [Tooltip("Enemy prefab with EnemyNNController + Damageable.")]
    public GameObject enemyPrefab;
    public Transform arenasParent;
    public float arenaSpacing = 90f;
    [Tooltip("Max seconds for one episode/batch. Survivors are culled and scored with full time.")]
    public float episodeTime = 30f;

    [Tooltip("How many enemies to spawn per arena slot (cohort size for the same genome).")]
    public int enemiesPerArenaSlot = 8;
    [Tooltip("Scatter radius within an arena slot for the cohort.")]
    public float cohortScatterRadius = 8f;

    [Header("Training Extras (Friends & Items)")]
    [Tooltip("Optional: Villager and Mage prefabs to populate each arena slot.")]
    public GameObject villagerPrefab;
    public GameObject magePrefab;
    public GameObject messengerPrefab;
    [Tooltip("Optional: Potion prefab visible to enemies and drinkable by friendlies if supported.")]
    public GameObject potionPrefab;
    [Tooltip("Counts to spawn per arena slot.")]
    public int villagersPerArena = 5;
    public int magesPerArena = 1;
    public int messengersPerArena = 2;
    public int potionsPerArena = 4;
    [Tooltip("Spawn radius around each slot origin for extras.")]
    public float extrasRadius = 8f;
    [Tooltip("Layer used by NavMesh.SamplePosition for extras. Use NavMesh.AllAreas for default.")]
    public int navMeshAreaMask = NavMesh.AllAreas;

    [Header("Saving")]
    public bool saveEveryGeneration = true;
    public int saveTopK = 3;
    [Tooltip("Leave empty to use Application.persistentDataPath + \"/Brains\"")]
    public string saveFolderAbsolute = "";
    public bool maintainBestSoFar = true;
    public string notesTag = "Classic NeuroEvolution run";

    [Header("Debug")]
    public bool logEval = true;
    public bool logSaves = true;

    // Internal state
    List<Genome> pop;
    System.Random rng;
    int uniqueId;
    string saveFolderResolved;

    void Start()
    {
        if (!enemyPrefab)
        {
            Debug.LogError("[EvolutionManager] Enemy prefab is missing.");
            enabled = false;
            return;
        }
        if (parallelArenas <= 0) parallelArenas = 1;
        rng = new System.Random();

        saveFolderResolved = string.IsNullOrEmpty(saveFolderAbsolute)
            ? Path.Combine(Application.persistentDataPath, "Brains")
            : saveFolderAbsolute.Trim();

        if (!Directory.Exists(saveFolderResolved))
            Directory.CreateDirectory(saveFolderResolved);

        BuildInitialPopulation();
        StartCoroutine(TrainRoutine());
    }

    void BuildInitialPopulation()
    {
        pop = new List<Genome>(population);
        for (int i = 0; i < population; i++)
            pop.Add(new Genome(inputSize, hiddenSize, outputSize, initializeRandom: true, rng));
    }

    IEnumerator TrainRoutine()
    {
        for (int gen = 0; gen < generations; gen++)
        {
            Debug.Log($"==== Generation {gen} ====");
            yield return EvaluateAll(pop, gen);

            pop.Sort((a, b) => b.fitness.CompareTo(a.fitness));
            Debug.Log($"Gen {gen} best fitness = {pop[0].fitness:F2}");

            if (saveEveryGeneration)
            {
                SaveTopK(pop, gen, saveTopK);
                if (maintainBestSoFar && pop.Count > 0) SaveBestSoFar(pop[0], gen);
            }

            // Elitism
            var next = new List<Genome>(population);
            int elites = Mathf.Max(1, Mathf.RoundToInt(population * eliteFraction));
            elites = Mathf.Min(elites, pop.Count);
            for (int i = 0; i < elites; i++) next.Add(pop[i].Clone());

            // Fill with mutated children via tournament selection
            while (next.Count < population)
            {
                var parent = TournamentSelect(pop, 3, rng);
                var child = parent.Clone();
                child.Mutate(mutationRate, mutationSigma, rng);
                next.Add(child);
            }
            pop = next;
        }

        // Final evaluation + save
        yield return EvaluateAll(pop, generations);
        pop.Sort((a, b) => b.fitness.CompareTo(a.fitness));
        SaveTopK(pop, generations, Mathf.Max(saveTopK, 1));
        if (maintainBestSoFar && pop.Count > 0) SaveBestSoFar(pop[0], generations);
        Debug.Log("[EvolutionManager] Training complete.");
    }

    IEnumerator EvaluateAll(List<Genome> genomes, int genIndex)
    {
        int next = 0;

        while (next < genomes.Count)
        {
            // Spawn a batch
            int batchCount = Mathf.Min(parallelArenas, genomes.Count - next);
            var batch = new List<SimInstance>(batchCount);
            var batchExtras = new List<GameObject>(); // track villagers/mages/messengers/potions for this batch

            for (int i = 0; i < batchCount; i++)
            {
                var g = genomes[next++];
                var sim = SpawnSim(g, i);
                batch.Add(sim);

                // Populate this arena slot with extras
                Vector3 slotOrigin = transform.position + new Vector3(i * arenaSpacing, 0f, 0f);
                SpawnExtrasForSlot(slotOrigin, batchExtras);
            }

            // Run the episode
            float t = 0f;
            while (t < episodeTime && batch.Exists(s => s != null && s.AnyAlive))
            {
                t += Time.deltaTime;
                yield return null;
            }

            // Score + cleanup
            foreach (var sim in batch)
            {
                if (sim == null) continue;
                float timeAliveAvg = Mathf.Min(episodeTime, sim.AverageAliveTime());
                sim.genome.fitness = Mathf.Max(sim.genome.fitness, timeAliveAvg); // keep best score across repeats
                CleanupSim(sim);
            }

            // Cleanup batch extras
            for (int i = batchExtras.Count - 1; i >= 0; i--)
            {
                if (batchExtras[i]) Destroy(batchExtras[i]);
            }
            batchExtras.Clear();

            if (logEval)
                Debug.Log($"[EvolutionManager] Evaluated batch. Gen {genIndex}, scored {batchCount}.");
        }
    }

    // Simulation instance for one genome evaluated as a cohort in an arena slot
    class SimInstance
    {
        public class Member
        {
            public GameObject go;
            public Damageable hp;
            public float spawnTime;
            public bool alive;
            public float deathTime;

            public float AliveTimeNow() => alive ? (Time.time - spawnTime) : (deathTime - spawnTime);
            public void MarkDead()
            {
                if (!alive) return;
                alive = false;
                deathTime = Time.time;
            }
        }

        public List<Member> members = new List<Member>();
        public Genome genome;

        public bool AnyAlive
        {
            get
            {
                for (int i = 0; i < members.Count; i++)
                    if (members[i].alive) return true;
                return false;
            }
        }

        public float AverageAliveTime()
        {
            if (members.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < members.Count; i++)
                sum += members[i].AliveTimeNow();
            return sum / members.Count;
        }
    }

    SimInstance SpawnSim(Genome g, int slotIndex)
    {
        if (enemiesPerArenaSlot < 1) enemiesPerArenaSlot = 1;

        Vector3 slotOrigin = transform.position + new Vector3(slotIndex * arenaSpacing, 0f, 0f);
        var parent = arenasParent ? arenasParent : transform;

        var sim = new SimInstance { genome = g };

        for (int k = 0; k < enemiesPerArenaSlot; k++)
        {
            // Scatter around slot origin
            Vector2 r = UnityEngine.Random.insideUnitCircle * cohortScatterRadius;
            Vector3 want = slotOrigin + new Vector3(r.x, 0f, r.y);
            Vector3 pos = want;
            if (NavMesh.SamplePosition(want, out var hit, Mathf.Max(2f, cohortScatterRadius), NavMesh.AllAreas))
                pos = hit.position;

            var go = Instantiate(enemyPrefab, pos, Quaternion.identity, parent);

            var ctrl = go.GetComponent<EnemyNNController>();
            var hp = go.GetComponent<Damageable>();
            if (!ctrl || !hp)
            {
                Debug.LogError("[EvolutionManager] Prefab must have EnemyNNController and Damageable.");
                Destroy(go);
                continue;
            }

            // Same brain for the entire cohort
            var brain = g.ToTextAsset($"Gen{g.gen}_{uniqueId++}");
            ctrl.SetBrain(brain);

            var m = new SimInstance.Member
            {
                go = go,
                hp = hp,
                spawnTime = Time.time,
                alive = true
            };
            // Subscribe death to mark member
            hp.onDeath += _ => m.MarkDead();

            sim.members.Add(m);
        }

        return sim;
    }

    void CleanupSim(SimInstance s)
    {
        if (s == null || s.members == null) return;
        for (int i = 0; i < s.members.Count; i++)
        {
            var m = s.members[i];
            if (m != null && m.go != null)
                Destroy(m.go);
        }
        s.members.Clear();
    }

    // Spawns villagers, mages, messengers, and potions around a slot origin and collects them in 'bucket' for cleanup
    void SpawnExtrasForSlot(Vector3 origin, List<GameObject> bucket)
    {
        var parent = arenasParent ? arenasParent : transform;

        // Villagers
        if (villagerPrefab && villagersPerArena > 0)
        {
            for (int i = 0; i < villagersPerArena; i++)
            {
                if (TrySample(origin, extrasRadius, out var pos))
                {
                    var go = Instantiate(villagerPrefab, pos, Quaternion.identity, parent);
                    bucket.Add(go);
                }
            }
        }

        // Mages
        if (magePrefab && magesPerArena > 0)
        {
            for (int i = 0; i < magesPerArena; i++)
            {
                if (TrySample(origin, extrasRadius, out var pos))
                {
                    var go = Instantiate(magePrefab, pos, Quaternion.identity, parent);
                    bucket.Add(go);
                }
            }
        }

        // Messengers (NEW)
        if (messengerPrefab && messengersPerArena > 0)
        {
            for (int i = 0; i < messengersPerArena; i++)
            {
                if (TrySample(origin, extrasRadius, out var pos))
                {
                    var go = Instantiate(messengerPrefab, pos, Quaternion.identity, parent);
                    bucket.Add(go);
                }
            }
        }

        // Potions
        if (potionPrefab && potionsPerArena > 0)
        {
            for (int i = 0; i < potionsPerArena; i++)
            {
                if (TrySample(origin, extrasRadius, out var pos))
                {
                    var go = Instantiate(potionPrefab, pos, Quaternion.identity, parent);
                    bucket.Add(go);
                }
            }
        }
    }

    bool TrySample(Vector3 center, float radius, out Vector3 hitPos)
    {
        Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
        Vector3 want = center + new Vector3(r.x, 0f, r.y);
        if (NavMesh.SamplePosition(want, out var hit, radius, navMeshAreaMask))
        {
            hitPos = hit.position;
            return true;
        }
        hitPos = center;
        return false;
    }


    void SaveTopK(List<Genome> genomes, int genIndex, int topK)
    {
        if (genomes == null || genomes.Count == 0) return;
        topK = Mathf.Clamp(topK, 1, genomes.Count);

        for (int i = 0; i < topK; i++)
        {
            var g = genomes[i];
            string fileNoExt = $"BestGenome_gen{genIndex:D2}_rank{i + 1}_fit{g.fitness:F2}";
            SaveGenomeAsFlatJson(g, fileNoExt);
        }
        if (logSaves)
            Debug.Log($"[EvolutionManager] Saved top {topK} brains for generation {genIndex} to {saveFolderResolved}");
    }

    void SaveBestSoFar(Genome best, int genIndex)
    {
        SaveGenomeAsFlatJson(best, "BestSoFar");
        if (logSaves)
            Debug.Log($"[EvolutionManager] Saved BestSoFar (gen {genIndex}) to {saveFolderResolved}");
    }

    void SaveGenomeAsFlatJson(Genome g, string fileNoExt)
    {
        var def = g.ToDef();
        string json = JsonUtility.ToJson(def, prettyPrint: true);

        string fn = $"{Sanitize(fileNoExt)}.json";
        string path = Path.Combine(saveFolderResolved, fn);
        File.WriteAllText(path, json);
    }

    Genome TournamentSelect(List<Genome> pool, int k, System.Random rngLocal)
    {
        if (pool == null || pool.Count == 0) throw new InvalidOperationException("Empty pool");
        k = Mathf.Clamp(k, 2, Mathf.Min(6, pool.Count));
        Genome best = null;
        for (int i = 0; i < k; i++)
        {
            var cand = pool[rngLocal.Next(pool.Count)];
            if (best == null || cand.fitness > best.fitness) best = cand;
        }
        return best;
    }

    static string Sanitize(string s) =>
        s.Replace(' ', '_').Replace('/', '_').Replace('\\', '_').Replace(':', '_');

    // ----- Genome (flat JSON compatible with EnemyNNController) -----
    [Serializable]
    class Genome
    {
        public int gen;
        public float fitness;

        public int[] layers;          // supports [in, hidden, out]
        public float[] weightsFlat;   // concatenated by layer (out*in per layer)
        public float[] biasesFlat;    // concatenated by layer (out per layer)
        public string[] actionNames;  // optional

        public Genome(int inSize, int hidden, int outSize, bool initializeRandom, System.Random rng)
        {
            layers = hidden > 0 ? new[] { inSize, hidden, outSize } : new[] { inSize, outSize };
            int expectW = hidden > 0 ? (inSize * hidden + hidden * outSize) : (inSize * outSize);
            int expectB = hidden > 0 ? (hidden + outSize) : outSize;

            weightsFlat = new float[expectW];
            biasesFlat = new float[expectB];

            if (initializeRandom)
            {
                if (hidden > 0)
                {
                    // Xavier-like small init
                    float wScale1 = 1f / Mathf.Sqrt(inSize);
                    float wScale2 = 1f / Mathf.Sqrt(hidden);

                    int idx = 0;
                    for (int j = 0; j < hidden; j++)
                        for (int i = 0; i < inSize; i++)
                            weightsFlat[idx++] = (float)NextGaussian(rng, 0, 0.5f * wScale1);

                    for (int j = 0; j < outSize; j++)
                        for (int i = 0; i < hidden; i++)
                            weightsFlat[idx++] = (float)NextGaussian(rng, 0, 0.5f * wScale2);

                    int bi = 0;
                    for (int j = 0; j < hidden; j++) biasesFlat[bi++] = 0f;
                    for (int j = 0; j < outSize; j++) biasesFlat[bi++] = 0f;
                }
                else
                {
                    // Single layer
                    float wScale = 1f / Mathf.Sqrt(inSize);
                    int idx = 0;
                    for (int j = 0; j < outSize; j++)
                        for (int i = 0; i < inSize; i++)
                            weightsFlat[idx++] = (float)NextGaussian(rng, 0, 0.5f * wScale);

                    for (int j = 0; j < outSize; j++) biasesFlat[j] = 0f;
                }
            }
        }

        public Genome Clone()
        {
            var c = new Genome(1, 1, 1, false, null);
            c.gen = gen + 1;
            c.fitness = 0f;
            c.layers = (int[])layers.Clone();
            c.weightsFlat = (float[])weightsFlat.Clone();
            c.biasesFlat = (float[])biasesFlat.Clone();
            c.actionNames = actionNames != null ? (string[])actionNames.Clone() : null;
            return c;
        }

        public void Mutate(float rate, float sigma, System.Random rngLocal)
        {
            // Mutate weights
            for (int i = 0; i < weightsFlat.Length; i++)
            {
                if (rngLocal.NextDouble() < rate)
                    weightsFlat[i] += (float)NextGaussian(rngLocal, 0, sigma);
            }
            // Mutate biases
            for (int i = 0; i < biasesFlat.Length; i++)
            {
                if (rngLocal.NextDouble() < rate)
                    biasesFlat[i] += (float)NextGaussian(rngLocal, 0, sigma);
            }
        }

        public NeuralNetDef ToDef()
        {
            return new NeuralNetDef
            {
                layers = layers,
                weightsFlat = weightsFlat,
                biasesFlat = biasesFlat,
                actionNames = actionNames
            };
        }

        public TextAsset ToTextAsset(string nameHint)
        {
            string json = JsonUtility.ToJson(ToDef(), prettyPrint: false);
            var ta = new TextAsset(json);
            ta.name = nameHint;
            return ta;
        }

        static double NextGaussian(System.Random r, double mean, double stdDev)
        {
            double u1 = 1.0 - r.NextDouble();
            double u2 = 1.0 - r.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }

    static double NextGaussian(System.Random r, double mean, double stdDev)
    {
        double u1 = 1.0 - r.NextDouble();
        double u2 = 1.0 - r.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}