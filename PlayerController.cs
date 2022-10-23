using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerObject;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform orientation;
    public ThirdPersonCam tpCamera;

    [Header("Colliders")]
    public CapsuleCollider defaultCollider;
    public CapsuleCollider slideCollider;

    [Header("Running")]
    [SerializeField] private float runningSpeed;
    [SerializeField] private float groundDrag;

    [Header("Sliding")]
    [Tooltip("Max obtainable speed when sliding down slope.")]
    [SerializeField] private float slideSlopeSpeed;
    [SerializeField] private float slideSpeed;
    [SerializeField] private float slopeSpeedChangeFactor;
    [SerializeField] private float slideSpeedChangeFactor;

    [Header("Wallrunning")]
    [SerializeField] float wallrunSpeed;

    [Header("Dashing")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashSpeedChangeFactor;
    [SerializeField] private float defaultSpeedChangeFactor;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    private bool _canJump = true;

    private float _moveSpeed;

    [Header("KeyBinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask whatIsGround;
    private bool _grounded;

    private float _horizontalInput;
    private float _verticalInput;

    [HideInInspector] public Vector3 moveDirection;
    Vector3 _dashDirection;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool _exitingSlope;

    [Header("Debug UI")]
    [SerializeField] private TMP_Text velocityText;
    [SerializeField] private TMP_Text groundedText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text animatorText;
    [Space]
    private Rigidbody _rb;

    public MovementState state;

    public enum MovementState
    {
        running,
        dashing,
        sliding,
        wallrunning,
        air,
    }

    [HideInInspector] public bool dashing;
    [HideInInspector] public bool sliding;
    [HideInInspector] public bool wallrunning;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
    }

    private void PlayerInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey) && _canJump && _grounded && !sliding)
        {
            _canJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    public bool OnSlope()
    {
        Debug.DrawRay(transform.position, Vector3.down, Color.green);
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 1f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void Jump()
    {
        _exitingSlope = true;

        // Reset Y velocity to keep jump consistent
        _rb.velocity = new Vector3(_rb.velocity.x, jumpForce, _rb.velocity.z);
    }
    private void ResetJump()
    {
        _canJump = true;
        _exitingSlope = false;
    }
    private void BetterJump()
    {
        if (_rb.velocity.y < 0)
        {
            _rb.velocity += (fallMultiplier - 1) * Physics.gravity.y * Time.deltaTime * Vector3.up;
        }
        else if (_rb.velocity.y > 0 && !Input.GetMouseButton(0))
        {
            _rb.velocity += (lowJumpMultiplier - 1) * Physics.gravity.y * Time.deltaTime * Vector3.up;
        }
    }

    private void MovePlayer()
    {
        if (state == MovementState.dashing) return;

        // Calculate movement direction
        moveDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

        if (OnSlope() && !_exitingSlope)
        {
            _rb.AddForce(_moveSpeed * 10f * GetSlopeMoveDirection(moveDirection), ForceMode.Force);

            // If moving up slope, stop bumping by applying force down
            if (_rb.velocity.y > 0)
            {
                _rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        else if (_grounded && (_verticalInput != 0 || _horizontalInput != 0))
        {
            _rb.AddForce(5f * _moveSpeed * moveDirection.normalized, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(5f * airMultiplier * _moveSpeed * moveDirection.normalized, ForceMode.Force);
        }

        if (!wallrunning)
        {
            _rb.useGravity = !OnSlope();
        }
    }

    private void SpeedControl()
    {
        // Limit speed on slope
        if (OnSlope() && !_exitingSlope)
        {
            if (_rb.velocity.magnitude > _moveSpeed)
            {
                _rb.velocity = _rb.velocity.normalized * _moveSpeed;
            }
        }
        else
        {
            Vector3 flatVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

            // Limit velocity
            if (flatVelocity.magnitude > _moveSpeed * 1f)
            {
                Vector3 limitedVelocity = flatVelocity.normalized * _moveSpeed;
                _rb.velocity = new Vector3(limitedVelocity.x, _rb.velocity.y, limitedVelocity.z);
            }
        }

        velocityText.text = "move speed: " + _rb.velocity.magnitude.ToString("00.00");
    }

    private float _desiredMoveSpeed;
    private float _lastDesiredMoveSpeed;
    private MovementState _lastState;
    private bool _keepMomentum;

    private void StateHandler()
    {
        // State - Wallrunning
        if (wallrunning)
        {
            state = MovementState.wallrunning;
            _desiredMoveSpeed = wallrunSpeed;
        }

        // State - Sliding
        else if (sliding)
        {
            state = MovementState.sliding;
            animator.SetBool("Falling", false);
            if (OnSlope() && _rb.velocity.y < 0.1f)
            {
                _desiredMoveSpeed = slideSlopeSpeed;
                _speedChangeFactor = slopeSpeedChangeFactor;
            }
            else
            {
                _desiredMoveSpeed = slideSpeed;
                _speedChangeFactor = slideSpeedChangeFactor;
            }
        }
        // State - Dashing
        else if (dashing)
        {
            StopAllCoroutines();
            state = MovementState.dashing;

            _desiredMoveSpeed = dashSpeed;
            _speedChangeFactor = dashSpeedChangeFactor;
        }
        // State - Running
        else if (_grounded)
        {
            animator.SetBool("Falling", false);
            state = MovementState.running;

            _desiredMoveSpeed = runningSpeed;
        }
        // State - Air
        else if (!_grounded)
        {
            animator.SetBool("Falling", true);
            state = MovementState.air;

            _desiredMoveSpeed = runningSpeed;
        }

        bool desiredMoveSpeedHasChanged = _desiredMoveSpeed != _lastDesiredMoveSpeed;

        // Keep momentum from slide
        if (_lastState == MovementState.sliding || _lastState == MovementState.dashing)
        {
            _keepMomentum = true;
        }

        // Lerp down from running speed to slide, lerp back up to running speed
        if (state == MovementState.sliding && (_lastState == MovementState.running || _lastState == MovementState.air))
        {
            _keepMomentum = true;
        }

        // Reset momentum when dashing at low speed
        if (state == MovementState.dashing && (_rb.velocity.magnitude < 10f || _lastState == MovementState.air))
        {
            _keepMomentum = false;
        }

        if (desiredMoveSpeedHasChanged)
        {
            if (_keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                _moveSpeed = _desiredMoveSpeed;
            }
        }

        stateText.text = state.ToString();

        _lastDesiredMoveSpeed = _desiredMoveSpeed;
        _lastState = state;
    }

    private float _speedChangeFactor;
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // Smoothly lerp moveSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(_desiredMoveSpeed - _moveSpeed);
        float startValue = _moveSpeed;

        float boostFactor = _speedChangeFactor;

        while (time < difference)
        {
            _moveSpeed = Mathf.Lerp(startValue, _desiredMoveSpeed, time / difference);

            time += Time.deltaTime * boostFactor;

            yield return null;
        }

        _moveSpeed = _desiredMoveSpeed;
        _speedChangeFactor = 1f;
        _keepMomentum = false;
    }

    private void Update()
    {
        // Grounded raycast check
        _grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        Debug.DrawRay(transform.position, Vector3.down, Color.green);
        groundedText.text = _grounded ?  "grounded" : "not grounded";
        animatorText.text = OnSlope() ?  "sloped": "not sloped";

        PlayerInput();
        SpeedControl();
        StateHandler();

        if (state == MovementState.running)
        {
            _rb.drag = groundDrag;
        }
        else
        {
            _rb.drag = 0;
        }
        animator.SetFloat("RunSpeed", _rb.velocity.magnitude);
    }

    private void FixedUpdate()
    {
        BetterJump();
        MovePlayer();
    }
}
