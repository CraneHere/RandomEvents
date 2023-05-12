using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    private void Awake()
    {
        gameObject.layer = 6;
    }

    public abstract void OnFocus();
    public abstract void OnLoseFocus();
    public abstract void OnInteract();
}
