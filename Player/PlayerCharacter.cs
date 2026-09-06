using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCharacter : NetworkBehaviour, IDeath
{
    PlayerMovement playerMovement;
    StatusComponent statusComponent;
    PlayerController playerController;
    PlayerBombSetComponent playerBombSetComponent;

    PlayerInput playerInput;
    CharacterController cc;
    Animator anim;

    private NetworkVariable<bool> isInvincible =
        new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    [SerializeField] private CharacterSetting characterSetting;

    public NetworkVariable<bool> isDead =
    new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    //콜리전 박스 겹쳐져서 두 번 죽는 경우 방지
    private bool isGameOver = false;

    Action onDie;
    Action onRevive;

    LayerMask deadPlayerLayer;
    LayerMask playerLayer;

    CinemachineCamera cineCam;
    
    [SerializeField] private TextMeshProUGUI nicknameText;
    
    string playerNickname;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        //coll = GetComponent<Collider>();

        playerMovement = GetComponent<PlayerMovement>();
        statusComponent = GetComponent<StatusComponent>();

        playerController = GetComponent<PlayerController>();
        playerBombSetComponent = GetComponent<PlayerBombSetComponent>();

        deadPlayerLayer = LayerMask.NameToLayer("DeadPlayer");
        playerLayer = LayerMask.NameToLayer("Player");
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        BindFunctions(); //함수 바인딩을 먼저 시킨 후 수치 등 초기화 할 수 있도록 
        InitializeComponents();

        if (IsOwner)
        {
            cineCam = FindFirstObjectByType<CinemachineCamera>();

            if (cineCam)
            {
                cineCam.Follow = transform;
                cineCam.LookAt = transform;
            }
            SetNicknameText();
        }
    }

   
    private void SetNicknameText()
    {
        if (LoginInfoManager.Instance)
        {
            playerNickname = LoginInfoManager.Instance.PlayerNickname;
            SetNicknameTextRpc(LoginInfoManager.Instance.PlayerNickname);
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SetNicknameTextRpc(string nickname)
    {
        nicknameText.text = nickname;
    }
    private void InitializeComponents()
    {
        if (playerMovement != null)
        {
            playerMovement.Init(cc, anim/*, characterSetting.initSpeed*/);
        }

        if (playerBombSetComponent != null)
        {
            playerBombSetComponent.Init();
        }

        if (playerController != null)
        {
            playerController.Init(playerBombSetComponent, playerMovement);
        }

        if (statusComponent != null) //모든 컴포넌트의 수치를 변경시키는 역할을 하므로 다른 컴포넌트의 초기화가 끝난 후 초기화하도록 설정
        {
            statusComponent.Init(characterSetting);
        }
    }

    private void BindFunctions()
    {
        //if (!IsOwner) //클라 측에서도 필요한 부분 함수가 있을 수 있으므로 세부적인 함수에서 클라, 서버 처리하도록 주석처리
        //    return;
        onDie += statusComponent.OnDie;
        onDie += playerMovement.OnDie;
        onDie += playerBombSetComponent.OnDie;

        onRevive += statusComponent.OnRevive;
        onRevive += playerMovement.OnRevive;
        onRevive += playerBombSetComponent.OnRevive;

        statusComponent.onSpeedChanged += playerMovement.PlayerSpeedChanged;
        statusComponent.onRangeChanged += playerBombSetComponent.PlayerRangeChanged;
        statusComponent.onCountChanged += playerBombSetComponent.PlayerCountChanged;
    }

    private void UnbindFunctions()
    {
        //if (!IsOwner) //클라 측에서도 필요한 부분 함수가 있을 수 있으므로 세부적인 함수에서 클라, 서버 처리하도록 주석처리
        //    return;
        onDie -= statusComponent.OnDie;
        onDie -= playerMovement.OnDie;
        onDie -= playerBombSetComponent.OnDie;

        onRevive -= statusComponent.OnRevive;
        onRevive -= playerMovement.OnRevive;
        onRevive -= playerBombSetComponent.OnRevive;
                  
        statusComponent.onSpeedChanged -= playerMovement.PlayerSpeedChanged;
        statusComponent.onRangeChanged -= playerBombSetComponent.PlayerRangeChanged;
        statusComponent.onCountChanged -= playerBombSetComponent.PlayerCountChanged;

        playerController.DisableDelegate(playerBombSetComponent, playerMovement);
    }
    private IEnumerator TurnOnInvincibleMode(int invincibleTime, bool isTurnOn)
    {
        SkinnedMeshRenderer[] meshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        bool turnOn = !isTurnOn;
        int invincibleSecond = invincibleTime * 10;

        for (int i = 0; i < invincibleSecond; i++)
        {
            for (int j = 0; j < meshes.Length; j++)
            {
                meshes[j].enabled = turnOn;
            }
            turnOn = !turnOn;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator Revive()
    {
        yield return new WaitForSeconds(3f);

        if (isDead.Value && !isGameOver)
        {
            RespawnClientRpc();

            DataManager.Instance.OnRevive();
            isDead.Value = false;

            StageManager.Instance.OnPlayerRevive(OwnerClientId);

            TurnOnInvincibleModeClientRpc(2, true);
            onRevive?.Invoke();

            yield return new WaitForSeconds(2f);
            isInvincible.Value = false;
            TurnCollisionRpc(false);
        }
    }

    public void Death(bool fromPlayer, ulong clientID) //서버에서만 호출되도록 설계해놓음
    {
        if (isInvincible.Value) return;
        if (!IsServer) return;
        //if (!IsOwner) return;

        if (!isDead.Value) //죽음 중복 처리 막기 위해
        {
            isDead.Value = true;

            onDie?.Invoke(); //서버서 Invoke하므로 연결된 함수 따로 클라이언트 처리도 가능하도록 설계

            
            isInvincible.Value = true;

            DeathClientRpc();
            TurnCollisionRpc(true);

            if (!statusComponent.NoLife.Value)
            {
                StartCoroutine(Revive());
            }

            TurnOnInvincibleModeClientRpc(2, false);

            StageManager.Instance.OnPlayerDie(OwnerClientId);
        }
    }

    [Rpc(SendTo.Owner)]
    private void DeathClientRpc()
    {
        if (IsOwner) //네트워크 애니메이터 권한이 Owner에게 있다.
        {
            anim.SetTrigger("Death");
        }
    }

    [Rpc(SendTo.Owner)]
    private void RespawnClientRpc()
    {
        gameObject.transform.position = new Vector3(1, 0, 1);

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TurnCollisionRpc(bool turnOn)
    {
        gameObject.layer = turnOn ? deadPlayerLayer : playerLayer;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TurnOnInvincibleModeClientRpc(int time, bool turnOn)
    {
        StartCoroutine(TurnOnInvincibleMode(time, turnOn));
    }

    private void OnDisable()
    {
        UnbindFunctions();
    }
}
