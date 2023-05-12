using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomEvents : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] prefabsForTrigger;
    private int index;
    private string tagObject;

    private GameObject[] prefabManager;

    //StepsEvent
    public AudioSource audioSource;
    private Coroutine stepsSound;
    private GameObject audioCapacity;
    private Vector3 audioPosition;
    private float timeStepsToEnable = 0f;
    private float timeStepsIncrement = 0.3f;
    private float timeBeforeDestroyAudio = 1.5f;
    private float generalTime = 9f;

    //StepInactive
    private Coroutine timeDisableStepAudio;
    private float timeScale = 1f;

    //SidePositionSetting
    [SerializeField] private Vector3 center;
    [SerializeField] private Vector3 size;
    private Vector3 centerRight;
    private Vector3 centerLeft;

    private void Awake()
    {
        centerRight = new Vector3(center.x / 2, center.y / 2, center.z / 0.6f);
        centerLeft = new Vector3(center.x / 2, center.y / 2, center.z / 5.4f);
    }

    private void OnTriggerStay(Collider other)
    {

        if (!other.CompareTag("Player"))
        {
            return;
        }

        index = UnityEngine.Random.Range(0, prefabsForTrigger.Length - 1);
        tagObject = prefabsForTrigger[index].tag;
        switch (tagObject)
        {
            case ("triggerObjects/stepSound"):
                if (stepsSound == null && timeDisableStepAudio == null)
                {
                    stepsSound = StartCoroutine(StepRoutine());
                }
                break;
            default:
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {

    }

    private IEnumerator TimeDisableStep()
    {
        WaitForSeconds timeToWait = new WaitForSeconds(timeScale);
        for (int i = 0; i < 300; i++)
        {
            yield return timeToWait;
        }
        timeDisableStepAudio = null;
    }

    private IEnumerator StepRoutine()
    {
        yield return new WaitForSeconds(timeStepsToEnable);
        WaitForSeconds timeToWaitIncrement = new WaitForSeconds(timeStepsIncrement);
        WaitForSeconds timeToWaitDestroy = new WaitForSeconds(timeBeforeDestroyAudio);
        float timeElapse = 0;
        Vector3 positionRight = default;
        Vector3 positionLeft = default;
        while (timeElapse < generalTime)
        {
            int sidePosition = UnityEngine.Random.Range(1, 101);
            if (sidePosition % 2 == 0)
            {
                positionRight = centerRight + new Vector3(UnityEngine.Random.Range(-size.x / 3, size.x / 3), UnityEngine.Random.Range(-size.y / 4, size.y / 4), UnityEngine.Random.Range(-size.z / 8, size.z / 8));
            }
            else
            {
                positionLeft = centerLeft + new Vector3(UnityEngine.Random.Range(-size.x / 3, size.x / 3), UnityEngine.Random.Range(-size.y / 4, size.y / 4), UnityEngine.Random.Range(-size.z / 8, size.z / 8));
            }
            audioPosition = sidePosition % 2 == 0 ? positionRight : positionLeft;
            Instantiate(audioSource, audioPosition, Quaternion.identity);
            yield return timeToWaitDestroy;
            audioCapacity = GameObject.FindGameObjectWithTag("triggerObjects/stepSound");
            Destroy(audioCapacity);
            timeElapse += 1.5f;
            yield return timeToWaitIncrement;
        }

        timeDisableStepAudio = StartCoroutine(TimeDisableStep());
        stepsSound = null;
    }
}
