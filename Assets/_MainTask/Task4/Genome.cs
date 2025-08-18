using System;

[Serializable]
public class Genome
{
    public NeuralNetwork net;
    public float fitness;

    public Genome(int inSize, int hiddenSize, int outSize, bool initRandom = true)
    {
        net = new NeuralNetwork(inSize, hiddenSize, outSize, initRandom);
        net.EnsureAllocated();
        fitness = 0f;
    }

    public Genome Clone()
    {
        if (net == null) throw new InvalidOperationException("Genome.Clone: net is null.");
        var g = new Genome(net.inSize, net.hiddenSize, net.outSize, initRandom: false);
        g.net.EnsureAllocated();
        g.net.CopyFrom(this.net);
        g.fitness = 0f;
        return g;
    }

    public void Mutate(float rate, float sigma, System.Random rng = null)
    {
        if (net == null) return;
        rng ??= new System.Random();
        float NextGaussian()
        {
            var u1 = 1.0 - rng.NextDouble();
            var u2 = 1.0 - rng.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
        }

        net.EnsureAllocated();
        int I = net.inSize, H = net.hiddenSize, O = net.outSize;

        for (int i=0;i<I;i++) for (int h=0;h<H;h++)
            if (UnityEngine.Random.value < rate) net.w1[i,h] += NextGaussian()*sigma;

        for (int h=0;h<H;h++)
            if (UnityEngine.Random.value < rate) net.hBias[h] += NextGaussian()*sigma;

        for (int h=0;h<H;h++) for (int o=0;o<O;o++)
            if (UnityEngine.Random.value < rate) net.w2[h,o] += NextGaussian()*sigma;

        for (int o=0;o<O;o++)
            if (UnityEngine.Random.value < rate) net.oBias[o] += NextGaussian()*sigma;
    }
}