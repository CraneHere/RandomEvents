using System.Collections.Generic;
using UnityEngine;

public class EventUIManager : MonoBehaviour
{
    [SerializeField] EventManager eventManager;
    [SerializeField] Transform leftPanel;
    [SerializeField] Transform rightPanel;
    [SerializeField] GameObject groupRowPrefab;
    [SerializeField] GameObject eventRowPrefab;

    private List<UIEventRow> groupRows = new List<UIEventRow>();
    private List<UIEventRow> eventRows = new List<UIEventRow>();
    private int selectedGroupIndex = -1;

    void Start()
    {
        for (int i = 0; i < eventManager.groups.Length; i++)
        {
            GameObject row = Instantiate(groupRowPrefab, leftPanel);
            UIEventRow uiRow = row.GetComponent<UIEventRow>();

            uiRow.SetData(eventManager.groups[i].name, GetGroupStatus(i));

            int index = i;
            uiRow.SetClickAction(() => OnGroupClicked(index));

            groupRows.Add(uiRow);
        }
    }

    void Update()
    {
        for (int i = 0; i < groupRows.Count; i++)
        {
            groupRows[i].SetStatusText(GetGroupStatus(i));
        }
    }

    private string GetGroupStatus(int groupIndex)
    {
        if (!eventManager.IsGroupAvailable(groupIndex))
        {
            return "LOCK " + Mathf.CeilToInt(eventManager.GetCooldownRemaining(groupIndex)) + "s";
        }

        if (eventManager.IsGroupBlockedByExclusion(groupIndex))
        {
            return "EXCL";
        }

        if (!eventManager.IsGroupConditionMet(groupIndex))
        {
            return "COND";
        }

        if (!eventManager.IsGroupScheduled(groupIndex))
        {
            return "IDLE";
        }

        return "READY";
    }

    private void OnGroupClicked(int groupIndex)
    {
        selectedGroupIndex = groupIndex;

        foreach (Transform child in rightPanel)
        {
            Destroy(child.gameObject);
        }
        eventRows.Clear();

        RandomEvent[] events = eventManager.groups[groupIndex].randomEvents;

        for (int j = 0; j < events.Length; j++)
        {
            GameObject row = Instantiate(eventRowPrefab, rightPanel);
            UIEventRow uiRow = row.GetComponent<UIEventRow>();

            float percent = eventManager.eventSelectors[groupIndex].GetWeightPercent(j);
            uiRow.SetData(events[j].name, percent.ToString("F1") + "%");

            eventRows.Add(uiRow);
        }
    }

    public void RefreshUI()
    {
        if (selectedGroupIndex >= 0)
        {
            RandomEvent[] events = eventManager.groups[selectedGroupIndex].randomEvents;

            for (int j = 0; j < eventRows.Count; j++)
            {
                float percent = eventManager.eventSelectors[selectedGroupIndex].GetWeightPercent(j);
                eventRows[j].SetData(events[j].name, percent.ToString("F1") + "%");
            }
        }
    }
}
