using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField]
    private float mouseXSensitivity;

    [SerializeField]
    private float mouseYSensitivity;

    [Header("References")]
    [SerializeField] private Transform playerBody;

    private float _xRotation;
    private Vector2 _rawMouseDelta = Vector2.zero;

    // Code has been inspired and modified a bit based on these tutorials
    // https://www.youtube.com/watch?v=f473C43s8nE&t=505s
    // https://www.youtube.com/watch?v=_QajrabyTJc
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void Update()
    {
        GetRawMouseInput();
        float mouseX = _rawMouseDelta.x * mouseXSensitivity;
        float mouseY = _rawMouseDelta.y * mouseYSensitivity;

        // Looking up and down
        _xRotation -= mouseY;
        _xRotation = Math.Clamp(_xRotation, -90f, 90f); // Makes it so we can only look right above us and not flip our entire camera

        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f); // Its rotating about the x axis aka up and down
        
        // Looking right and left
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void GetRawMouseInput()
    {
        if (Mouse.current != null)
        {
            _rawMouseDelta = Mouse.current.delta.ReadValue();
        }
    }
}
