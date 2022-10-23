using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCam : MonoBehaviour
{
    [SerializeField] private Transform _orientation;
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _playerObj;

    public float RotationSpeed;

    public GameObject defaultCamera;
    public GameObject leftWallCam;
    public GameObject rightWallCam;
    public GameObject slideCam;

    public CameraStyle CurrentStyle;

    public enum CameraStyle
    {
        Default,
        WallrunningLeft,
        WallrunningRight,
        Sliding,
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SwitchCameraStyle(CameraStyle newStyle)
    {
        defaultCamera.SetActive(false);
        leftWallCam.SetActive(false);
        rightWallCam.SetActive(false);
        slideCam.SetActive(false);

        if (newStyle == CameraStyle.Default) defaultCamera.SetActive(true);
        if (newStyle == CameraStyle.WallrunningLeft) leftWallCam.SetActive(true);
        if (newStyle == CameraStyle.WallrunningRight) rightWallCam.SetActive(true);
        if (newStyle == CameraStyle.Sliding) slideCam.SetActive(true);

        CurrentStyle = newStyle;
    }

    private void Update()
    {
        // Rotate orientation
        Vector3 viewDir = _playerObj.position - new Vector3(transform.position.x, _player.position.y, transform.position.z);
        _orientation.forward = viewDir.normalized;

        // Rotate player object
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 inputDir = _orientation.forward * verticalInput + _orientation.right * horizontalInput;

        if (inputDir != Vector3.zero)
        {
            _playerObj.forward = Vector3.Slerp(_playerObj.forward, inputDir.normalized, Time.deltaTime * RotationSpeed);
        }
    }
}
