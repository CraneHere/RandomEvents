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
    private List<EventTrigger>[] activeTriggers;
    private Coroutine[] schedulers;

    private float timeToEnable = 0.6f;

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

    public bool IsGroupBlockedByExclusion(int groupIndex)
    {
        GroupRandomEvents[] exclusives = groups[groupIndex].exclusiveWith;
        if (exclusives == null) return false;

        for (int i = 0; i < exclusives.Length; i++)
        {
            int idx = System.Array.IndexOf(groups, exclusives[i]);
            if (idx < 0) continue;
            if (!IsGroupAvailable(idx)) return true;
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
            StartCoroutine(ContinuousGroupRoutine(groupIndex, spawnPosition, activeUntil));
        }
        else
        {
            int idEvent = eventSelectors[groupIndex].SelectIndex();
            RandomEvent randomEvent = groups[groupIndex].randomEvents[idEvent];

            float lockDuration = GetRandomCooldown(groups[groupIndex]);
            if (randomEvent.isTemporal)
            {
                lockDuration += randomEvent.eventDuration;
            }
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
                yield break;
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
        if (randomEvent.isTemporal)
        {
            StartCoroutine(EventRoutine(randomEvent, spawnPosition, group));
            return;
        }

        Instantiate(randomEvent.prefab, spawnPosition, Quaternion.identity);
        SpawnAdditionalPrefabs(group, spawnPosition);
    }

    private IEnumerator EventRoutine(RandomEvent randomEvent, Vector3 spawnPosition, GroupRandomEvents group)
    {
        yield return new WaitForSeconds(timeToEnable);
        Instantiate(randomEvent.prefab, spawnPosition, Quaternion.identity);
        SpawnAdditionalPrefabs(group, spawnPosition);
    }

    private void SpawnAdditionalPrefabs(GroupRandomEvents group, Vector3 spawnPosition)
    {
        if (group == null || group.additionalPrefabs == null || group.additionalPrefabs.Length == 0) return;

        if (group.additionalPrefabsDelay > 0f)
        {
            StartCoroutine(AdditionalPrefabsRoutine(group, spawnPosition));
        }
        else
        {
            InstantiateAdditionalPrefabs(group, spawnPosition);
        }
    }

    private void InstantiateAdditionalPrefabs(GroupRandomEvents group, Vector3 spawnPosition)
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

        for (int i = 0; i < group.additionalPrefabs.Length; i++)
        {
            if (group.additionalPrefabs[i] == null) continue;
            GameObject obj = Instantiate(group.additionalPrefabs[i], position, group.additionalPrefabs[i].transform.rotation);
            if (group.additionalPrefabsLifetime > 0f)
            {
                Destroy(obj, group.additionalPrefabsLifetime);
            }
        }
    }

    private IEnumerator AdditionalPrefabsRoutine(GroupRandomEvents group, Vector3 spawnPosition)
    {
        yield return new WaitForSeconds(group.additionalPrefabsDelay);
        InstantiateAdditionalPrefabs(group, spawnPosition);
    }
}
