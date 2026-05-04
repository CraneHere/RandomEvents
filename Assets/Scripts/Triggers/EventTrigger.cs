using UnityEngine;

public class EventTrigger : MonoBehaviour
{
    [SerializeField] EventManager eventManager;
    [SerializeField] GroupRandomEvents[] allowedGroups;

    private int[] groupIndices;
    private bool playerInside = false;

    void Start()
    {
        groupIndices = new int[allowedGroups.Length];
        for (int i = 0; i < allowedGroups.Length; i++)
        {
            groupIndices[i] = System.Array.IndexOf(eventManager.groups, allowedGroups[i]);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerInside) return;

        playerInside = true;
        for (int i = 0; i < groupIndices.Length; i++)
        {
            if (groupIndices[i] >= 0)
            {
                eventManager.EnableGroup(groupIndices[i], this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!playerInside) return;

        playerInside = false;
        for (int i = 0; i < groupIndices.Length; i++)
        {
            if (groupIndices[i] >= 0)
            {
                eventManager.DisableGroup(groupIndices[i], this);
            }
        }
    }
}
