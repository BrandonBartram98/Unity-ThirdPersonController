using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerObj;
    [SerializeField] private Transform playerCam;
    [SerializeField] private Animator animator;
    private Rigidbody _rb;
    private PlayerController _pc;

    [Header("Dashing")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDuration;
    [SerializeField] private TrailRenderer dashTrail;

    [Header("Cooldown")]
    [SerializeField] private float dashCooldown;
    private float _dashCDTimer;

    [Header("KeyBinds")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pc = GetComponent<PlayerController>();
    }

    private void Dash()
    {
        if (_dashCDTimer > 0 || _pc.state == PlayerController.MovementState.sliding) return;
        else _dashCDTimer = dashCooldown;

        animator.SetBool("Dashing", true);

        _rb.useGravity = false;
        _pc.dashing = true;
        dashTrail.emitting = true;

        Transform forwardT;

        forwardT = orientation;
        Vector3 direction = GetDirection(forwardT);

        Vector3 forceToApply = direction * dashForce;

        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);

        Invoke(nameof(ResetDash), dashDuration);
    }
    private Vector3 delayedForceToApply;
    private void DelayedDashForce()
    {
        _rb.velocity = Vector3.zero;
        _rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        animator.SetBool("Dashing", false);
        _rb.useGravity = true;
        dashTrail.emitting = false;
        _pc.dashing = false;
    }

    private Vector3 GetDirection(Transform forwardT)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3();

        direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;

        if (verticalInput == 0 && horizontalInput == 0)
        {
            direction = playerObj.forward;
        }

        return direction;
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey))
        {
            Dash();
        }

        if (_dashCDTimer > 0)
        {
            _dashCDTimer -= Time.deltaTime;
        }
    }
}
