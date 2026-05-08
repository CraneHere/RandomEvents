using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public EventUIManager eventUIManager;
    public bool isWeatherRainy = false;

    public GroupRandomEvents[] groups;
    public WeightedSelector[] eventSelectors;
    public float minWeight = 0.05f;
    public float maxWeight = 0.6f;

    private float[] nextAvailableTime;
    private float[] groupActiveUntilTime;
    private List<EventTrigger>[] activeTriggers;
    private Coroutine[] schedulers;

    private float GetRandomCooldown(GroupRandomEvents group)
    {
        float min = Mathf.Min(group.cooldownMin, group.cooldownMax);
        float max = Mathf.Max(group.cooldownMin, group.cooldownMax);
        return Random.Range(min, max);
    }

    private float GetRandomActiveDuration(GroupRandomEvents group)
    {
        float min = Mathf.Min(group.activeDurationMin, group.activeDurationMax);
        float max = Mathf.Max(group.activeDurationMin, group.activeDurationMax);
        return Random.Range(min, max);
    }

    void Awake()
    {
        eventSelectors = new WeightedSelector[groups.Length];

        for (int i = 0; i < groups.Length; i++)
        {
            float[] eventWeights = new float[groups[i].randomEvents.Length];

            for (int j = 0; j < eventWeights.Length; j++)
            {
                eventWeights[j] = groups[i].randomEvents[j].baseWeight;
            }

            eventSelectors[i] = new WeightedSelector(eventWeights, minWeight, maxWeight);
        }

        nextAvailableTime = new float[groups.Length];
        groupActiveUntilTime = new float[groups.Length];
        activeTriggers = new List<EventTrigger>[groups.Length];
        schedulers = new Coroutine[groups.Length];

        for (int i = 0; i < groups.Length; i++)
        {
            activeTriggers[i] = new List<EventTrigger>();
        }
    }

    public bool IsGroupAvailable(int groupIndex)
    {
        return Time.time >= nextAvailableTime[groupIndex];
    }

    public bool IsGroupActive(int groupIndex)
    {
        return Time.time < groupActiveUntilTime[groupIndex];
    }

    public float GetActiveRemaining(int groupIndex)
    {
        return Mathf.Max(0f, groupActiveUntilTime[groupIndex] - Time.time);
    }

    public bool IsGroupBlockedByExclusion(int groupIndex)
    {
        GroupRandomEvents[] exclusives = groups[groupIndex].exclusiveWith;
        if (exclusives == null) return false;

        for (int i = 0; i < exclusives.Length; i++)
        {
            int idx = System.Array.IndexOf(groups, exclusives[i]);
            if (idx < 0) continue;
            if (IsGroupActive(idx)) return true;
        }

        return false;
    }

    public float GetCooldownRemaining(int groupIndex)
    {
        return Mathf.Max(0f, nextAvailableTime[groupIndex] - Time.time);
    }

    public bool IsGroupScheduled(int groupIndex)
    {
        return schedulers[groupIndex] != null;
    }

    public void EnableGroup(int groupIndex, EventTrigger trigger)
    {
        if (!activeTriggers[groupIndex].Contains(trigger))
        {
            activeTriggers[groupIndex].Add(trigger);
        }

        if (schedulers[groupIndex] == null)
        {
            schedulers[groupIndex] = StartCoroutine(GroupScheduler(groupIndex));
        }
    }

    public void DisableGroup(int groupIndex, EventTrigger trigger)
    {
        activeTriggers[groupIndex].Remove(trigger);

        if (activeTriggers[groupIndex].Count == 0 && schedulers[groupIndex] != null)
        {
            StopCoroutine(schedulers[groupIndex]);
            schedulers[groupIndex] = null;
        }
    }

    private IEnumerator GroupScheduler(int groupIndex)
    {
        GroupRandomEvents group = groups[groupIndex];

        while (true)
        {
            if (!IsGroupAvailable(groupIndex))
            {
                float remaining = GetCooldownRemaining(groupIndex);
                if (remaining > 0f)
                {
                    yield return new WaitForSeconds(remaining);
                }
                continue;
            }

            if (IsGroupBlockedByExclusion(groupIndex))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (!IsGroupConditionMet(groupIndex))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (Random.value > group.fireChance)
            {
                if (group.applyCooldownOnMiss && Mathf.Max(group.cooldownMin, group.cooldownMax) > 0f)
                {
                    nextAvailableTime[groupIndex] = Time.time + Mathf.Max(0f, GetRandomCooldown(group));
                    continue;
                }
                yield return null;
                continue;
            }

            List<EventTrigger> triggers = activeTriggers[groupIndex];
            if (triggers.Count == 0) continue;

            Vector3 pos = triggers[Random.Range(0, triggers.Count)].transform.position;
            TriggerGroup(groupIndex, pos);
        }
    }

    public void TriggerGroup(int groupIndex, Vector3 spawnPosition)
    {
        if (!IsGroupAvailable(groupIndex)) return;
        if (!IsGroupConditionMet(groupIndex)) return;

        GroupRandomEvents group = groups[groupIndex];

        if (group.isContinuous)
        {
            float activeDuration = Mathf.Max(GetRandomActiveDuration(group), group.continuousEventInterval);
            float activeUntil = Time.time + activeDuration;
            nextAvailableTime[groupIndex] = activeUntil + Mathf.Max(0f, GetRandomCooldown(group));
            groupActiveUntilTime[groupIndex] = activeUntil;
            StartCoroutine(ContinuousGroupRoutine(groupIndex, spawnPosition, activeUntil));

            if (group.additionalEventsContinuous)
            {
                StartCoroutine(ContinuousEventRoutine(group, spawnPosition, activeUntil));
            }
        }
        else
        {
            int idEvent = eventSelectors[groupIndex].SelectIndex();
            RandomEvent randomEvent = groups[groupIndex].randomEvents[idEvent];

            float lockDuration = GetRandomCooldown(groups[groupIndex]);
            nextAvailableTime[groupIndex] = Time.time + lockDuration;

            SpawnEvent(randomEvent, spawnPosition, group);
        }

        if (eventUIManager != null)
        {
            eventUIManager.RefreshUI();
        }
    }

    public bool IsGroupConditionMet(int groupIndex)
    {
        GroupRandomEvents group = groups[groupIndex];
        if (group.requiresRainyWeather && !isWeatherRainy)
        {
            return false;
        }

        return true;
    }

    private IEnumerator ContinuousGroupRoutine(int groupIndex, Vector3 spawnPosition, float activeUntilTime)
    {
        GroupRandomEvents group = groups[groupIndex];

        while (Time.time < activeUntilTime)
        {
            if (!IsGroupConditionMet(groupIndex))
            {
                break;
            }

            int idEvent = eventSelectors[groupIndex].SelectIndex();
            RandomEvent randomEvent = group.randomEvents[idEvent];
            SpawnEvent(randomEvent, spawnPosition, group);

            float interval = Mathf.Max(0.1f, group.continuousEventInterval);
            yield return new WaitForSeconds(interval);
        }

    }

    private void SpawnEvent(RandomEvent randomEvent, Vector3 spawnPosition)
    {
        SpawnEvent(randomEvent, spawnPosition, null);
    }

    private void SpawnEvent(RandomEvent randomEvent, Vector3 spawnPosition, GroupRandomEvents group)
    {
        GameObject obj = Instantiate(randomEvent.prefab, spawnPosition, Quaternion.identity);

        if (group != null && group.continuousEventInterval > 0f)
        {
            Destroy(obj, group.continuousEventInterval);
        }

        if (group == null || !group.additionalEventsContinuous)
        {
            SpawnAdditionalEvents(group, spawnPosition);
        }
    }

    private IEnumerator ContinuousEventRoutine(GroupRandomEvents group, Vector3 spawnPosition, float activeUntilTime)
    {
        float interval = Mathf.Max(0.1f, group.additionalEventsInterval);

        if (group.additionalEventsDelay > 0f)
        {
            yield return new WaitForSeconds(group.additionalEventsDelay);
        }

        while (Time.time < activeUntilTime)
        {
            InstantiateAdditionalEvents(group, spawnPosition);
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnAdditionalEvents(GroupRandomEvents group, Vector3 spawnPosition)
    {
        if (group == null || group.additionalEvents == null || group.additionalEvents.Length == 0) return;

        if (group.additionalEventsDelay > 0f)
        {
            StartCoroutine(AdditionalEventsRoutine(group, spawnPosition));
        }
        else
        {
            InstantiateAdditionalEvents(group, spawnPosition);
        }
    }

    private void InstantiateAdditionalEvents(GroupRandomEvents group, Vector3 spawnPosition)
    {
        Vector3 position = spawnPosition;

        if (group.spawnAbovePlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                position = player.transform.position + Vector3.up * group.spawnHeightAbovePlayer;
            }
        }

        for (int i = 0; i < group.additionalEvents.Length; i++)
        {
            if (group.additionalEvents[i] == null) continue;
            GameObject obj = Instantiate(group.additionalEvents[i], position, group.additionalEvents[i].transform.rotation);
            if (group.additionalEventsLifetime > 0f)
            {
                Destroy(obj, group.additionalEventsLifetime);
            }
        }
    }

    private IEnumerator AdditionalEventsRoutine(GroupRandomEvents group, Vector3 spawnPosition)
    {
        yield return new WaitForSeconds(group.additionalEventsDelay);
        InstantiateAdditionalEvents(group, spawnPosition);
    }
}
