using UnityEngine;

[CreateAssetMenu(fileName = "GroupRandomEvents", menuName = "Scriptable Objects/GroupRandomEvents")]
public class GroupRandomEvents : ScriptableObject
{
    public RandomEvent[] randomEvents;
    public float weight;
}
