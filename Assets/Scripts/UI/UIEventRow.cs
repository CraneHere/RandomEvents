using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIEventRow : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI percentText;

    public void SetData(string name, float percent)
    {
        nameText.text = name;
        percentText.text = percent + "%";
    }

    public void SetClickAction(UnityEngine.Events.UnityAction action)
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(action);
    }
}
