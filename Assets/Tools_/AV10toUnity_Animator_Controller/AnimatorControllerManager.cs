using UnityEngine;
using System.Collections;

public class AnimatorControllerManager : MonoBehaviour
{
    [Header("Animator Settings")]
    [SerializeField] private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private bool isMoving = false;
    [SerializeField] private bool isInCombo = false;

    [Header("Combo Settings")]
    [SerializeField] private int currentComboIndex = 0;
    [SerializeField] private bool spaceKeyHeld = false;

    [Header("Debug Info")]
    [SerializeField] private string currentState = "wait";

    // Parameter name constants
    private const string COMBO_TRIGGER = "combo_trigger";
    private const string WAIT_TRIGGER = "wait_trigger";
    private const string RUN_TRIGGER = "run_trigger";
    private const string COMBO_BOOL = "combo_bool";
    private const string WAIT_BOOL = "wait_bool";
    private const string RUN_BOOL = "run_bool";

    // Input tracking
    private bool wasMovingLastFrame = false;
    private bool wasSpaceHeldLastFrame = false;

    void Start()
    {
        // Get animator component if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator component not found! Please assign an Animator to this GameObject.");
            enabled = false;
            return;
        }

        // Initialize to wait state
        SetToWaitState();
    }

    void Update()
    {
        HandleMovementInput();
        HandleComboInput();
        UpdateDebugInfo();
    }

    private void HandleMovementInput()
    {
        // Check for WASD input
        bool currentlyMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                              Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        // Movement state changed
        if (currentlyMoving != wasMovingLastFrame)
        {
            if (currentlyMoving)
            {
                // Start running
                StartRunning();
            }
            else
            {
                // Stop running
                StopRunning();
            }
        }

        wasMovingLastFrame = currentlyMoving;
        isMoving = currentlyMoving;
    }

    private void HandleComboInput()
    {
        bool currentlyHoldingSpace = Input.GetKey(KeyCode.Space);

        // Space key pressed (started holding)
        if (currentlyHoldingSpace && !wasSpaceHeldLastFrame)
        {
            ExecuteComboAttack();
        }
        // Space key released (stopped holding)
        else if (!currentlyHoldingSpace && wasSpaceHeldLastFrame)
        {
            ResetCombo();
        }

        wasSpaceHeldLastFrame = currentlyHoldingSpace;
        spaceKeyHeld = currentlyHoldingSpace;
    }

    private void StartRunning()
    {
        if (!isInCombo)
        {
            animator.SetBool(RUN_BOOL, true);
            animator.SetTrigger(RUN_TRIGGER);
            animator.SetBool(WAIT_BOOL, false);
            currentState = "run";
        }
        else
        {
            // If in combo and want to move, prepare for combo->run transition when space is released
            // Don't change state immediately, let combo finish naturally
        }
    }

    private void StopRunning()
    {
        if (!isInCombo)
        {
            animator.SetBool(WAIT_BOOL, true);
            animator.SetTrigger(WAIT_TRIGGER);
            animator.SetBool(RUN_BOOL, false);
            currentState = "wait";
        }
        else
        {
            // If in combo and stop moving, prepare for combo->wait transition when space is released
            // Don't change state immediately, let combo finish naturally
        }
    }

    private void ExecuteComboAttack()
    {
        if (!isInCombo)
        {
            // Start first combo
            StartCombo();
        }
        else
        {
            // Continue combo chain
            ContinueCombo();
        }
    }

    private void StartCombo()
    {
        isInCombo = true;
        currentComboIndex = 1;

        // Set combo parameters for direct transition
        animator.SetBool(COMBO_BOOL, true);
        animator.SetTrigger(COMBO_TRIGGER);
        animator.SetBool(RUN_BOOL, false);
        animator.SetBool(WAIT_BOOL, false);

        currentState = "combo_01";
    }

    private void ContinueCombo()
    {
        if (currentComboIndex < 7)
        {
            currentComboIndex++;
            animator.SetTrigger(COMBO_TRIGGER);
            currentState = $"combo_{currentComboIndex:D2}";
        }
        else
        {
            // Loop back to combo_01
            currentComboIndex = 1;
            animator.SetTrigger(COMBO_TRIGGER);
            currentState = "combo_01";
        }
    }


    private void ResetCombo()
    {
        isInCombo = false;
        currentComboIndex = 0;

        // Return to appropriate state based on movement
        if (isMoving)
        {
            // Direct transition from combo to run
            animator.SetBool(RUN_BOOL, true);
            animator.SetTrigger(RUN_TRIGGER);
            animator.SetBool(COMBO_BOOL, false);
            currentState = "run";
        }
        else
        {
            // Transition from combo to wait
            animator.SetBool(COMBO_BOOL, false);
            SetToWaitState();
        }
    }

    private void SetToWaitState()
    {
        animator.SetBool(WAIT_BOOL, true);
        animator.SetBool(RUN_BOOL, false);
        animator.SetBool(COMBO_BOOL, false);
        animator.SetTrigger(WAIT_TRIGGER);
        currentState = "wait";
    }

    private void UpdateDebugInfo()
    {
        // Update debug information for inspector
        // No timer updates needed anymore
    }

    // Public methods for external access
    public bool IsInCombo => isInCombo;
    public bool IsMoving => isMoving;
    public int CurrentComboIndex => currentComboIndex;
    public string CurrentState => currentState;
    public bool SpaceKeyHeld => spaceKeyHeld;

    // Method to manually reset combo (useful for external systems)
    public void ForceResetCombo()
    {
        ResetCombo();
    }

    void OnValidate()
    {
        // Ensure combo index is valid
        currentComboIndex = Mathf.Max(0, currentComboIndex);
    }
}
