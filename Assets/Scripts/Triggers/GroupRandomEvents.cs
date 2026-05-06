using UnityEngine;

[CreateAssetMenu(fileName = "GroupRandomEvents", menuName = "Scriptable Objects/GroupRandomEvents")]
public class GroupRandomEvents : ScriptableObject
{
    public RandomEvent[] randomEvents;

    [Header("Additional Prefabs")]
    public GameObject[] additionalPrefabs;
    public float additionalPrefabsDelay = 0f;
    public float additionalPrefabsLifetime = 2f;
    public bool spawnAbovePlayer = false;
    public float spawnHeightAbovePlayer = 20f;

    [Header("Timing")]
    [Range(0f, 1f)] public float fireChance = 1f;
    public float activeDurationMin = 20f;
    public float activeDurationMax = 40f;
    public float cooldownMin = 30f;
    public float cooldownMax = 60f;
    public bool applyCooldownOnMiss = true;
    public bool isContinuous = false;
    public float continuousEventInterval = 8f;

    [Header("Conditions")]
    public bool requiresRainyWeather = false;

    [Header("Exclusions")]
    public GroupRandomEvents[] exclusiveWith;
}
