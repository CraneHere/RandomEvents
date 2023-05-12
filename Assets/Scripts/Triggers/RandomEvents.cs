using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomEvents : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] prefabsForTrigger;
    private int index;
    private string tagObject;

    public GameObject playerSettings;

    //StepsEvent
    public AudioSource audioSource;
    private Coroutine stepsSound;
    private GameObject audioCapacity;
    private float timeToEnable = 0.6f;
    private float timeStepsIncrement = 0.3f;
    private float timeBeforeDestroyAudio = 1.5f;
    private float generalTime = 9f;

    //StepInactive
    private Coroutine timeDisableAudio;
    private float timeScale = 300f;

    //SidePositionSetting
    [SerializeField] private Vector3 center;
    [SerializeField] private Vector3 size;
    private Vector3 centerRight;

    //ThunderEvent
    public AudioSource thunderSource;
    private Coroutine thunderSounds;
    public AudioClip[] thunderTypes;
    private GameObject[] thunderCapacity;
    private float timePlayingThunder = 10f;
    private int countOfTimes;

    //SideThunderPositionSetting
    private Vector3 centerThunder;
    private Vector3 sizeThunder = new Vector3(100, 10, 100);

    private void Awake()
    {
        centerRight = new Vector3(center.x / 2, center.y / 2, center.z / 0.6f);
    }

    private void OnTriggerStay(Collider other)
    {

        if (!other.CompareTag("Player"))
        {
            return;
        }

        index = Random.Range(0, prefabsForTrigger.Length - 1);
        tagObject = prefabsForTrigger[index].tag;
        switch (tagObject)
        {
            case ("triggerObjects/stepSound"):
                if (stepsSound == null && timeDisableAudio == null)
                {
                    timeDisableAudio = StartCoroutine(TimeDisableAll());
                    stepsSound = StartCoroutine(StepRoutine());
                }
                break;
            case ("triggerObjects/thunderSound"):
                if (thunderSounds == null && timeDisableAudio == null)
                {
                    timeDisableAudio = StartCoroutine(TimeDisableAll());
                    thunderSounds = StartCoroutine(ThunderRoutine());
                }
                break;
            default:
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {

    }

    private IEnumerator TimeDisableAll()
    {
        WaitForSeconds timeToWait = new WaitForSeconds(timeScale);
        yield return timeToWait;

        timeDisableAudio = null;
    }

    private IEnumerator ThunderRoutine()
    {
        yield return new WaitForSeconds(timeToEnable);
        WaitForSeconds timeToWait = new WaitForSeconds(timePlayingThunder);
        countOfTimes = Random.Range(4, 6);
        Vector3 generalPosition = default;
        while (0 < countOfTimes)
        {
            centerThunder = playerSettings.transform.position;
            generalPosition = centerThunder + new Vector3(UnityEngine.Random.Range(-sizeThunder.x / 2, sizeThunder.x / 2), UnityEngine.Random.Range(-sizeThunder.y, sizeThunder.y), UnityEngine.Random.Range(-sizeThunder.z / 2, sizeThunder.z / 2));
            Instantiate(thunderSource, generalPosition, Quaternion.identity);
            countOfTimes--;
        }

        yield return new WaitForSeconds(4f);

        thunderCapacity = GameObject.FindGameObjectsWithTag("triggerObjects/thunderSound");
        foreach (GameObject thunder in thunderCapacity)
        {
            thunder.GetComponent<AudioSource>().PlayOneShot(thunderTypes[Random.Range(0, thunderTypes.Length - 1)]);
            yield return timeToWait;
        }
        foreach (GameObject thunder in thunderCapacity)
        {
            Destroy(thunder);
        }
        thunderSounds = null;
    }

    private IEnumerator StepRoutine()
    {
        yield return new WaitForSeconds(timeToEnable);
        WaitForSeconds timeToWaitIncrement = new WaitForSeconds(timeStepsIncrement);
        WaitForSeconds timeToWaitDestroy = new WaitForSeconds(timeBeforeDestroyAudio);
        float timeElapse = 0;
        Vector3 positionRight = default;
        while (timeElapse < generalTime)
        {
            positionRight = centerRight + new Vector3(UnityEngine.Random.Range(-size.x / 3, size.x / 3), UnityEngine.Random.Range(-size.y / 4, size.y / 4), UnityEngine.Random.Range(-size.z / 8, size.z / 8));
            Instantiate(audioSource, positionRight, Quaternion.identity);
            yield return timeToWaitDestroy;
            audioCapacity = GameObject.FindGameObjectWithTag("triggerObjects/stepSound");
            Destroy(audioCapacity);
            timeElapse += 1.5f;
            yield return timeToWaitIncrement;
        }
        stepsSound = null;
    }
}
