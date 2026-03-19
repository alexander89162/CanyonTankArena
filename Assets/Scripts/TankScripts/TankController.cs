using UnityEngine;
using UnityEngine.InputSystem;

public class TankController : MonoBehaviour
{
    //Movement tank Func
    public MovementController movement;
    //Slope Align Tank Func
    public TankSlope tankSlope;

    //To place tank turrents to array for cycling
    public Transform[] turrets = new Transform[3];

    //Private variable to set current turrent
    private int turretIndex = 0;
    private Transform activeTurret;

    private Vector2 moveInput;                     

    void Awake()
    {
        SwitchToTurret(turretIndex);

        if(movement == null)
        {
            movement = GetComponent<MovementController>();
        }

        if (tankSlope != null)
        {
            tankSlope.tankRoot = transform;  // Pass root reference
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnCycleTurret(InputAction.CallbackContext context)
    {
        if (context.performed)  // On press only
        {
            turretIndex = (turretIndex + 1) % 3;  // Cycle 0→1→2→0
            SwitchToTurret(turretIndex);
        }
    }

    void Update()
    {
        if (movement != null)
        {
            movement.moveInput = moveInput;  // Pass input to movement controller
        }
    }



    void SwitchToTurret(int index)
    {
        for (int i = 0; i < turrets.Length; i++)
            if (turrets[i] != null) turrets[i].gameObject.SetActive(i == index);

        if (turrets[index] != null)
        {
            activeTurret = turrets[index];
        }
    }
}