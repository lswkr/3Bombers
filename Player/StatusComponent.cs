using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static Constants;
using static DataManager;

public class StatusComponent : NetworkBehaviour
{

    private NetworkVariable<CharacterType> selectCharacterType = new NetworkVariable<CharacterType>(CharacterType.Count);
    public NetworkVariable<Vector3> spawnPosition = new NetworkVariable<Vector3>(new Vector3(-1, 0, -1));
    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);

    public void SetCharacterType(CharacterType characterType)
    {
        selectCharacterType.Value = characterType;
    }

    int initRange;
    int initCount;
    float initSpeed;

    #region UI

    #endregion

    private int curRange;
    private int curCount;
    private float curSpeed;
    private int curLife;

    public Action<int> onRangeChanged;
    public Action<int> onCountChanged;
    public Action<float> onSpeedChanged;

    public Action onGameOver;

    PlayerCharacterData savedData;
    bool isFirstInitializing = true;

    public NetworkVariable<bool> NoLife = new NetworkVariable<bool>(false);
    public int CurRange
    {
        get { return curRange; }
        private set
        {

            if (MAXRANGE < value)
                return;

            curRange = value;
            onRangeChanged?.Invoke(curRange);
        }
    }
    public int CurCount
    {
        get { return curCount; }
        private set
        {
            if (MAXBOMBCOUNT < value)
                return;

            curCount = value;
            onCountChanged?.Invoke(curCount);
        }
    }
    public float CurSpeed
    {
        get { return curSpeed; }
        private set
        {

            curSpeed = Mathf.Min(value, MAXSPEED);
            onSpeedChanged?.Invoke(curSpeed);
        }
    }
    public int CurLife
    {
        get { return curLife; }

        private set
        {
            if (value > MAXLIFE)
                return;
            curLife = value;

            if (IsServer)
                OnLifeChangeClientRpc(curLife);
        }
    }

    [Rpc(SendTo.Owner)]
    private void OnLifeChangeClientRpc(int value)
    {
        WidgetManager.Instance.UpdateLifeWidget(value);
        if (value == -1)
        {
            DataManager.Instance.SaveScore(OwnerClientId);
        }
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (spawnPosition.Value != new Vector3(-1, 0, -1))//값이 Owner에 먼저 도착 안 했을 경우
        {
            if (IsOwner)
            {
                Debug.Log("값 변경");

                StartCoroutine(TeleportAfterFrame());
                //gameObject.transform.position = spawnPosition.Value;
            }
        }

        else //값이 Owner에 먼저 도착했을 경우를 대비
        {
            Debug.Log("도착 X");
            spawnPosition.OnValueChanged += SpawnPositionClientFirst;
        }


    }
    IEnumerator TeleportAfterFrame()
    {
        yield return null;
        //GetComponent<NetworkTransform>().Teleport(spawnPosition.Value, Quaternion.identity, transform.localScale);

        CharacterController cc = GetComponent<CharacterController>();
        if (cc)
        {
            cc.enabled = false;
            gameObject.transform.position = spawnPosition.Value;
            cc.enabled = true;
        }

        Debug.Log($"{OwnerClientId} Spawn Position: {spawnPosition.Value}");
        Debug.Log($"{OwnerClientId} Real Position: {transform.position}");
    }
    private void SaveScore()
    {
        DataManager.Instance.SaveScore(OwnerClientId);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        spawnPosition.OnValueChanged -= SpawnPositionClientFirst;
        StageManager.Instance.onGameComplete -= SaveScore;
    }

    private void SpawnPositionClientFirst(Vector3 previousValue, Vector3 newValue)
    {
        if (IsOwner)
        {
            Debug.Log($"콜백: previousValue: {previousValue}, newValue: {newValue}");


            CharacterController cc = GetComponent<CharacterController>();
            if (cc)
            {
                cc.enabled = false;
                gameObject.transform.position = spawnPosition.Value;
                cc.enabled = true;
            }
            Debug.Log($"{OwnerClientId} Spawn Position: {spawnPosition.Value}");
            Debug.Log($"{OwnerClientId} Real Position: {transform.position}");
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnStatusUpdateRpc(int newRange, int newCount, float newSpeed)
    {
        CurRange = newRange;
        CurCount = newCount;
        CurSpeed = newSpeed;
    }
    [Rpc(SendTo.Owner)]
    private void OnInitClientRpc()
    {
        WidgetManager.Instance.InitClientWidget((int)selectCharacterType.Value);
    }

    public void Init(CharacterSetting characterSetting)
    {
        OnInitClientRpc();

        if (!isFirstInitializing)
        {
            /*initRange =*/
            CurRange = savedData.range;
            /*initCount =*/
            CurCount = savedData.count;
            /*initSpeed =*/
            CurSpeed = savedData.speed;
            /*initLife = */
            CurLife = savedData.life;
        }
        else
        {
            initRange = CurRange = characterSetting.initRange;
            initCount = CurCount = characterSetting.initCount;
            initSpeed = CurSpeed = characterSetting.initSpeed;
            CurLife = MAXLIFE;
        }

    }

    Vector3 reposition = new Vector3(1, 0, 1);
    //[Rpc(SendTo.Owner)]
    //public void AdjustSpawnPositionRpc(Vector3 reposition)
    //{
    //    this.reposition = reposition;
    //}
    public void OnRangeUp()
    {
        if (CurRange >= MAXRANGE)
        {
            if (IsOwner)
                DataManager.Instance.GetScore(50);
            return;
        }
        CurRange++;
    }

    public void OnSpeedUp()
    {
        if (CurSpeed >= MAXSPEED)
        {
            if (IsOwner)
                DataManager.Instance.GetScore(50);
            return;
        }
        CurSpeed *= 1.2f;
    }

    public void OnCountUp()
    {
        if (CurCount >= MAXBOMBCOUNT)
        {
            if (IsOwner)
                DataManager.Instance.GetScore(50);
            return;
        }
        CurCount++;
    }

    public void OnDie()
    {
        if (!IsServer)
            return;

        if (CurLife > 0)
        {
            CurLife = CurLife - 1;
            CurRange = initRange;
            CurCount = initCount;
            CurSpeed = 0;

            OnStatusUpdateRpc(CurRange, CurCount, CurSpeed);
        }
        else
        {
            onGameOver?.Invoke(); //ps: 서버에서만 일으키는게 문제 생길 수 있을 듯(이후 클라에게도 뭔가 해주기도 해야할 것 같음)
            onGameOver = null;

            StageManager.Instance.OnGameOver();
            NoLife.Value = true;
            OnLifeChangeClientRpc(-1); //Life 다 사라졌을 때 Widget의 초상화 수를 다 없애기 위해
        }
    }

    public void OnRevive()
    {
        CurSpeed = initSpeed;
        OnStatusUpdateRpc(CurRange, CurCount, CurSpeed);
    }


    public void SaveData()
    {
        if (IsServer)
        {
            PlayerCharacterData playerCharacterData = new PlayerCharacterData(CurRange, CurCount, CurSpeed, CurLife);
            DataManager.Instance.SavePlayerCharacterData(OwnerClientId, playerCharacterData);
        }
    }

    public void LoadData(PlayerCharacterData playerCharacterData)
    {
        isFirstInitializing = false; //Init과 순서를 맞추기 위한 변수
        savedData = playerCharacterData;
    }
}


