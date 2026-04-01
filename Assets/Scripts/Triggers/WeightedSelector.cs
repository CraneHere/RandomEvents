using System;
using System.Linq;
using UnityEngine;

public class WeightedSelector
{
    public float[] weights;
    public float[] baseWeights;
    public float minWeight;
    public float maxWeight;
    public float pen = 0.08f;
    public float recoveryRate = 0.46f;

    public WeightedSelector(float[] initialWeights, float minWeight, float maxWeight)
    {
        weights = new float[initialWeights.Length];
        baseWeights = new float[initialWeights.Length];

        System.Array.Copy(initialWeights, weights, initialWeights.Length);
        System.Array.Copy(initialWeights, baseWeights, initialWeights.Length);

        float sum = weights.Sum();
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] /= sum;
            baseWeights[i] /= sum;
        }

        this.minWeight = minWeight;
        this.maxWeight = maxWeight;

    }

    public int SelectIndex()
    {
        float sumWeights = weights.Sum();
        float cumulative = 0;

        float randomValue = UnityEngine.Random.value * sumWeights;

        for (int i = 0; i < weights.Length; ++i)
        {
            cumulative += weights[i];
            if (cumulative >= randomValue)
            {
                ChancesRecount(i);
                return i;
            }
        }

        return 0;
    }

    private void ChancesRecount(int indexItem)
    {
        weights[indexItem] -= pen;
        float newPen = pen / (weights.Length - 1);

        for (int i = 0; i < weights.Length; ++i)
        {
            if (i != indexItem)
            {
                weights[i] += newPen;
            }
        }

        for (int i = 0; i < weights.Length; ++i)
        {
            weights[i] = Mathf.Clamp(weights[i], minWeight, maxWeight);
        }

        float sumWeights = weights.Sum();

        for (int i = 0; i < weights.Length; ++i)
        {
            weights[i] /= sumWeights;
        }
    }

    public void RecoverWeights()
    {
        for (int i = 0; i < weights.Length; ++i)
        {
            weights[i] = Mathf.Lerp(weights[i], baseWeights[i], recoveryRate);
        }

        float sumWeights = weights.Sum();

        for (int i = 0; i < weights.Length; ++i)
        {
            weights[i] /= sumWeights;
        }
    }

    public float GetWeightPercent(int index)
    {
        float sumWeights = weights.Sum();

        return weights[index] / sumWeights * 100;
    }
}
