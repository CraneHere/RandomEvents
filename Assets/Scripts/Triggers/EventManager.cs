using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class EventManager : MonoBehaviour
{
    public EventUIManager eventUIManager;

    public GroupRandomEvents[] groups;
    public WeightedSelector groupSelector;
    public WeightedSelector[] eventSelectors;
    public float minWeight = 0.05f;
    public float maxWeight = 0.6f;

    float[] groupWeights;
    float[] eventWeights;

    void Awake()
    {
        groupWeights = new float[groups.Length];

        for (int i = 0; i < groups.Length; i++)
        {
            groupWeights[i] = groups[i].weight;
        }

        groupSelector = new WeightedSelector(groupWeights, minWeight, maxWeight);

        eventSelectors = new WeightedSelector[groups.Length];

        for (int i = 0; i < groups.Length; i++)
        {
            eventWeights = new float[groups[i].randomEvents.Length];

            for (int j = 0; j < eventWeights.Length; j++)
            {
                eventWeights[j] = groups[i].randomEvents[j].baseWeight;
            }

            eventSelectors[i] = new WeightedSelector(eventWeights, minWeight, maxWeight);
        }
    }

    private void TriggerEnter()
    {
        int idGroup = groupSelector.SelectIndex();

        int idEvent = eventSelectors[idGroup].SelectIndex();

        GameObject eventPrefab = groups[idGroup].randomEvents[idEvent].prefab;

        Instantiate(eventPrefab, eventPrefab.transform.position, Quaternion.identity);

        eventUIManager.RefreshUI();
    }

    private void OnTriggerEnter()
    {
        TriggerEnter();
    }
}
