using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    CharacterController cc;
    Animator anim;
    Collider coll;
    //AttackBox attackBox;

    //이동
    private Vector3 moveInput;
    private Vector3 moveVector;
    private Vector3 finalMovement;

    private float curSpeed;
    private float initWalkSpeed;
    private float walkSpeed;
    private Vector3 verticalVelocity;

    private bool isRunning = false;

    [SerializeField] float rotSpeed;

    //공격
    DataManager dataManager;

    private NetworkVariable<bool> isDead =
      new NetworkVariable<bool>(false,
      NetworkVariableReadPermission.Everyone,
      NetworkVariableWritePermission.Server);


    bool isInvincible = false;

    Vector2 tilePosition;

    public void Init(CharacterController cc, Animator anim/*, float walkSpeed*/)
    {
        this.cc = cc;
        this.anim = anim;
        //this.initWalkSpeed = walkSpeed;
    }

    private void Start()
    {
        DataManager.Instance.OnStart();
    }

    private void OnEnable()
    {
        DataManager.Instance.onGameOver += IsGameOver;
    }
    private void OnDisable()
    {
        DataManager.Instance.onGameOver -= IsGameOver;
    }
    private void Update()
    {
        Move();
        Turn();
        SetAnimation();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;
        if (isDead.Value || !StageManager.IsStageStart.Value)
            return;

        Vector2 inputDir = context.ReadValue<Vector2>();
        moveInput = new Vector3(inputDir.x, 0f, inputDir.y).normalized;

        if (context.performed)
        {
            curSpeed = walkSpeed;
        }
        else
        {
            curSpeed = 0;
        }
        moveVector = moveInput * curSpeed * Time.deltaTime;
    }
    public void Move()
    {
        if (isDead.Value)
            return;
        if (!cc.isGrounded)
        {
            verticalVelocity += new Vector3(0, Constants.Gravity, 0) * Time.deltaTime;
        }
        else
        {
            verticalVelocity = Vector3.zero;
        }
        cc.Move(moveVector + verticalVelocity);

    }

    private void Turn()
    {
        if (isDead.Value) return;
        if (moveInput.sqrMagnitude >= 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        }
    }


    private void SetAnimation()
    {
        anim.SetFloat("Speed", curSpeed, 0.1f, Time.deltaTime);
    }


    void IsGameOver()
    {
        //isGameOver = true;
    }


    public void PlayerSpeedChanged(float inSpeed)
    {
        walkSpeed = inSpeed;
    }

    public void OnDie()
    {
        isDead.Value = true;
        moveInput = Vector3.zero;
        moveVector = Vector3.zero;
    }
    public void OnRevive()
    {
        isDead.Value = false;
    }
}
