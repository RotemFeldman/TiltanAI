using System;
using System.IO;
using UnityEngine;

[Serializable]
public class FlatNetDTO
{
    public string name;
    public string notes;
    public int inSize, hiddenSize, outSize;
    public float[] w1, hBias, w2, oBias;
}

public static class GenomeIO
{
    public static Genome LoadFlatJson(string json)
    {
        if (string.IsNullOrEmpty(json)) { Debug.LogError("[GenomeIO] Empty JSON"); return null; }
        FlatNetDTO dto = null;
        try { dto = JsonUtility.FromJson<FlatNetDTO>(json); }
        catch (Exception ex) { Debug.LogError($"[GenomeIO] Parse error: {ex}"); return null; }
        if (!ValidateDto(dto, out var err)) { Debug.LogError($"[GenomeIO] Invalid DTO: {err}"); return null; }

        var g = new Genome(dto.inSize, dto.hiddenSize, dto.outSize, initRandom:false);
        g.net.EnsureAllocated();

        for (int i=0;i<dto.inSize;i++)
            for (int j=0;j<dto.hiddenSize;j++)
                g.net.w1[i,j] = dto.w1[i*dto.hiddenSize + j];

        Array.Copy(dto.hBias, g.net.hBias, dto.hiddenSize);

        for (int j=0;j<dto.hiddenSize;j++)
            for (int k=0;k<dto.outSize;k++)
                g.net.w2[j,k] = dto.w2[j*dto.outSize + k];

        Array.Copy(dto.oBias, g.net.oBias, dto.outSize);
        g.fitness = 0f;
        return g;
    }

    public static Genome LoadFlatJson(TextAsset ta) => ta ? LoadFlatJson(ta.text) : null;

    public static Genome LoadFlatFile(string absPath)
    {
        try { return LoadFlatJson(File.ReadAllText(absPath)); }
        catch (Exception ex) { Debug.LogError($"[GenomeIO] Load file error: {ex}"); return null; }
    }

    public static string ToFlatJson(Genome g, string name=null, string notes=null, bool pretty=true)
    {
        if (!ValidateGenome(g, out var err)) { Debug.LogError($"[GenomeIO] ToFlatJson invalid: {err}"); return null; }
        int I=g.net.inSize, H=g.net.hiddenSize, O=g.net.outSize;
        var dto = new FlatNetDTO{
            name = name ?? "TrainedGenome",
            notes = notes ?? "Exported by GenomeIO",
            inSize = I, hiddenSize = H, outSize = O,
            w1 = new float[I*H], hBias = new float[H], w2 = new float[H*O], oBias = new float[O]
        };

        for (int i=0;i<I;i++) for (int j=0;j<H;j++) dto.w1[i*H+j] = g.net.w1[i,j];
        Array.Copy(g.net.hBias, dto.hBias, H);
        for (int j=0;j<H;j++) for (int k=0;k<O;k++) dto.w2[j*O+k] = g.net.w2[j,k];
        Array.Copy(g.net.oBias, dto.oBias, O);

        return JsonUtility.ToJson(dto, pretty);
    }

    public static string SaveFlatJson(Genome g, string folderAbsolutePath, string fileNameNoExt, string name=null, string notes=null)
    {
        try
        {
            if (string.IsNullOrEmpty(folderAbsolutePath))
                folderAbsolutePath = Path.Combine(Application.dataPath, "Brains");
            if (!Directory.Exists(folderAbsolutePath))
                Directory.CreateDirectory(folderAbsolutePath);

            var json = ToFlatJson(g, name, notes, true);
            if (json == null) return null;

            string path = Path.Combine(folderAbsolutePath, fileNameNoExt + ".json");
            File.WriteAllText(path, json);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log($"[GenomeIO] Saved → {path}");
            return path;
        }
        catch (Exception ex) { Debug.LogError($"[GenomeIO] Save error: {ex}"); return null; }
    }

    static bool ValidateDto(FlatNetDTO dto, out string error)
    {
        if (dto == null) { error="null dto"; return false; }
        int I=dto.inSize, H=dto.hiddenSize, O=dto.outSize;
        if (I<=0||H<=0||O<=0){ error="bad sizes"; return false; }
        if (dto.w1==null||dto.w1.Length!=I*H){ error="w1 len"; return false; }
        if (dto.hBias==null||dto.hBias.Length!=H){ error="hBias len"; return false; }
        if (dto.w2==null||dto.w2.Length!=H*O){ error="w2 len"; return false; }
        if (dto.oBias==null||dto.oBias.Length!=O){ error="oBias len"; return false; }
        error=null; return true;
    }

    static bool ValidateGenome(Genome g, out string error)
    {
        if (g==null||g.net==null){ error="null genome/net"; return false; }
        var n=g.net; n.EnsureAllocated();
        if (n.w1==null||n.w1.GetLength(0)!=n.inSize||n.w1.GetLength(1)!=n.hiddenSize){ error="w1"; return false; }
        if (n.hBias==null||n.hBias.Length!=n.hiddenSize){ error="hBias"; return false; }
        if (n.w2==null||n.w2.GetLength(0)!=n.hiddenSize||n.w2.GetLength(1)!=n.outSize){ error="w2"; return false; }
        if (n.oBias==null||n.oBias.Length!=n.outSize){ error="oBias"; return false; }
        error=null; return true;
    }
}
