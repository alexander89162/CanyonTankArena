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
    public UIVirtualJoystick movementJoystick;

    //Private variable to set current turrent
    private int turretIndex = 0;
    private Transform activeTurret;

    [SerializeField] private PlayerInput playerInput;

    private InputAction moveAction;
    private Vector2 keyboardMoveInput;
    private Vector2 touchMoveInput;

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

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput != null && playerInput.actions != null)
        {
            InputActionMap tanksMap = playerInput.actions.FindActionMap("Tanks", false);
            if (tanksMap != null)
                moveAction = tanksMap.FindAction("Move", false);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        keyboardMoveInput = context.ReadValue<Vector2>();
    }

    public void OnCycleTurret(InputAction.CallbackContext context)
    {
        if (context.performed)  // On press only
        {
            CycleTurret();
        }
    }

    public void SetMoveInput(Vector2 newMoveInput)
    {
        touchMoveInput = newMoveInput;
    }

    public void CycleTurret()
    {
        turretIndex = (turretIndex + 1) % 3;  // Cycle 0→1→2→0
        SwitchToTurret(turretIndex);
    }

    public void SetTurretCycleButtonState(bool isPressed)
    {
        if (isPressed)
            CycleTurret();
    }

    void Update()
    {
        Vector2 effectiveMoveInput = GetEffectiveMoveInput();

        if (movement != null)
        {
            movement.moveInput = effectiveMoveInput;  // Pass input to movement controller
        }

        if (movementJoystick != null)
        {
            movementJoystick.SetVisualInput(effectiveMoveInput);
        }
    }

    Vector2 GetEffectiveMoveInput()
    {
        if (touchMoveInput.sqrMagnitude > 0.0001f)
            return touchMoveInput;

        Vector2 actionMoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (actionMoveInput.sqrMagnitude > 0.0001f)
            return actionMoveInput;

        return keyboardMoveInput;
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