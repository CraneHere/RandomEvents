using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIEventRow : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI percentText;

    public void SetData(string name, string status)
    {
        nameText.text = name;
        percentText.text = status;
    }

    public void SetStatusText(string status)
    {
        percentText.text = status;
    }

    public void SetClickAction(UnityEngine.Events.UnityAction action)
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(action);
    }
}
