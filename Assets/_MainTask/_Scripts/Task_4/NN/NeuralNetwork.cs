using UnityEngine;

/// Simple 1-hidden-layer MLP used by EnemyBrain
public class NeuralNetwork
{
    public readonly int inSize, hiddenSize, outSize;

    // weights/biases
    public float[,] w1;   // [in, hidden]
    public float[]  hBias;// [hidden]
    public float[,] w2;   // [hidden, out]
    public float[]  oBias;// [out]

    public NeuralNetwork(int inSize, int hiddenSize, int outSize, bool initRandom = true)
    {
        this.inSize = Mathf.Max(1, inSize);
        this.hiddenSize = Mathf.Max(1, hiddenSize);
        this.outSize = Mathf.Max(1, outSize);
        Allocate();

        if (initRandom) RandomInit(0.25f);
    }

    /// Ensure arrays exist and have the correct sizes (re-allocates if needed).
    public void EnsureAllocated()
    {
        if (w1 == null || w1.GetLength(0) != inSize || w1.GetLength(1) != hiddenSize) Allocate();
        if (hBias == null || hBias.Length != hiddenSize) hBias = new float[hiddenSize];
        if (w2 == null || w2.GetLength(0) != hiddenSize || w2.GetLength(1) != outSize) Allocate();
        if (oBias == null || oBias.Length != outSize) oBias = new float[outSize];
    }

    void Allocate()
    {
        w1 = new float[inSize, hiddenSize];
        hBias = new float[hiddenSize];
        w2 = new float[hiddenSize, outSize];
        oBias = new float[outSize];
    }

    void RandomInit(float scale)
    {
        var rng = new System.Random(1337);
        float Rand() => (float)((rng.NextDouble() * 2 - 1) * scale);

        for (int i = 0; i < inSize; i++)
            for (int h = 0; h < hiddenSize; h++)
                w1[i, h] = Rand();

        for (int h = 0; h < hiddenSize; h++)
            hBias[h] = Rand();

        for (int h = 0; h < hiddenSize; h++)
            for (int o = 0; o < outSize; o++)
                w2[h, o] = Rand();

        for (int o = 0; o < outSize; o++)
            oBias[o] = Rand();
    }

    /// Forward pass. Pads/truncates x to inSize. Never throws; returns zeros if something is off.
    public float[] Forward(float[] x)
    {
        // Input guard
        if (x == null) { Debug.LogWarning("NeuralNetwork.Forward: input was null; returning zeros."); return new float[outSize]; }

        EnsureAllocated();

        // Pad / truncate input to expected size
        float[] xi = new float[inSize];
        int n = Mathf.Min(inSize, x.Length);
        System.Array.Copy(x, xi, n);

        // hidden = ReLU(x * w1 + hBias)
        float[] hidden = new float[hiddenSize];
        for (int h = 0; h < hiddenSize; h++)
        {
            float sum = hBias[h];
            for (int i = 0; i < inSize; i++) sum += xi[i] * w1[i, h];
            // ReLU
            hidden[h] = sum > 0 ? sum : 0f;
        }

        // output = hidden * w2 + oBias (logits; no softmax)
        float[] outv = new float[outSize];
        for (int o = 0; o < outSize; o++)
        {
            float sum = oBias[o];
            for (int h = 0; h < hiddenSize; h++) sum += hidden[h] * w2[h, o];
            // NaN/Inf guard
            if (float.IsNaN(sum) || float.IsInfinity(sum)) sum = 0f;
            outv[o] = sum;
        }
        return outv;
    }

    // Convenience copy (used by Genome.Clone if you want)
    public void CopyFrom(NeuralNetwork other)
    {
        if (other == null) return;
        if (other.inSize != inSize || other.hiddenSize != hiddenSize || other.outSize != outSize)
        {
            Debug.LogWarning("NeuralNetwork.CopyFrom: size mismatch; reallocating.");
            // Not changing this network's sizes; just copy what fits
        }
        EnsureAllocated();
        other.EnsureAllocated();

        int I = Mathf.Min(inSize, other.inSize);
        int H = Mathf.Min(hiddenSize, other.hiddenSize);
        int O = Mathf.Min(outSize, other.outSize);

        for (int i = 0; i < I; i++)
            for (int h = 0; h < H; h++)
                w1[i, h] = other.w1[i, h];

        for (int h = 0; h < H; h++)
            hBias[h] = other.hBias[h];

        for (int h = 0; h < H; h++)
            for (int o = 0; o < O; o++)
                w2[h, o] = other.w2[h, o];

        for (int o = 0; o < O; o++)
            oBias[o] = other.oBias[o];
    }
}
