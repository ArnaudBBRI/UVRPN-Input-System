using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PrintMouseCoordinates : MonoBehaviour
{
    Vector2 mousePosition;
    NewControls inputActions;
    [SerializeField] private TextMeshProUGUI text1;
    [SerializeField] private TextMeshProUGUI text2;
    void Start()
    {
        inputActions = new NewControls();
        inputActions.Newactionmap.Enable();
        inputActions.Newactionmap.interact.performed += PrintClick;
        inputActions.Newactionmap.point.performed += PrintMousePosition;
    }

    private void PrintMousePosition(InputAction.CallbackContext ctx)
    {
        mousePosition = ctx.ReadValue<Vector2>();
        text2.text = mousePosition.ToString();
    }

    private void PrintClick(InputAction.CallbackContext ctx)
    {
        text1.text = $"Button clicked at time {Time.realtimeSinceStartup}";
    }
}