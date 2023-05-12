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
    [SerializeField] private bool canHeadBob = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool useFootstepsAudio = true;
    [SerializeField] private bool useInGroundAudio = true;
    [SerializeField] private bool useStamina = true;
        
    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode zoomKey = KeyCode.E;
    [SerializeField] private KeyCode interactKey = KeyCode.Q;
 
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

    [Header("Health Parameters")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float timeToRegenStarts = 3f;
    [SerializeField] private float valueRegenIncrement = 0.1f;
    [SerializeField] private float valueHealthIncrement = 1f;
    private Coroutine regenRoutine;
    private float currentHealth;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;
    public static Action<float> OnTakeDamage;

    [Header("Stamina Parameters")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaUseMultiplier = 5f;
    [SerializeField] private float timeToRegenStaminaStarts = 5f;
    [SerializeField] private float staminaRegenIncrement = 2f;
    [SerializeField] private float staminaTimeIncrement = 0.1f;
    private Coroutine staminaRoutine;
    private float currentStamina;
    public static Action<float> OnStaminaChange;

    [Header("Jumping Parametr")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private float standingCenterPoint = 0;
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
    [SerializeField] private AudioClip[] groundSounds = default;
    [SerializeField] private AudioClip[] rockGroundSounds = default;
    [SerializeField] private AudioClip[] defaultSounds = default;
    private float footstepsTimer = 0;
    private float GetCurrentOffset => isSprinting ? sprintStepSpeed * baseStepSpeed : isCrouching ? crouchStepSpeed * baseStepSpeed : baseStepSpeed;

    [Header("Interact Parameters")]
    [SerializeField] private LayerMask interactionLayer = default;
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    private Interactable currentInteractable;

    [Header("HeadBob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float speedBobSpeed = 18f;
    [SerializeField] private float speedBobAmount = 0.11f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;

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

    private void OnEnable()
    {
        OnTakeDamage += ApplyDmg;

    }

    private void OnDisable()
    {
        OnTakeDamage -= ApplyDmg;
    }

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYPos = playerCamera.transform.localPosition.y;
        defaultFOV = playerCamera.fieldOfView;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (CanMove)
        {
            HandleMovement();
            HandleMouseMovement();
            
            if (canHeadBob)
            {
                HandleHeadBob();
            }

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

            if (canInteract)
            {
                HandleInteractCheck();
                HandleInteractInput();
            }

            /*if (useStamina)
            {
                HandleStamina();
            }*/

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

    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;

        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed :
                isSprinting ? speedBobSpeed : walkBobSpeed);

            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : isSprinting ? speedBobAmount : walkBobAmount),
                playerCamera.transform.localPosition.z);
        }
    }

    private void HandleInteractCheck()
    {
        if (Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.gameObject.layer == 6 && (currentInteractable == null || hit.collider.gameObject.GetInstanceID() != currentInteractable.GetInstanceID()))
            {
                hit.collider.TryGetComponent(out currentInteractable);

                if (currentInteractable)
                {
                    currentInteractable.OnFocus();
                }
            }
        }
        else if (currentInteractable)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }
    }

    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null && Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayer))
        {
            currentInteractable.OnInteract();
        }
    }

    private void HandleJumping ()
    {
        if (isJumping && !isSliding)
        {
            moveDirection.y = jumpForce;

        }
    }

    private void HandleStamina()
    {
        if (isSprinting && currentInput != Vector2.zero)
        {
            if (staminaRoutine != null)
            {
                StopCoroutine(staminaRoutine);
                staminaRoutine = null;
            }    

            currentStamina -= staminaUseMultiplier * Time.deltaTime;

            if (currentStamina < 0) currentStamina = 0;

            OnStaminaChange?.Invoke(currentStamina);

            if (currentStamina <= 0) canSprint = false;
        }

        if (!isSprinting && currentStamina < maxStamina && staminaRoutine == null)
        {
            staminaRoutine = StartCoroutine(StaminaRegen());
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
                    case "footsteps/ground":
                        footstepsAudioSource.PlayOneShot(groundSounds[UnityEngine.Random.Range(0, groundSounds.Length - 1)]);
                        break;
                    case "footsteps/rockGround":
                        footstepsAudioSource.PlayOneShot(rockGroundSounds[UnityEngine.Random.Range(0, rockGroundSounds.Length - 1)]);
                        break;
                    default:
                        footstepsAudioSource.PlayOneShot(defaultSounds[UnityEngine.Random.Range(0, defaultSounds.Length - 1)]);
                        break;
                }
            }

            footstepsTimer = GetCurrentOffset;
        }
    }

    private void ApplyDmg(float dmg)
    {
        currentHealth -= dmg;
        OnDamage?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            KillPlayer();
        }
        else if (regenRoutine != null)
        {
            StopCoroutine(regenRoutine);
        }

        regenRoutine = StartCoroutine(HealthRegen());
    }

    private void KillPlayer()
    {
        currentHealth = 0;

        if (regenRoutine != null)
        {
            StopCoroutine(regenRoutine);
        }

        print("dead");
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

    private IEnumerator HealthRegen()
    {
        yield return new WaitForSeconds(timeToRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(valueRegenIncrement);

        while (currentHealth < maxHealth)
        {
            currentHealth += valueHealthIncrement;

            OnHeal?.Invoke(currentHealth);

            if (currentHealth > maxHealth) currentHealth = maxHealth;
            yield return timeToWait;
        }

        regenRoutine = null;
    }

    private IEnumerator StaminaRegen()
    {
        yield return new WaitForSeconds(timeToRegenStaminaStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(staminaTimeIncrement);

        while (currentStamina < maxStamina)
        {
            if (currentStamina >= 30)
            {
                canSprint = true;
            }
            currentStamina += staminaRegenIncrement;

            if (currentStamina > maxStamina) currentStamina = maxStamina;

            OnStaminaChange?.Invoke(currentStamina);

            yield return timeToWait;
        }

        staminaRoutine = null;
    }
}