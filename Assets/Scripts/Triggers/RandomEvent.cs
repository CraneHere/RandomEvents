using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "Scriptable Objects/Event")]
public class RandomEvent : ScriptableObject
{
    public float baseWeight;
    public GameObject prefab;

    public float eventDuration;
    public bool isTemporal;
}
