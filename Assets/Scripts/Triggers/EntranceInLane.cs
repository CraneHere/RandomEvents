using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntranceInLane : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] Vector3 position;
    public GameObject[] prefabsForTrigger;
    private int index;
    private string tagObject;

    [Header("Lamp Down")]
    public Rigidbody lampDown_rb;

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player"))
        {
            return;
        }

        index = UnityEngine.Random.Range(0, prefabsForTrigger.Length - 1);
        tagObject = prefabsForTrigger[index].tag;
        switch (tagObject)
        {
            case ("triggerObjects/ball"):
                if (GameObject.FindGameObjectWithTag("triggerObjects/ball") == true)
                {
                    break;
                }
                position = new Vector3(8, 1, -13);
                Instantiate(prefabsForTrigger[index], position, Quaternion.identity);
                break;
            case ("triggerObjects/lamp"):
                if (GameObject.FindGameObjectWithTag("triggerObjects/lamp") == true)
                {
                    break;
                }
                position = new Vector3(10, 3, -12);
                lampDown_rb.isKinematic = false;
                Instantiate(prefabsForTrigger[index], position, Quaternion.identity);
                break;
            default:
                print("None");
                break;
        }        
    }

    private void OnTriggerExit(Collider other)
    {
        
    }
}