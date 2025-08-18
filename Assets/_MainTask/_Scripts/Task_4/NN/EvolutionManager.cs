using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class EvolutionManager : MonoBehaviour
{
    [Header("Population")]
    public int population = 40;
    public int generations = 20;
    [Range(0f, 0.5f)] public float eliteFraction = 0.10f; // % kept unchanged

    [Header("Network Shape")]
    public int inputSize = 12;
    public int hiddenSize = 16;
    public int outputSize = 5; // [Search, Chase, Attack, Retreat, Drink]

    [Header("Mutation")]
    [Tooltip("Per-weight mutation probability.")]
    [Range(0f, 1f)] public float mutationRate = 0.08f;
    [Tooltip("Gaussian std-dev for weight/bias step size.")]
    public float mutationSigma = 0.25f;

    [Header("Parallelism / Arenas")]
    public int parallelArenas = 8;
    public TrainingArena arenaPrefab;
    public Transform arenasParent;
    public float arenaSpacing = 50f;

    [Header("Saving")]
    [Tooltip("Save top-K JSON genomes each generation.")]
    public bool saveEveryGeneration = true;
    public int saveTopK = 3;
    [Tooltip("Absolute folder path. Leave empty to save under Assets/Brains.")]
    public string saveFolderAbsolute = "";
    [Tooltip("Note string embedded into the saved JSONs.")]
    public string notesTag = "Classic NeuroEvolution run";
    [Tooltip("Also maintain Assets/Brains/BestSoFar.json each generation.")]
    public bool maintainBestSoFar = true;

    List<TrainingArena> arenas;
    List<Genome> pop;

    void Start()
    {
        if (arenaPrefab == null)
        {
            Debug.LogError("[EvolutionManager] ArenaPrefab is not assigned.");
            enabled = false;
            return;
        }

        BuildArenas();
        BuildInitialPopulation();
        StartCoroutine(TrainRoutine());
    }

    void BuildArenas()
    {
        arenas = new List<TrainingArena>(parallelArenas);

        // Simple line layout; you can change to grid if you like
        for (int i = 0; i < parallelArenas; i++)
        {
            Vector3 pos = new Vector3(i * arenaSpacing, 0f, 0f);
            var arena = Instantiate(arenaPrefab, pos, Quaternion.identity, arenasParent);
            arena.name = $"Arena_{i:00}";
            arenas.Add(arena);
        }
    }

    void BuildInitialPopulation()
    {
        pop = new List<Genome>(population);
        for (int i = 0; i < population; i++)
        {
            // initRandom=true generates small random weights
            pop.Add(new Genome(inputSize, hiddenSize, outputSize, initRandom: true));
        }
    }

    IEnumerator TrainRoutine()
    {
        for (int gen = 0; gen < generations; gen++)
        {
            Debug.Log($"==== Generation {gen} ====");
            yield return EvaluateAll(pop);

            // Sort by fitness (desc)
            pop.Sort((a, b) => b.fitness.CompareTo(a.fitness));

            // Save top-K for this generation
            if (saveEveryGeneration)
            {
                SaveTopK(pop, gen, saveTopK);
                if (maintainBestSoFar && pop.Count > 0)
                    SaveBestSoFar(pop[0], gen);
            }

            Debug.Log($"Gen {gen} best fitness = {pop[0].fitness:F2}");

            // Build next generation: elites unchanged, rest via mutate(clone)
            var next = new List<Genome>(population);

            int elites = Mathf.Max(1, Mathf.RoundToInt(population * eliteFraction));
            elites = Mathf.Min(elites, pop.Count);

            // keep elites
            for (int i = 0; i < elites; i++)
                next.Add(pop[i].Clone());

            // fill with mutated children
            System.Random rng = new System.Random();
            while (next.Count < population)
            {
                var parent = TournamentSelect(pop, 3, rng);
                var child = parent.Clone();
                child.Mutate(mutationRate, mutationSigma, rng);
                next.Add(child);
            }

            pop = next;
        }

        // Final eval and final saves
        yield return EvaluateAll(pop);
        pop.Sort((a, b) => b.fitness.CompareTo(a.fitness));
        SaveTopK(pop, generations, Mathf.Max(saveTopK, 1));
        if (maintainBestSoFar && pop.Count > 0)
            SaveBestSoFar(pop[0], generations);

        Debug.Log("[EvolutionManager] Training complete. Best genomes saved.");
    }

    IEnumerator EvaluateAll(List<Genome> genomes)
    {
        // reset arenas
        foreach (var a in arenas) a.Cleanup();

        // simple round-robin scheduler
        int next = 0;
        var active = new Dictionary<TrainingArena, Genome>();

        while (next < genomes.Count || active.Count > 0)
        {
            // fill idle arenas
            foreach (var a in arenas)
            {
                if (active.ContainsKey(a)) continue;
                if (next >= genomes.Count) break;

                var g = genomes[next++];
                a.Evaluate(g);
                active[a] = g;
            }

            // collect finished
            var finished = new List<TrainingArena>();
            foreach (var kv in active)
                if (kv.Key.Done) finished.Add(kv.Key);

            foreach (var f in finished)
            {
                f.Cleanup();
                active.Remove(f);
            }

            yield return null; // next frame
        }
    }

    void SaveTopK(List<Genome> genomes, int genIndex, int topK)
    {
        if (genomes == null || genomes.Count == 0) return;

        topK = Mathf.Clamp(topK, 1, genomes.Count);

        for (int i = 0; i < topK; i++)
        {
            var g = genomes[i];
            string fileNoExt = $"BestGenome_gen{genIndex:D2}_rank{i + 1}_fit{g.fitness:F2}";
            string prettyName = $"Gen {genIndex} Rank {i + 1}";
            GenomeIO.SaveFlatJson(
                g,
                folderAbsolutePath: saveFolderAbsolute, // "" -> Assets/Brains
                fileNameNoExt: fileNoExt,
                name: prettyName,
                notes: notesTag
            );
        }
    }

    void SaveBestSoFar(Genome best, int genIndex)
    {
        if (best == null) return;

        GenomeIO.SaveFlatJson(
            best,
            folderAbsolutePath: saveFolderAbsolute, // "" -> Assets/Brains
            fileNameNoExt: "BestSoFar",
            name: $"Best so far (gen {genIndex})",
            notes: notesTag
        );
    }

    // ---- selection helpers ----

    Genome TournamentSelect(List<Genome> pool, int k, System.Random rng)
    {
        if (pool == null || pool.Count == 0) throw new InvalidOperationException("Empty pool");
        k = Mathf.Clamp(k, 2, Mathf.Min(6, pool.Count));

        Genome best = null;
        for (int i = 0; i < k; i++)
        {
            var cand = pool[rng.Next(pool.Count)];
            if (best == null || cand.fitness > best.fitness) best = cand;
        }
        return best;
    }
}
