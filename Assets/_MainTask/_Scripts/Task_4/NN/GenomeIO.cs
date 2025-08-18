using System;
using System.IO;
using UnityEngine;

/// Flat JSON schema used for saving/loading genomes.
/// Arrays are FLAT (row-major):
/// - w1 length = inSize * hiddenSize; index: i*hiddenSize + j  (input i -> hidden j)
/// - hBias length = hiddenSize
/// - w2 length = hiddenSize * outSize; index: j*outSize + k    (hidden j -> output k)
/// - oBias length = outSize
[Serializable]
public class FlatNetDTO
{
    public string name;
    public string notes;

    public int inSize;
    public int hiddenSize;
    public int outSize;

    public float[] w1;     // len = inSize * hiddenSize (row-major: i*H + j)
    public float[] hBias;  // len = hiddenSize
    public float[] w2;     // len = hiddenSize * outSize (row-major: j*O + k)
    public float[] oBias;  // len = outSize
}

/// Utilities to convert between a runtime Genome (NeuralNetwork) and flat JSON files.
public static class GenomeIO
{
    // -------------------- LOAD (from JSON string / TextAsset / file) --------------------

    public static Genome LoadFlatJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[GenomeIO] LoadFlatJson: input JSON was null/empty.");
            return null;
        }

        FlatNetDTO dto = null;
        try
        {
            dto = JsonUtility.FromJson<FlatNetDTO>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GenomeIO] JSON parse error: {ex}");
            return null;
        }

        if (!ValidateDto(dto, out var err))
        {
            Debug.LogError($"[GenomeIO] DTO invalid: {err}");
            return null;
        }

        // Create a genome with the right sizes
        var g = new Genome(dto.inSize, dto.hiddenSize, dto.outSize, initRandom: false);
        g.net.EnsureAllocated();

        // Fill weights/biases from flat arrays
        // w1: [in, hidden] from flat index i*H + j
        for (int i = 0; i < dto.inSize; i++)
            for (int j = 0; j < dto.hiddenSize; j++)
                g.net.w1[i, j] = dto.w1[i * dto.hiddenSize + j];

        // hBias
        Array.Copy(dto.hBias, g.net.hBias, dto.hiddenSize);

        // w2: [hidden, out] from flat index j*O + k
        for (int j = 0; j < dto.hiddenSize; j++)
            for (int k = 0; k < dto.outSize; k++)
                g.net.w2[j, k] = dto.w2[j * dto.outSize + k];

        // oBias
        Array.Copy(dto.oBias, g.net.oBias, dto.outSize);

        g.fitness = 0f;
        return g;
    }

    public static Genome LoadFlatJson(TextAsset textAsset)
    {
        if (textAsset == null)
        {
            Debug.LogError("[GenomeIO] LoadFlatJson(TextAsset): textAsset is null.");
            return null;
        }
        return LoadFlatJson(textAsset.text);
    }

    public static Genome LoadFlatFile(string absolutePath)
    {
        try
        {
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"[GenomeIO] LoadFlatFile: file not found: {absolutePath}");
                return null;
            }
            string json = File.ReadAllText(absolutePath);
            return LoadFlatJson(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GenomeIO] LoadFlatFile error: {ex}");
            return null;
        }
    }

    // -------------------- SAVE (to JSON string / file) --------------------

    /// Convert a Genome to a flat JSON string (does not write to disk).
    public static string ToFlatJson(Genome g, string name = null, string notes = null, bool pretty = true)
    {
        if (!ValidateGenome(g, out var err))
        {
            Debug.LogError($"[GenomeIO] ToFlatJson: genome invalid: {err}");
            return null;
        }

        int I = g.net.inSize, H = g.net.hiddenSize, O = g.net.outSize;

        var dto = new FlatNetDTO
        {
            name = name ?? "TrainedGenome",
            notes = notes ?? "Exported by GenomeIO",
            inSize = I,
            hiddenSize = H,
            outSize = O,
            w1 = new float[I * H],
            hBias = new float[H],
            w2 = new float[H * O],
            oBias = new float[O]
        };

        // Flatten w1 (i*H + j)
        for (int i = 0; i < I; i++)
            for (int j = 0; j < H; j++)
                dto.w1[i * H + j] = g.net.w1[i, j];

        // hBias
        Array.Copy(g.net.hBias, dto.hBias, H);

        // Flatten w2 (j*O + k)
        for (int j = 0; j < H; j++)
            for (int k = 0; k < O; k++)
                dto.w2[j * O + k] = g.net.w2[j, k];

        // oBias
        Array.Copy(g.net.oBias, dto.oBias, O);

        return JsonUtility.ToJson(dto, pretty);
    }

    /// Save a Genome to a .json file. If folderAbsolutePath is empty, saves to Assets/Brains.
    public static string SaveFlatJson(Genome g, string folderAbsolutePath, string fileNameNoExt, string name = null, string notes = null)
    {
        try
        {
            if (string.IsNullOrEmpty(fileNameNoExt))
            {
                Debug.LogError("[GenomeIO] SaveFlatJson: fileNameNoExt is null/empty.");
                return null;
            }

            if (string.IsNullOrEmpty(folderAbsolutePath))
                folderAbsolutePath = Path.Combine(Application.dataPath, "Brains");

            if (!Directory.Exists(folderAbsolutePath))
                Directory.CreateDirectory(folderAbsolutePath);

            var json = ToFlatJson(g, name, notes, pretty: true);
            if (json == null) return null;

            string path = Path.Combine(folderAbsolutePath, fileNameNoExt + ".json");
            File.WriteAllText(path, json);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log($"[GenomeIO] Saved genome → {path}");
            return path;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GenomeIO] SaveFlatJson failed: {ex}");
            return null;
        }
    }

    // -------------------- Validation helpers --------------------

    static bool ValidateDto(FlatNetDTO dto, out string error)
    {
        if (dto == null) { error = "DTO is null"; return false; }
        if (dto.inSize <= 0 || dto.hiddenSize <= 0 || dto.outSize <= 0)
        { error = $"Bad sizes: I={dto.inSize}, H={dto.hiddenSize}, O={dto.outSize}"; return false; }

        int I = dto.inSize, H = dto.hiddenSize, O = dto.outSize;
        if (dto.w1 == null || dto.w1.Length != I * H) { error = "w1 length mismatch"; return false; }
        if (dto.hBias == null || dto.hBias.Length != H) { error = "hBias length mismatch"; return false; }
        if (dto.w2 == null || dto.w2.Length != H * O) { error = "w2 length mismatch"; return false; }
        if (dto.oBias == null || dto.oBias.Length != O) { error = "oBias length mismatch"; return false; }

        error = null; return true;
    }

    static bool ValidateGenome(Genome g, out string error)
    {
        if (g == null || g.net == null) { error = "Genome or net is null"; return false; }
        var net = g.net;
        net.EnsureAllocated();

        int I = net.inSize, H = net.hiddenSize, O = net.outSize;
        if (net.w1 == null || net.w1.GetLength(0) != I || net.w1.GetLength(1) != H) { error = "net.w1 invalid"; return false; }
        if (net.hBias == null || net.hBias.Length != H) { error = "net.hBias invalid"; return false; }
        if (net.w2 == null || net.w2.GetLength(0) != H || net.w2.GetLength(1) != O) { error = "net.w2 invalid"; return false; }
        if (net.oBias == null || net.oBias.Length != O) { error = "net.oBias invalid"; return false; }

        error = null; return true;
    }
}
