using System;
using UnityEngine;

[Serializable]
public class NeuralNetDef
{
    public int[] layers;              // e.g. [7, 16, 5]
    public float[][][] weights;       // [L-1][out][in]
    public float[][] biases;          // [L-1][out]
    public string[] actionNames;      // optional, ["Search","Chase","Attack","Retreat","Potion"]

    // Flat backups for reliable JSON deserialization (JsonUtility-friendly)
    // If present, we rebuild 'weights' and 'biases' from these.
    public float[] weightsFlat;       // concatenated rows for each layer: layer0 rows (out0 * in0), then layer1 rows, etc.
    public float[] biasesFlat;        // concatenated biases for each layer: out0, then out1, etc.
}

public static class NeuralNet
{
    // Forward pass; x length must equal layers[0]
    public static void Forward(NeuralNetDef net, float[] x, float[] outBuf)
    {
        float[] a = x;
        for (int l = 0; l < net.layers.Length - 1; l++)
        {
            int inN = net.layers[l];
            int outN = net.layers[l + 1];

            float[] z = l == net.layers.Length - 2 ? outBuf : new float[outN];
            var W = net.weights[l];
            var b = net.biases[l];

            for (int j = 0; j < outN; j++)
            {
                float s = b[j];
                var wj = W[j];
                for (int i = 0; i < inN; i++) s += wj[i] * a[i];

                // Hidden: ReLU, Output: linear (we’ll argmax)
                z[j] = (l == net.layers.Length - 2) ? s : Mathf.Max(0f, s);
            }
            a = z;
        }
    }

    public static int ArgMax(float[] v)
    {
        int idx = 0; float best = v[0];
        for (int i = 1; i < v.Length; i++) if (v[i] > best) { best = v[i]; idx = i; }
        return idx;
    }
}