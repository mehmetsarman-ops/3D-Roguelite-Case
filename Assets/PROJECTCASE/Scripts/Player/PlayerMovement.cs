using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using RogueliteGame.Combat;
using RogueliteGame.Skills.UI;

namespace RogueliteGame.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string speedParameterName = "Speed";
        [SerializeField] private string isMovingParameterName = "IsMoving";
        [SerializeField] private string attackParameterName = "IsAttack";
        [SerializeField] private string isDeadParameterName = "IsDead";
        [SerializeField] private string hasEnemyParameterName = "IsEnemyDetection";
        [SerializeField] private string dodgeForwardParameterName = "DodgeF";
        [SerializeField] private string dodgeBackwardParameterName = "DodgeB";
        [SerializeField] private float movementSmoothTime = 0.1f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float combatRotationSpeed = 540f;
        [SerializeField] private bool rotateTowardsMovement = true;

        [Header("Input")]
        [SerializeField] private bool useCameraRelativeMovement = true;
        [SerializeField] private int attackMouseButton = 0;

        [Header("Mobile Joystick")]
        [SerializeField] private RogueliteGame.Input.MobileJoystick moveJoystick;

        [Header("Dodge")]
        [SerializeField] private float dodgeDistance = 3.5f;
        [SerializeField] private float dodgeDuration = 0.35f;
        [SerializeField] private float dodgeCooldown = 1.0f;
        [SerializeField] private bool invincibleDuringDodge = true;
        [SerializeField] private KeyCode dodgeKey = KeyCode.Space;
        [Header("Dodge Cooldown UI")]
        [SerializeField] private Image dodgeForwardCooldownFillImage;
        [SerializeField] private Image dodgeBackwardCooldownFillImage;

        [Header("Attack / Movement Lock")]
        [SerializeField] private bool lockMovementDuringAttack = true;
        [SerializeField] private float movementInputBufferTime = 0.2f;

        [Header("Movement Speed")]
        [SerializeField] private float movementSpeed = 4.5f;

        [Header("Initial Delay")]
        [Tooltip("Oyun başladığında karakterin hareket etmesini geciktirmek için süre (saniye). 0 ise gecikme yok.")]
        public float initialMovementDelay = 0f;

        [Header("Enemy Detection")]
        [SerializeField] private float detectionRadius = 8f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float detectionInterval = 0.15f;

        [Header("Combat")]
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private float damageDelay = 0.8f;
        [Header("Projectile")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform arrowSpawnPoint;
        [SerializeField] private Vector3 arrowSpawnLocalOffset = new Vector3(0f, 1.2f, 0.5f);
        [SerializeField] private Vector3 arrowTargetOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private float arrowSpeed = 20f;
        [SerializeField] private float arrowLifetime = 4f;

        [Header("Skill System")]
        [SerializeField] private RogueliteGame.Skills.PlayerSkillManager skillManager;

        [Header("Player Health")]
        [SerializeField] private float maxHealth = 100f;
        [Header("Player Health Bar")]
        [SerializeField] private float healthBarYOffset = 2f;
        [SerializeField] private Vector2 healthBarSize = new Vector2(120f, 14f);

        private Animator animator;
        private UnityEngine.Camera mainCamera;

        private float currentSpeed;
        private float smoothSpeedVelocity;
        private Vector3 currentMovementDirection;

        private Vector3 bufferedMovementDirection;
        private float bufferedMovementMagnitude;
        private float bufferedMovementTime;
        private bool hasBufferedMovement;

        private bool isDead;
        private float currentHealth;
        private bool isAttackInProgress;
        private bool isMovementLockedByAttack;
        private Coroutine attackRoutine;
        private bool wasInAttackStateLastFrame;
        private bool wasInAimRecoilStateLastFrame;
        private Transform healthBarRoot;
        private Image healthBarFill;

        private Quaternion? pendingCombatRotation;
        private Quaternion? pendingPostAttackRotation;

        private bool isDodging;
        private float dodgeElapsed;
        private Vector3 dodgeDirection;
        private float lastDodgeTime = -99f;
        private Coroutine dodgeRoutine;
        private Image activeDodgeCooldownFillImage;
        private bool hasActiveDodgeCooldownUI;

        private bool canMove = true;

        private readonly Collider[] detectedColliders = new Collider[20];
        private int enemyCount;
        private Transform nearestEnemy;
        private bool hasEnemyInRange;
        private float nextDetectionTime;

        private int speedHash;
        private int isMovingHash;
        private int attackHash;
        private int isDeadHash;
        private int hasEnemyHash;
        private int dodgeFHash;
        private int dodgeBHash;
        private bool speedParameterExists;
        private bool isMovingParameterExists;
        private bool attackParameterExists;
        private bool isDeadParameterExists;
        private bool hasEnemyParameterExists;
        private bool dodgeFParameterExists;
        private bool dodgeBParameterExists;
        private static readonly int WaitStateHash = Animator.StringToHash("wait");
        private static readonly int DrawArrowStateHash = Animator.StringToHash("Standing Draw Arrow");
        private static readonly int AimRecoilStateHash = Animator.StringToHash("Standing Aim Recoil");
        private static readonly int DodgeFStateHash = Animator.StringToHash("Standing Dodge Forward");
        private static readonly int DodgeBStateHash = Animator.StringToHash("Standing Dodge Backward");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            mainCamera = UnityEngine.Camera.main;
            ResolveAnimatorParameterNames();

            speedHash = Animator.StringToHash(speedParameterName);
            isMovingHash = Animator.StringToHash(isMovingParameterName);
            attackHash = Animator.StringToHash(attackParameterName);
            isDeadHash = Animator.StringToHash(isDeadParameterName);
            speedParameterExists = HasAnimatorParameter(speedParameterName, AnimatorControllerParameterType.Float);
            isMovingParameterExists = HasAnimatorParameter(isMovingParameterName, AnimatorControllerParameterType.Bool);
            attackParameterExists = HasAnimatorParameter(attackParameterName, AnimatorControllerParameterType.Bool);
            isDeadParameterExists = HasAnimatorParameter(isDeadParameterName, AnimatorControllerParameterType.Bool);
            hasEnemyHash = Animator.StringToHash(hasEnemyParameterName);
            hasEnemyParameterExists = HasAnimatorParameter(hasEnemyParameterName, AnimatorControllerParameterType.Bool);
            dodgeFHash = Animator.StringToHash(dodgeForwardParameterName);
            dodgeFParameterExists = HasAnimatorParameter(dodgeForwardParameterName, AnimatorControllerParameterType.Trigger);
            dodgeBHash = Animator.StringToHash(dodgeBackwardParameterName);
            dodgeBParameterExists = HasAnimatorParameter(dodgeBackwardParameterName, AnimatorControllerParameterType.Trigger);
            EnsureBaseLayerWeight();
            ResolveEnemyLayerMaskIfNeeded();

            currentHealth = maxHealth;
            CreatePlayerHealthBar();
            RefreshPlayerHealthBar();
            InitializeDodgeCooldownUI();
            UpdateDodgeCooldownUI();

            if (skillManager == null)
                skillManager = GetComponent<RogueliteGame.Skills.PlayerSkillManager>();

            if (initialMovementDelay > 0f)
            {
                canMove = false;
                StartCoroutine(EnableMovementAfterDelay());
            }
        }

        private IEnumerator EnableMovementAfterDelay()
        {
            yield return new WaitForSeconds(initialMovementDelay);
            canMove = true;
        }

        private void ResolveAnimatorParameterNames()
        {
            speedParameterName = ResolveAnimatorParameterName(speedParameterName, "Speed", AnimatorControllerParameterType.Float);
            isMovingParameterName = ResolveAnimatorParameterName(isMovingParameterName, "IsMoving", AnimatorControllerParameterType.Bool);
            attackParameterName = ResolveAnimatorParameterName(attackParameterName, "IsAttack", AnimatorControllerParameterType.Bool);
            isDeadParameterName = ResolveAnimatorParameterName(isDeadParameterName, "IsDead", AnimatorControllerParameterType.Bool);
            hasEnemyParameterName = ResolveAnimatorParameterName(hasEnemyParameterName, "IsEnemyDetection", AnimatorControllerParameterType.Bool);
            dodgeForwardParameterName = ResolveAnimatorParameterName(dodgeForwardParameterName, "DodgeF", AnimatorControllerParameterType.Trigger);
            dodgeBackwardParameterName = ResolveAnimatorParameterName(dodgeBackwardParameterName, "DodgeB", AnimatorControllerParameterType.Trigger);
        }

        private string ResolveAnimatorParameterName(string configuredName, string preferredName, AnimatorControllerParameterType expectedType)
        {
            if (HasAnimatorParameter(configuredName, expectedType))
                return configuredName;

            if (HasAnimatorParameter(preferredName, expectedType))
                return preferredName;

            return configuredName;
        }

        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
        {
            if (animator == null || string.IsNullOrWhiteSpace(parameterName)) return false;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == parameterName && parameter.type == expectedType)
                    return true;
            }

            return false;
        }

        private void EnsureBaseLayerWeight()
        {
            if (animator == null || animator.layerCount <= 0) return;
            if (animator.GetLayerWeight(0) < 0.99f)
                animator.SetLayerWeight(0, 1f);
        }

        private void Update()
        {
            UpdateDodgeCooldownUI();
            if (isDead) return;

            HandleDodgeInput();

            HandleMovementInput();
            DetectEnemies();

            if (IsInDodgeLockState())
            {
                if (animator != null) animator.speed = 1f;
                UpdateAnimator();
                return;
            }
            HandleCombat();
            ApplyAttackSpeedModifier();
            HandleAttackDamageByAnimationState();
            HandleAttackStateTransition();
            ApplyMovementBySpeed();
            UpdateAnimator();
            HandleRotation();
        }

        private void LateUpdate()
        {
        }

        private void OnDisable()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            isAttackInProgress = false;
            isMovementLockedByAttack = false;
            isDodging = false;
            wasInAttackStateLastFrame = false;
            wasInAimRecoilStateLastFrame = false;
            if (dodgeRoutine != null) { StopCoroutine(dodgeRoutine); dodgeRoutine = null; }
            if (animator != null)
            {
                if (attackParameterExists) animator.SetBool(attackHash, false);
                if (dodgeFParameterExists) animator.ResetTrigger(dodgeFHash);
                if (dodgeBParameterExists) animator.ResetTrigger(dodgeBHash);
            }
        }

        private void SetAttackFlag(bool isAttackActive)
        {
            if (animator == null || !attackParameterExists) return;
            animator.SetBool(attackHash, isAttackActive);

            if (isAttackActive)
            {
                isAttackInProgress = true;
                if (lockMovementDuringAttack) isMovementLockedByAttack = true;
            }
            else
            {
                isAttackInProgress = false;
                isMovementLockedByAttack = false;
            }
        }

        private void HandleAttackDamageByAnimationState()
        {
            if (animator == null) return;

            bool inAimRecoil = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AimRecoilStateHash;
            if (inAimRecoil && !wasInAimRecoilStateLastFrame)
            {
                if (nearestEnemy != null && nearestEnemy.gameObject.activeInHierarchy)
                {
                    if (arrowPrefab != null) SpawnSkillArrows(nearestEnemy);
                    else ApplyDirectDamage(nearestEnemy);
                }
            }

            wasInAimRecoilStateLastFrame = inAimRecoil;
        }

        private void HandleMovementInput()
        {
            if (!canMove)
            {
                currentSpeed = 0f;
                smoothSpeedVelocity = 0f;
                return;
            }

            float horizontal = UnityEngine.Input.GetAxisRaw("Horizontal");
            float vertical = UnityEngine.Input.GetAxisRaw("Vertical");
            Vector3 keyboardInput = new Vector3(horizontal, 0f, vertical);
            if (keyboardInput.magnitude > 1f) keyboardInput.Normalize();

            Vector3 joystickInput = Vector3.zero;
            if (moveJoystick != null)
            {
                Vector2 joyDir = moveJoystick.InputDirection;
                joystickInput = new Vector3(joyDir.x, 0f, joyDir.y);
            }

            Vector3 inputDirection = keyboardInput.sqrMagnitude >= joystickInput.sqrMagnitude
                ? keyboardInput
                : joystickInput;

            Vector3 desiredDirection = useCameraRelativeMovement
                ? GetCameraRelativeDirection(inputDirection)
                : inputDirection;

            float desiredMagnitude = inputDirection.magnitude;

            // isAttackInProgress kullandım çünkü SetAttackFlag(false) çağrıldığında hareket anında açılsın, animator geçişini beklemesin
            bool movementLocked = lockMovementDuringAttack && isAttackInProgress;
            if (movementLocked)
            {
                if (desiredMagnitude > 0.01f)
                {
                    currentMovementDirection = desiredDirection;

                    bufferedMovementDirection = desiredDirection.normalized;
                    bufferedMovementMagnitude = desiredMagnitude;
                    bufferedMovementTime = Time.time;
                    hasBufferedMovement = true;
                }

                currentSpeed = 0f;
                smoothSpeedVelocity = 0f;
                return;
            }

            currentMovementDirection = desiredDirection;

            float targetSpeed = desiredMagnitude;
            currentSpeed = Mathf.SmoothDamp(
                currentSpeed, targetSpeed, ref smoothSpeedVelocity, movementSmoothTime);
        }

        private Vector3 GetCameraRelativeDirection(Vector3 inputDirection)
        {
            if (mainCamera == null || inputDirection.magnitude < 0.01f)
                return inputDirection;

            Vector3 fwd = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();

            return fwd * inputDirection.z + right * inputDirection.x;
        }

        private void UpdateAnimator()
        {
            if (animator != null)
            {
                float speedForAnimator = currentSpeed;
                if (lockMovementDuringAttack && isAttackInProgress)
                    speedForAnimator = 0f;
                bool isMovingForAnimator = speedForAnimator > 0.1f;
                float normalizedSpeed = isMovingForAnimator ? 1f : 0f;

                if (speedParameterExists)
                    animator.SetFloat(speedHash, normalizedSpeed);
                if (isMovingParameterExists)
                    animator.SetBool(isMovingHash, isMovingForAnimator);
                if (hasEnemyParameterExists)
                    animator.SetBool(hasEnemyHash, hasEnemyInRange);
            }
        }

        private void HandleRotation()
        {
            if (pendingCombatRotation.HasValue)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, pendingCombatRotation.Value, combatRotationSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, pendingCombatRotation.Value) < 0.5f)
                {
                    transform.rotation = pendingCombatRotation.Value;
                    pendingCombatRotation = null;
                }
                return;
            }

            if (pendingPostAttackRotation.HasValue)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, pendingPostAttackRotation.Value, rotationSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, pendingPostAttackRotation.Value) < 0.5f)
                {
                    transform.rotation = pendingPostAttackRotation.Value;
                    pendingPostAttackRotation = null;
                }
                return;
            }

            if (IsInAttackAnimatorState()) return;
            if (!rotateTowardsMovement || currentMovementDirection.magnitude < 0.1f)
                return;

            Quaternion target = Quaternion.LookRotation(currentMovementDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        private void HandleAttackStateTransition()
        {
            bool inAttackState = IsInAttackAnimatorState();

            if (lockMovementDuringAttack && wasInAttackStateLastFrame && !inAttackState)
            {
                isMovementLockedByAttack = false;
                if (hasBufferedMovement && (Time.time - bufferedMovementTime) <= movementInputBufferTime)
                {
                    currentMovementDirection = bufferedMovementDirection;
                    currentSpeed = bufferedMovementMagnitude;
                    smoothSpeedVelocity = 0f;
                }

                hasBufferedMovement = false;
            }

            // Saldırıdan çıkınca snap yerine smooth dönüş yaptım, ani dönüş kötü görünüyordu
            if (wasInAttackStateLastFrame && !inAttackState && currentMovementDirection.sqrMagnitude > 0.0001f)
            {
                pendingPostAttackRotation = Quaternion.LookRotation(currentMovementDirection.normalized);
                pendingCombatRotation = null;
            }

            wasInAttackStateLastFrame = inAttackState;
        }

        private void ApplyMovementBySpeed()
        {
            if (isDodging) return;
            if (lockMovementDuringAttack && isAttackInProgress) return;
            if (currentMovementDirection.sqrMagnitude < 0.0001f || currentSpeed < 0.01f) return;

            Vector3 move = currentMovementDirection.normalized * (movementSpeed * currentSpeed * Time.deltaTime);
            transform.position += move;
        }

        private void HandleDodgeInput()
        {
            if (isDodging) return;
            if (UnityEngine.Input.GetKeyDown(dodgeKey))
                TriggerDodge();
        }

        private void SetImageFillAmount(Image image, float amount)
        {
            if (image == null) return;
            image.fillAmount = Mathf.Clamp01(amount);
        }

        private void InitializeDodgeCooldownUI()
        {
            SetImageFillAmount(dodgeForwardCooldownFillImage, 1f);
            SetImageFillAmount(dodgeBackwardCooldownFillImage, 1f);
            activeDodgeCooldownFillImage = null;
            hasActiveDodgeCooldownUI = false;
        }

        private void BeginDodgeCooldownUI(DodgeMode dodgeMode)
        {
            switch (dodgeMode)
            {
                case DodgeMode.Forward:
                    activeDodgeCooldownFillImage = dodgeForwardCooldownFillImage;
                    SetImageFillAmount(dodgeForwardCooldownFillImage, 0f);
                    SetImageFillAmount(dodgeBackwardCooldownFillImage, 1f);
                    hasActiveDodgeCooldownUI = activeDodgeCooldownFillImage != null;
                    break;

                case DodgeMode.Backward:
                    activeDodgeCooldownFillImage = dodgeBackwardCooldownFillImage;
                    SetImageFillAmount(dodgeBackwardCooldownFillImage, 0f);
                    SetImageFillAmount(dodgeForwardCooldownFillImage, 1f);
                    hasActiveDodgeCooldownUI = activeDodgeCooldownFillImage != null;
                    break;
            }
        }

        private float GetDodgeCooldownProgress01()
        {
            if (dodgeCooldown <= 0f) return 1f;

            float totalCycle = Mathf.Max(0.001f, dodgeDuration + dodgeCooldown);
            float remaining;

            if (isDodging)
            {
                float dodgeRemaining = Mathf.Max(0f, dodgeDuration - dodgeElapsed);
                remaining = dodgeRemaining + dodgeCooldown;
            }
            else
            {
                remaining = Mathf.Max(0f, dodgeCooldown - (Time.time - lastDodgeTime));
            }

            return Mathf.Clamp01(1f - (remaining / totalCycle));
        }

        private void UpdateDodgeCooldownUI()
        {
            if (!hasActiveDodgeCooldownUI || activeDodgeCooldownFillImage == null) return;

            float progress = GetDodgeCooldownProgress01();
            SetImageFillAmount(activeDodgeCooldownFillImage, progress);

            if (!isDodging && progress >= 1f)
            {
                hasActiveDodgeCooldownUI = false;
                activeDodgeCooldownFillImage = null;
            }
        }

        private enum DodgeMode
        {
            Auto,
            Forward,
            Backward
        }

        private bool HasMovementIntentInput()
        {
            float horizontal = UnityEngine.Input.GetAxisRaw("Horizontal");
            float vertical = UnityEngine.Input.GetAxisRaw("Vertical");
            if ((horizontal * horizontal) + (vertical * vertical) > 0.01f)
                return true;

            if (moveJoystick != null && moveJoystick.InputDirection.sqrMagnitude > 0.01f)
                return true;

            return false;
        }

        public void TriggerDodge()
        {
            TryStartDodge(DodgeMode.Auto);
        }

        public void TriggerDodgeForward()
        {
            TryStartDodge(DodgeMode.Forward);
        }

        public void TriggerDodgeBackward()
        {
            TryStartDodge(DodgeMode.Backward);
        }

        private void TryStartDodge(DodgeMode dodgeMode)
        {
            if (isDead || isDodging) return;
            if (Time.time - lastDodgeTime < dodgeCooldown) return;

            if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }
            isAttackInProgress = false;
            isMovementLockedByAttack = false;
            wasInAimRecoilStateLastFrame = false;
            SetAttackFlag(false);

            bool hasMovementInput = currentMovementDirection.sqrMagnitude > 0.01f;
            bool isForwardDodge;

            switch (dodgeMode)
            {
                case DodgeMode.Forward:
                    isForwardDodge = true;
                    dodgeDirection = hasMovementInput
                        ? currentMovementDirection.normalized
                        : transform.forward;
                    break;

                case DodgeMode.Backward:
                    isForwardDodge = false;
                    dodgeDirection = -transform.forward;
                    break;

                default:
                    isForwardDodge = hasMovementInput;
                    dodgeDirection = isForwardDodge
                        ? currentMovementDirection.normalized
                        : -transform.forward;
                    break;
            }

            if (isForwardDodge && dodgeDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dodgeDirection);
            }
            pendingCombatRotation = null;
            pendingPostAttackRotation = null;

            if (animator != null)
            {
                if (isForwardDodge && dodgeFParameterExists)
                    animator.SetTrigger(dodgeFHash);
                else if (!isForwardDodge && dodgeBParameterExists)
                    animator.SetTrigger(dodgeBHash);
            }

            currentSpeed = 0f;
            smoothSpeedVelocity = 0f;
            BeginDodgeCooldownUI(isForwardDodge ? DodgeMode.Forward : DodgeMode.Backward);

            dodgeRoutine = StartCoroutine(DodgeCoroutine());
        }

        private IEnumerator DodgeCoroutine()
        {
            isDodging = true;
            dodgeElapsed = 0f;

            float speed = dodgeDistance / dodgeDuration;

            while (dodgeElapsed < dodgeDuration)
            {
                float t = dodgeElapsed / dodgeDuration;
                float easeFactor = 1f - (t * t);
                float frameSpeed = speed * Mathf.Max(easeFactor, 0.15f);

                transform.position += dodgeDirection * (frameSpeed * Time.deltaTime);
                dodgeElapsed += Time.deltaTime;
                yield return null;
            }

            isDodging = false;
            lastDodgeTime = Time.time;
            dodgeRoutine = null;

            if (!isDead && !IsInDodgeLockState() && hasEnemyInRange && nearestEnemy != null && !IsMoving() && !HasMovementIntentInput())
            {
                FaceNearestEnemy();
                TriggerAttack();
            }
        }

        public bool IsDodging => isDodging;

        private bool IsInDodgeLockState()
        {
            return isDodging || IsInDodgeAnimatorState();
        }

        private bool IsInDodgeAnimatorState()
        {
            if (animator == null) return false;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == DodgeFStateHash || state.shortNameHash == DodgeBStateHash)
                return true;
            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                return next.shortNameHash == DodgeFStateHash || next.shortNameHash == DodgeBStateHash;
            }
            return false;
        }

        private void DetectEnemies()
        {
            if (Time.time < nextDetectionTime) return;
            nextDetectionTime = Time.time + detectionInterval;
            int layerMask = enemyLayer.value;
            if (layerMask == 0)
            {
                hasEnemyInRange = false;
                nearestEnemy = null;
                enemyCount = 0;
                return;
            }

            enemyCount = Physics.OverlapSphereNonAlloc(
                transform.position, detectionRadius, detectedColliders, layerMask);

            hasEnemyInRange = false;
            nearestEnemy = null;

            float closest = float.MaxValue;
            for (int i = 0; i < enemyCount; i++)
            {
                if (!TryGetEnemyTransform(detectedColliders[i], out Transform enemyTransform))
                    continue;

                float d = Vector3.SqrMagnitude(enemyTransform.position - transform.position);
                if (d < closest)
                {
                    closest = d;
                    nearestEnemy = enemyTransform;
                }
            }

            hasEnemyInRange = nearestEnemy != null;
        }

        private void ResolveEnemyLayerMaskIfNeeded()
        {
            if (enemyLayer.value != 0) return;

            int detectedEnemyLayer = LayerMask.NameToLayer("Enemy");
            if (detectedEnemyLayer >= 0)
            {
                enemyLayer = 1 << detectedEnemyLayer;
            }
        }

        private bool TryGetEnemyTransform(Collider candidateCollider, out Transform enemyTransform)
        {
            enemyTransform = null;
            if (candidateCollider == null) return false;

            var enemy = candidateCollider.GetComponentInParent<RogueliteGame.Enemy.Enemy>();
            if (enemy != null)
            {
                if (!enemy.IsAlive) return false;
                enemyTransform = enemy.transform;
                return true;
            }

            return false;
        }

        private void HandleCombat()
        {
            if (IsInDodgeLockState())
            {
                SetAttackFlag(false);
                return;
            }

            // Hareket her zaman saldırıdan öncelikli, hareket ederken saldırı iptal oluyor
            if (IsMoving())
            {
                SetAttackFlag(false);
                return;
            }

            if (IsInAttackAnimatorState())
            {
                if (hasEnemyInRange && nearestEnemy != null && !HasMovementIntentInput())
                {
                    FaceNearestEnemy();
                    SetAttackFlag(true);
                }
                else
                {
                    SetAttackFlag(false);
                }
                return;
            }

            bool shouldAttack = false;

            if (UnityEngine.Input.GetMouseButtonDown(attackMouseButton) && hasEnemyInRange)
            {
                FaceNearestEnemy();
                shouldAttack = true;
            }

            if (!shouldAttack && !HasMovementIntentInput() && hasEnemyInRange && IsInIdleAnimatorState())
            {
                FaceNearestEnemy();
                shouldAttack = true;
            }

            SetAttackFlag(shouldAttack);
        }

        private bool IsInIdleAnimatorState()
        {
            if (animator == null) return false;
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == WaitStateHash;
        }

        private void FaceNearestEnemy()
        {
            if (IsInDodgeLockState()) return;
            if (nearestEnemy == null) return;
            Vector3 dir = nearestEnemy.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                pendingCombatRotation = Quaternion.LookRotation(dir);
                pendingPostAttackRotation = null;
            }
        }

        private bool IsInAttackAnimatorState()
        {
            if (animator == null) return false;
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.shortNameHash == DrawArrowStateHash
                || currentState.shortNameHash == AimRecoilStateHash)
                return true;

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
                return nextState.shortNameHash == DrawArrowStateHash
                    || nextState.shortNameHash == AimRecoilStateHash;
            }

            return false;
        }

        private void ApplyAttackSpeedModifier()
        {
            if (animator == null) return;

            if (skillManager == null)
            {
                animator.speed = 1f;
                return;
            }

            animator.speed = IsInAttackAnimatorState()
                ? skillManager.GetAttackSpeedMultiplier()
                : 1f;
        }

        private void SpawnSkillArrows(Transform attackTarget)
        {
            int arrowCount = skillManager != null ? skillManager.GetArrowCount() : 1;
            float spreadOffset = skillManager != null ? skillManager.GetArrowSpreadOffset() : 0f;
            ArrowSkillData skillData = skillManager != null
                ? skillManager.BuildArrowSkillData()
                : default;

            if (arrowCount <= 1)
            {
                SpawnArrowProjectile(attackTarget, 0f, skillData);
                return;
            }

            for (int i = 0; i < arrowCount; i++)
            {
                float t = i - (arrowCount - 1) * 0.5f;
                float lateralOffset = t * spreadOffset;
                SpawnArrowProjectile(attackTarget, lateralOffset, skillData);
            }
        }

        private void SpawnArrowProjectile(Transform attackTarget, float lateralOffset = 0f, ArrowSkillData skillData = default)
        {
            Transform spawnRef = arrowSpawnPoint != null ? arrowSpawnPoint : transform;
            Vector3 spawnPos = spawnRef.TransformPoint(arrowSpawnLocalOffset);
            Vector3 targetPos = attackTarget.position + arrowTargetOffset;
            Vector3 direction = targetPos - spawnPos;

            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;

            if (Mathf.Abs(lateralOffset) > 0.001f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, direction.normalized).normalized;
                spawnPos += right * lateralOffset;
            }

            Vector3 finalDir = (targetPos - spawnPos);
            if (finalDir.sqrMagnitude < 0.0001f) finalDir = direction;

            Quaternion rotation = Quaternion.LookRotation(finalDir.normalized);
            GameObject arrowInstance = Instantiate(arrowPrefab, spawnPos, rotation);

            Rigidbody rb = arrowInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var projectile = arrowInstance.GetComponent<RogueliteGame.Combat.ArrowProjectile>();
            if (projectile == null)
                projectile = arrowInstance.AddComponent<RogueliteGame.Combat.ArrowProjectile>();

            projectile.Initialize(attackTarget, attackDamage, arrowTargetOffset, arrowSpeed, arrowLifetime, skillData);
        }

        private void ApplyDirectDamage(Transform attackTarget)
        {
            var enemy = attackTarget.GetComponentInParent<RogueliteGame.Enemy.Enemy>();
            if (enemy != null && enemy.IsAlive)
                enemy.TakeDamage(attackDamage);
        }

        public void TriggerAttack()
        {
            if (isDead || animator == null || !attackParameterExists || !hasEnemyInRange || IsMoving()) return;
            if (nearestEnemy == null) return;
            FaceNearestEnemy();
            SetAttackFlag(true);
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;
            if (invincibleDuringDodge && isDodging) return;
            currentHealth -= damage;
            RefreshPlayerHealthBar();
            if (currentHealth <= 0f) Die();
        }

        // Ölüm gerçekleştiğinde dış sistemlerin (UI vb.) tepki verebilmesi için event fırlatıyorum
        public event Action OnDied;

        public void Die()
        {
            if (isDead) return;
            isDead = true;
            currentHealth = 0f;
            RefreshPlayerHealthBar();
            SetAttackFlag(false);
            if (animator != null && isDeadParameterExists) animator.SetBool(isDeadHash, true);
            OnDied?.Invoke();
        }

        public bool IsMoving() => currentSpeed > 0.1f;
        public bool GetIsDead() => isDead;
        public bool GetIsDodging() => isDodging;
        public bool HasEnemyInRange() => hasEnemyInRange;
        public float GetHealthRatio() => currentHealth / maxHealth;
        public Vector3 GetMovementDirection() => currentMovementDirection.normalized;
        public float GetInputMagnitude() => currentSpeed;

        private void CreatePlayerHealthBar()
        {
            if (healthBarRoot != null && healthBarFill != null) return;

            GameObject canvasGO = new GameObject("PlayerHealthBar");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = new Vector3(0f, healthBarYOffset, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 2;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = healthBarSize;

            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(canvasGO.transform, false);
            healthBarFill = fillGO.AddComponent<Image>();
            healthBarFill.color = Color.green;

            RectTransform fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            healthBarRoot = canvasGO.transform;
            canvasGO.AddComponent<RogueliteGame.UI.BillboardToCamera>();

            SetupActiveSkillIconDisplay();
        }

        private void SetupActiveSkillIconDisplay()
        {
            if (healthBarRoot == null) return;

            var display = gameObject.GetComponent<ActiveSkillIconDisplay>();
            if (display == null)
                display = gameObject.AddComponent<ActiveSkillIconDisplay>();

            display.Initialize(skillManager, healthBarRoot);
        }

        private void RefreshPlayerHealthBar()
        {
            if (healthBarFill == null) return;

            float ratio = Mathf.Clamp01(currentHealth / maxHealth);
            healthBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            healthBarFill.color = Color.Lerp(Color.red, Color.green, ratio);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = (Application.isPlaying && hasEnemyInRange)
                ? new Color(1f, 0f, 0f, 0.15f)
                : new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, detectionRadius);

            Gizmos.color = (Application.isPlaying && hasEnemyInRange)
                ? new Color(1f, 0f, 0f, 0.6f)
                : new Color(0f, 1f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (!Application.isPlaying) return;

            if (currentMovementDirection.magnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                    currentMovementDirection.normalized * 2f);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * 1.5f);

            if (nearestEnemy != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    transform.position + Vector3.up * 0.5f,
                    nearestEnemy.position + Vector3.up * 0.5f);
            }
        }
#endif
    }
}
