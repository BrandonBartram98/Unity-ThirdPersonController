using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSlide : MonoBehaviour
{
    [Header("References")]
    public Animator PlayerAnimator;
    public Transform Orientation;
    public Transform PlayerObj;
    private Rigidbody _rb;
    private PlayerController _pc;
    [SerializeField] private ThirdPersonCam _tpCam;

    [SerializeField] private TrailRenderer _leftFootTrail;
    [SerializeField] private TrailRenderer _rightFootTrail;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    public float slideRotation;
    public float defaultRotation;

    [Header("Input")]
    public KeyCode SlideKey = KeyCode.LeftShift;
    private float _horizontalInput;
    private float _verticalInput;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pc = GetComponent<PlayerController>();
    }

    private void StartSlide()
    {
        _pc.sliding = true;

        _tpCam.SwitchCameraStyle(ThirdPersonCam.CameraStyle.Sliding);

        _pc.tpCamera.RotationSpeed = slideRotation;

        _pc.defaultCollider.enabled = false;
        _pc.slideCollider.enabled = true;

        _leftFootTrail.emitting = true;
        _rightFootTrail.emitting = true;

        PlayerAnimator.SetBool("Sliding", true);

        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = Orientation.forward * _verticalInput + Orientation.right * _horizontalInput;

        // Normal slide
        if (!_pc.OnSlope() || _rb.velocity.y > -0.1f)
        {
            _rb.AddForce(PlayerObj.forward * slideForce, ForceMode.Force);
        }
        // Slope slide
        else
        {
            _rb.AddForce(_pc.GetSlopeMoveDirection(PlayerObj.forward) * slideForce, ForceMode.Force);
        }

        if (_rb.velocity.magnitude < 2f)
        {
            StopSlide();
        }
    }

    private void StopSlide()
    {
        _pc.sliding = false;

        _tpCam.SwitchCameraStyle(ThirdPersonCam.CameraStyle.Default);

        _pc.tpCamera.RotationSpeed = defaultRotation;

        _pc.defaultCollider.enabled = true;
        _pc.slideCollider.enabled = false;

        _leftFootTrail.emitting = false;
        _rightFootTrail.emitting = false;

        PlayerAnimator.SetBool("Sliding", false);
    }

    private void FixedUpdate()
    {
        if (_pc.sliding)
        {
            SlidingMovement();
        }
    }

    private void Update()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(SlideKey) && _rb.velocity.magnitude > 0.1f && !_pc.wallrunning)
        {
            StartSlide();
        }

        if (Input.GetKeyUp(SlideKey) && _pc.sliding)
        {
            StopSlide();
        }
    }
}
