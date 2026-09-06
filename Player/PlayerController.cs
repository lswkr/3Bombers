using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    PlayerBombSetComponent playerBombsetComponent; PlayerMovement playerMovementComponent;
    public void Init(PlayerBombSetComponent pbc, PlayerMovement playerMovement)
    {
        playerInput.actions["Move"].performed += playerMovement.OnMove;
        playerInput.actions["Move"].canceled += playerMovement.OnMove;

        playerInput.actions["Bomb"].started += pbc.Plant;
    }
    public void DisableDelegate(PlayerBombSetComponent pbc, PlayerMovement playerMovement)
    {
        playerInput.actions["Move"].performed -= playerMovement.OnMove;
        playerInput.actions["Move"].canceled -= playerMovement.OnMove;

        playerInput.actions["Bomb"].started -= pbc.Plant;
    }
    
}
