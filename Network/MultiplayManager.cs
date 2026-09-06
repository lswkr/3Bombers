using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static Constants;

public class MultiplayManager : NetworkBehaviour
{
    //순서
    //TODO 1: 다 모일 때까지 대기
    //TODO 2: 선택된 것들을 가져와서 Player로 Network소환
    //TODO 3: 캐릭터 죽음/게임오버 같은 거 델리게이트 붙이기
    //TODO 4: 캐릭터 생성 후 꺼놓고 게임 카운트 다운
    //TOOD 5: 캐릭터들 켜기

    private bool isFirstGame = true;
    private bool isFirstSpawned = true;
    private List<StatusComponent> spawnedCharacterStatusComponents = new List<StatusComponent>();
    [SerializeField] GameObject[] Characters = new GameObject[3];

    public struct PlayerSelection : INetworkSerializable
    {
        public ulong clientID;
        public int selectIdx;

        public PlayerSelection(ulong clientID, int selectIdx)
        {
            this.clientID = clientID;
            this.selectIdx = selectIdx;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientID);
            serializer.SerializeValue(ref selectIdx);
        }
    }

    List<PlayerSelection> playerCharactersIndices = new List<PlayerSelection>();

    Coord[] spawnPositions = new Coord[4] { new Coord(18, 1), new Coord(18, 18), new Coord(1, 18), new Coord(1, 1) };

    private static MultiplayManager instance;

    public static MultiplayManager Instance { get; private set; }
    //{
    //    get
    //    {
    //        if (instance == null)
    //        {
    //            instance = FindFirstObjectByType<MultiplayManager>();

    //            if (instance == null)
    //            {
    //                GameObject obj = new GameObject("MultiplayManager");
    //                instance = obj.AddComponent<MultiplayManager>();

    //                DontDestroyOnLoad(obj);
    //            }
    //        }
    //        return instance;
    //    }
    //}


    //private void Awake()
    //{
    //    if (instance == null)
    //    {
    //        instance = this;
    //        DontDestroyOnLoad(gameObject);
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //    }
    //}
    //private void Start()
    //{
    //    if(instance!=null)
    //    {
    //        NetworkObject netObj= instance.GetComponent<NetworkObject>();
    //        if(NetworkManager.Singleton.IsServer && !netObj.IsSpawned)
    //        {
    //            netObj.Spawn();
    //        }
    //    }

    //}
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;

            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != null && Instance != this)
        {
            NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
            if (IsServer && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
    }

    private void PrepareForGamePlay()
    {
        if (isFirstSpawned)
        {
            //NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            isFirstSpawned = false;
        }
    }

    void OnSceneEvent(SceneEvent sceneEvent)
    {

    }

    [Rpc(SendTo.Server)]
    public void OnSelectCharacterRpc(ulong clientId, int selectIdx)
    {
        playerCharactersIndices.Add(new PlayerSelection(clientId, selectIdx));
    }

    //public IEnumerator MoveToStage()
    //{
    //    if (!IsServer)
    //        yield break;

    //    yield return new WaitForSeconds(2f);

    //    //NetworkManager.Singleton.SceneManager.LoadScene("Stage1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    //}

    public void SpawnPlayerCharacter(ref PlayerCharacter[] playerCharacters, ref Dictionary<ulong, int> playerIDDictionary, ref int playerCount)
    {
        if (!IsServer)
            return;

        int spawnIdx = 0;
        foreach (PlayerSelection playerSelection in playerCharactersIndices)
        {
            if (spawnIdx >= 4)
                break;
            Debug.Log($"spawnIDX: {spawnIdx}");

            Vector3 spawnPosition = new Vector3(spawnPositions[spawnIdx].C, 0, spawnPositions[spawnIdx].R);

            GameObject playerCharacter = Instantiate(Characters[playerSelection.selectIdx], spawnPosition, Quaternion.identity);

            if (playerCharacter)
            {
                StatusComponent playerStatusComponent = playerCharacter.GetComponent<StatusComponent>();

                if (playerStatusComponent)
                {
                    playerStatusComponent.SetCharacterType((CharacterType)playerSelection.selectIdx);

                    if (!isFirstGame)
                    {
                        playerStatusComponent.LoadData(DataManager.Instance.LoadPlayerCharacterData(playerSelection.clientID));
                    }
                    spawnedCharacterStatusComponents.Add(playerStatusComponent);

                    //네트워크 Spawn전 값 설정
                    playerCharacters[spawnIdx] = playerCharacter.GetComponent<PlayerCharacter>();
                    playerIDDictionary[playerSelection.clientID] = spawnIdx;

                    //네트워크 Spawn후 값 설정
                    playerCharacter.GetComponent<NetworkObject>().SpawnAsPlayerObject(playerSelection.clientID);
                    playerStatusComponent.spawnPosition.Value = spawnPosition;
                    Debug.Log($"{playerSelection.clientID} spawnPosition: {playerStatusComponent.spawnPosition.Value}");

                    spawnIdx++;
                }


            }

        }
        playerCount = spawnIdx;

        if (isFirstGame)
        {
            isFirstGame = !isFirstGame;
        }
    }

    public void SaveCharactersData()
    {
        foreach (var statusComponent in spawnedCharacterStatusComponents)
        {
            statusComponent.SaveData();

            if (IsServer)
                statusComponent.gameObject.GetComponent<NetworkObject>().Despawn();
        }
        if (IsServer)
        {
            spawnedCharacterStatusComponents.Clear();
        }
    }

    public void OnGameComplete()
    {
        isFirstGame = true;
        spawnedCharacterStatusComponents.Clear();
        playerCharactersIndices.Clear();
    }
}
