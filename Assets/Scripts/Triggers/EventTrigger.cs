using System.Linq;
using UnityEngine;

public class EventTrigger : MonoBehaviour
{
    [SerializeField] EventManager eventManager;
    [SerializeField] GroupRandomEvents[] allowedGroups;

    private WeightedSelector groupSelector;
    private int[] groupIndices;

    void Start()
    {
        float[] weights = new float[allowedGroups.Length];
        groupIndices = new int[allowedGroups.Length];

        for (int i = 0; i < allowedGroups.Length; i++)
        {
            weights[i] = allowedGroups[i].weight;
            groupIndices[i] = System.Array.IndexOf(eventManager.groups, allowedGroups[i]);
        }

        groupSelector = new WeightedSelector(weights, eventManager.minWeight, eventManager.maxWeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        int localIndex = groupSelector.SelectIndex();
        int globalIndex = groupIndices[localIndex];

        eventManager.TriggerGroup(globalIndex, transform.position);
    }
}
