using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionManager : MonoBehaviour
{
    [Header("Population")]
    public int population = 40;
    public int generations = 20;
    [Range(0f, 0.5f)] public float eliteFraction = 0.10f;

    [Header("Network Shape")]
    public int inputSize = 12;
    public int hiddenSize = 16;
    public int outputSize = 5;

    [Header("Mutation")]
    [Range(0f, 1f)] public float mutationRate = 0.08f;
    public float mutationSigma = 0.25f;

    [Header("Parallelism / Arenas")]
    public int parallelArenas = 8;
    public TrainingArena arenaPrefab;
    public Transform arenasParent;
    public float arenaSpacing = 50f;

    [Header("Saving")]
    public bool saveEveryGeneration = true;
    public int saveTopK = 3;
    public string saveFolderAbsolute = "";
    public bool maintainBestSoFar = true;
    public string notesTag = "Classic NeuroEvolution run";

    List<TrainingArena> arenas;
    List<Genome> pop;

    void Start()
    {
        if (!arenaPrefab){ Debug.LogError("[EvolutionManager] ArenaPrefab missing"); enabled=false; return; }
        BuildArenas();
        BuildInitialPopulation();
        StartCoroutine(TrainRoutine());
    }

    void BuildArenas()
    {
        arenas = new List<TrainingArena>(parallelArenas);
        for (int i=0;i<parallelArenas;i++)
        {
            var pos = new Vector3(i*arenaSpacing, 0f, 0f);
            var a = Instantiate(arenaPrefab, pos, Quaternion.identity, arenasParent);
            a.name = $"Arena_{i:00}";
            arenas.Add(a);
        }
    }

    void BuildInitialPopulation()
    {
        pop = new List<Genome>(population);
        for (int i=0;i<population;i++) pop.Add(new Genome(inputSize, hiddenSize, outputSize, true));
    }

    IEnumerator TrainRoutine()
    {
        for (int gen=0; gen<generations; gen++)
        {
            Debug.Log($"==== Generation {gen} ====");
            yield return EvaluateAll(pop);

            pop.Sort((a,b)=> b.fitness.CompareTo(a.fitness));

            if (saveEveryGeneration)
            {
                SaveTopK(pop, gen, saveTopK);
                if (maintainBestSoFar && pop.Count>0) SaveBestSoFar(pop[0], gen);
            }

            Debug.Log($"Gen {gen} best fitness = {pop[0].fitness:F2}");

            var next = new List<Genome>(population);
            int elites = Mathf.Max(1, Mathf.RoundToInt(population * eliteFraction));
            elites = Mathf.Min(elites, pop.Count);

            for (int i=0;i<elites;i++) next.Add(pop[i].Clone());

            var rng = new System.Random();
            while (next.Count < population)
            {
                var parent = TournamentSelect(pop, 3, rng);
                var child = parent.Clone();
                child.Mutate(mutationRate, mutationSigma, rng);
                next.Add(child);
            }
            pop = next;
        }

        yield return EvaluateAll(pop);
        pop.Sort((a,b)=> b.fitness.CompareTo(a.fitness));
        SaveTopK(pop, generations, Mathf.Max(saveTopK,1));
        if (maintainBestSoFar && pop.Count>0) SaveBestSoFar(pop[0], generations);
        Debug.Log("[EvolutionManager] Training complete.");
    }

    IEnumerator EvaluateAll(List<Genome> gens)
    {
        foreach (var a in arenas) a.Cleanup();
        int next = 0;
        var active = new Dictionary<TrainingArena, Genome>();

        while (next < gens.Count || active.Count > 0)
        {
            foreach (var a in arenas)
            {
                if (active.ContainsKey(a)) continue;
                if (next >= gens.Count) break;
                var g = gens[next++];
                a.Evaluate(g);
                active[a] = g;
            }

            var finished = new List<TrainingArena>();
            foreach (var kv in active) if (kv.Key.Done) finished.Add(kv.Key);
            foreach (var f in finished) { f.Cleanup(); active.Remove(f); }

            yield return null;
        }
    }

    void SaveTopK(List<Genome> genomes, int genIndex, int topK)
    {
        if (genomes == null || genomes.Count == 0) return;
        topK = Mathf.Clamp(topK, 1, genomes.Count);
        for (int i=0;i<topK;i++)
        {
            var g = genomes[i];
            string fileNoExt = $"BestGenome_gen{genIndex:D2}_rank{i+1}_fit{g.fitness:F2}";
            string prettyName = $"Gen {genIndex} Rank {i+1}";
            GenomeIO.SaveFlatJson(g, saveFolderAbsolute, fileNoExt, prettyName, notesTag);
        }
    }

    void SaveBestSoFar(Genome best, int genIndex)
    {
        GenomeIO.SaveFlatJson(best, saveFolderAbsolute, "BestSoFar", $"Best so far (gen {genIndex})", notesTag);
    }

    Genome TournamentSelect(List<Genome> pool, int k, System.Random rng)
    {
        if (pool == null || pool.Count == 0) throw new InvalidOperationException("Empty pool");
        k = Mathf.Clamp(k, 2, Mathf.Min(6, pool.Count));
        Genome best = null;
        for (int i=0;i<k;i++)
        {
            var cand = pool[rng.Next(pool.Count)];
            if (best == null || cand.fitness > best.fitness) best = cand;
        }
        return best;
    }
}
