using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    public bool CanMove { get; private set; } = true;
    private bool isSprinting => canSprint && Input.GetKey(sprintKey);
    private bool isJumping => characterController.isGrounded && Input.GetKeyDown(jumpKey);
    private bool shouldCrouching => Input.GetKey(crouchKey) && characterController.isGrounded && !duringCrouchAnimation;

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool useFootstepsAudio = true;
        
    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode zoomKey = KeyCode.E;
    [SerializeField] private KeyCode unlockCursorKey = KeyCode.Escape;
 
    [Header("Move Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 2.0f;
    [SerializeField] private float slopeSpeed = 8f;

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lockSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lockSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLockLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLockLimit = 80.0f;

    [Header("Jumping Parametr")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;

    [Header("Zoom Parameters")]
    [SerializeField] private float timeToZoom = 0.3f;
    [SerializeField] private float zoomFOV = 30f;
    private float defaultFOV;
    private Coroutine zoomRoutine;

    [Header("FootstepsAudio Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepSpeed = 1.5f;
    [SerializeField] private float sprintStepSpeed = 0.6f;
    [SerializeField] private AudioSource footstepsAudioSource;
    [SerializeField] private AudioClip[] defaultSounds = default;
    private float footstepsTimer = 0;
    private float GetCurrentOffset => isSprinting ? sprintStepSpeed * baseStepSpeed : isCrouching ? crouchStepSpeed * baseStepSpeed : baseStepSpeed;

    //SLIDING PARAMETERS
    private Vector3 hitPointNormal;
    

    private bool isSliding
    {
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
            {

                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }

    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput; 
    private float rotationX;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultFOV = playerCamera.fieldOfView;
        LockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CanMove = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CanMove = false;
    }

    private void HandleCursorToggle()
    {
        if (Input.GetKeyDown(unlockCursorKey))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        HandleCursorToggle();

        if (CanMove)
        {
            HandleMovement();
            HandleMouseMovement();
         
            if (canJump)
            {
                HandleJumping();
            }

            if (canCrouch)
            {
                HandleCrouching();
            }

            if (canZoom)
            {
                HandleZooming();
            }

            if (useFootstepsAudio)
            {
                HandleFootsteps();
            }

            ApplyFinalMovement();
        }  
    }

    private void HandleMovement()
    {
        currentInput = new Vector2((isSprinting && !isSliding ? sprintSpeed : isCrouching ? crouchSpeed : walkSpeed) * Input.GetAxis("Vertical"),
            (isSprinting && !isSliding ? sprintSpeed : isCrouching ? crouchSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;

        moveDirection = (transform.TransformDirection(Vector3.forward *
            currentInput.x) + transform.TransformDirection(Vector3.right * currentInput.y));

        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseMovement ()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lockSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLockLimit, lowerLockLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lockSpeedX, 0);
    }

    private void HandleJumping ()
    {
        if (isJumping && !isSliding)
        {
            moveDirection.y = jumpForce;

        }
    }

    private void HandleZooming()
    {
        if (Input.GetKeyDown(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToogleZoom(true));
        }

        if (Input.GetKeyUp(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }
            zoomRoutine = StartCoroutine(ToogleZoom(false));
        }
    }

    private void HandleCrouching()
    {
        if (shouldCrouching)
        {
            StartCoroutine(CrouchStand());
        }
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded) return;
        if (currentInput == Vector2.zero) return;

        footstepsTimer -= Time.deltaTime;

        if (footstepsTimer <= 0)
        {
            if (Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 3f))
            {
                switch (hit.collider.tag)
                {

                    default:
                        footstepsAudioSource.PlayOneShot(defaultSounds[UnityEngine.Random.Range(0, defaultSounds.Length - 1)]);
                        break;
                }
            }

            footstepsTimer = GetCurrentOffset;
        }
    }

    private void ApplyFinalMovement()
    {
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (willSlideOnSlopes && isSliding)
        {
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private IEnumerator ToogleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float startingFOV = playerCamera.fieldOfView;
        float timeElapsed = 0;

        while (timeElapsed < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, timeElapsed / timeToZoom);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }

    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
        {
            yield break;
        }

        duringCrouchAnimation = true;

        float timeElapsed = 0;
        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed/timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed/timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }
}