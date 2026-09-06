using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Constants;

public class StageManager : NetworkBehaviour
{
    public int stageNum = 1;
    public bool isGameOver = false;
    public Action<int> onUpdateLife;

    public Action<ulong> onPlayerDie;
    public Action<ulong> onPlayerRevive;
    public Action onGameOver;
    public Action onGameComplete;

    private MapManager mapManager;
    private int mapNumber = 1;//맵 개수

    public int spawnedPlayers = 0;//일단 2로

    private EnemyCommander enemyCommander;
    private GameObject enemyCommanderObj;

    bool isFirstSpawnOnNetwork = true;

    public static NetworkVariable<bool> IsStageStart = new NetworkVariable<bool>(false);

    bool isStageComplete = false;
    //public void GameStartInit()
    //{
    //    stageNum = 1;
    //    isGameOver = false;
    //}

    public static StageManager Instance { get; private set; }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;

            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            isFirstSpawnOnNetwork = false;
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
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        //if (Instance == this)
        //{
        //    NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        //    Instance = null;
        //}
    }

    private void OnEnable()
    {
        //if (isFirstSpawnOnNetwork)
        //{
        //    NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        //    isFirstSpawnOnNetwork = false;
        //}
    }


    void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                if (sceneEvent.SceneName.Contains("Stage"))
                {
                    IsStageStart.Value = false;
                    isStageClear.Value = false;
                    if (enemyCommanderObj == null)
                    {
                        enemyCommanderObj = new GameObject("EnemyCommander");
                        enemyCommander = enemyCommanderObj.AddComponent<EnemyCommander>();

                        enemyCommanderObj.gameObject.transform.parent = transform;
                    }
                    
                    mapManager = FindFirstObjectByType<MapManager>(); //씬 바꿀 때마다 그 맵의 매니저 찾기

                    if (mapManager == null)
                    {
                        Debug.LogError("MapManager가 설정되지 않았습니다");
                    }

                    MultiplayManager.Instance.SpawnPlayerCharacter(ref playerCharacters, ref playerIDDictionary, ref spawnedPlayers);


                    isTargetAlive = new bool[spawnedPlayers];

                    for (int i = 0; i < spawnedPlayers; i++)
                    {
                        isTargetAlive[i] = true;
                    }

                    StartCoroutine(SpawnEnemies());
                }

            }
        }
    }
    NetworkVariable<bool> isStageClear = new NetworkVariable<bool>(false);

    public void OnStageComplete()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        stageNum++;
        isStageClear.Value = true;
        MultiplayManager.Instance.SaveCharactersData();


        if (stageTimerCoroutine!= null)
            StopCoroutine(stageTimerCoroutine);

        if (DoesGameComplete())
        {
            onGameComplete?.Invoke();
            NetworkManager.Singleton.SceneManager.LoadScene("GameClearScene", LoadSceneMode.Single);

            foreach(var playerID in playerIDDictionary)
            {
                RpcParams rpcParams = default;
                rpcParams.Send.Target = RpcTarget.Single(playerID.Key, RpcTargetUse.Temp);
                

                if (isTargetAlive[playerID.Key]) //살아있는 캐릭터만 점수 저장
                {
                    Debug.Log($"{playerID.Key} 점수 저장 시도");
                    SaveScore(playerID.Key/*,rpcParams*/);
                }
            }
            DataManager.Instance.OnGameComplete();
            stageNum = 1;
            MultiplayManager.Instance.OnGameComplete();
            return;
        }

        string sceneName = "Stage" + stageNum;
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public bool DoesGameComplete() //게임 완료 시
    {
        if (mapNumber + 1 == stageNum) //인덱스 처리
        {
            onGameComplete?.Invoke();
            return true;
        }
        return false;
    }
    public void OnPlayerDie(ulong clientID)
    {

        onPlayerDie?.Invoke(clientID);
        isTargetAlive[playerIDDictionary[clientID]] = false;

        if (changeTheTargetCoroutine != null)
            StopCoroutine(changeTheTargetCoroutine);
        
        SpreadTarget();
    }

    public void OnPlayerRevive(ulong clientID)
    {
        onPlayerRevive?.Invoke(clientID);
        isTargetAlive[playerIDDictionary[clientID]] = true;

        if (NetworkManager.Singleton.IsServer)
            changeTheTargetCoroutine = StartCoroutine(ChangeTheTarget());
    }



    public void OnStart()
    {
        //onUpdateLife?.Invoke(PlayerLife);
        //onGetScore?.Invoke(score, score);
    }

    public void OnGameOver()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            spawnedPlayers--;

            if (spawnedPlayers <= 0)
            {
                isGameOver = true;
                NetworkManager.Singleton.SceneManager.LoadScene("GameOverScene", LoadSceneMode.Single);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void GameOverUIClientRpc()
    {
        WidgetManager.Instance.SpawnGameOverUI();
    }
    //[Rpc(SendTo.SpecifiedInParams)]
    private void SaveScore(ulong clientID/*, RpcParams rpcParams*/)
    {
        DataManager.Instance.SaveScore(clientID);
    }
    //씬 바뀔 때 spawendPlayers 개수 알 수 있도록 처리해주기

    bool isGameStopped = true;
    int timeLimit = 100;
    

    //서버에서만 호출하기
    IEnumerator StageTimer()
    {
        if (!NetworkManager.Singleton.IsServer)
            yield break;
        int timeCount = 0;
        WaitForSeconds perSecond = new WaitForSeconds(1f);

        while (isGameStopped)
        {
            timeCount++;
            CounterUIClientRpc(timeLimit - timeCount);

            yield return perSecond;

            if (isStageClear.Value)
            {
                yield break;
            }

            if (timeCount == timeLimit)
            {
                //플레이어 다 죽이고
                KillAllThePlayers();
                if (isGameOver)
                {
                    //페이드 아웃하고
                    //페이드 아웃 시간에 맞춰서
                    //for(int i=0;i<fadeOutTime;i++)
                    //{
                    //     yield return perSecond;
                    //}

                    yield break;
                }

                else
                {
                    for (int i = 0; i < 2; i++) //timeCount=0으로 만들고 2초 뒤(캐릭터가 2초 뒤에 생성되므로) 진행
                    {
                        yield return perSecond;
                    }
                    timeCount = 0;
                }

            }
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void CounterUIClientRpc(int timeLeft)
    {
        WidgetManager.Instance.UpdateTimer(timeLeft);
    }

    //적 관련 로직

    int totalEnemyCount;
    int currentDeadEnemy = 0;
    private List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
    Coroutine stageTimerCoroutine;

    //배열 실수를 방지하기 위해 따로 분리
    [SerializeField] private GameObject runnerPrefab;
    [SerializeField] private GameObject bomberPrefab;

    private IEnumerator SpawnEnemies()
    {
        //시작할 때 뜸 들이기
        if (NetworkManager.Singleton.IsServer)
        {
            int coolTime = 5;

            for (int i = coolTime; i > 0; i--)
            {
                CounterUIClientRpc(i);
                yield return new WaitForSecondsRealtime(1);
            }

            Queue<EnemySpawnInfo> enemySpawnQueue = mapManager.GetScheduledEnemies(); //서버여야 결과 값이 나온다.

            while (enemySpawnQueue.Count > 0)
            {
                EnemySpawnInfo curEnemyInfo = enemySpawnQueue.Dequeue();

                GameObject curEnemyPrefab = runnerPrefab;
                if (curEnemyInfo.enemyType == EnemyType.bomber)
                {
                    curEnemyPrefab = bomberPrefab;
                }
                else
                {
                    curEnemyPrefab = runnerPrefab;
                }

                GameObject enemyObj = Instantiate(curEnemyPrefab, new Vector3(curEnemyInfo.coord.C, 0, curEnemyInfo.coord.R), Quaternion.identity);

                if (enemyObj)
                {
                    EnemyBase enemyComponent = enemyObj.GetComponent<EnemyBase>();
                    if (enemyComponent)
                    {

                        spawnedEnemies.Add(enemyComponent);
                        enemyComponent.onDead += CountDeadEnemy;

                        enemyComponent.InitEnemy(enemyCommander);
                        enemyObj.GetComponent<NetworkObject>().Spawn();

                        totalEnemyCount++;
                    }
                }
            }

            //생성 끝낸 뒤, SpreadTarget
            currentTargetIdx = UnityEngine.Random.Range(0, spawnedPlayers);
            enemyCommander.SpreadTarget(playerCharacters[currentTargetIdx]);
            changeTheTargetCoroutine = StartCoroutine(ChangeTheTarget());
            stageTimerCoroutine = StartCoroutine(StageTimer());

            IsStageStart.Value = true;
        }

    }
    private void CountDeadEnemy()
    {
        currentDeadEnemy++;
        if (totalEnemyCount == currentDeadEnemy)
        {
            mapManager.OnAllEnemiesEliminated();

            if (changeTheTargetCoroutine != null)
                StopCoroutine(changeTheTargetCoroutine);
        }
    }

    private bool[] isTargetAlive = new bool[4];
    PlayerCharacter[] playerCharacters = new PlayerCharacter[4];
    private Dictionary<ulong, int> playerIDDictionary = new Dictionary<ulong, int>();

    int currentTargetIdx = 0;

    Coroutine changeTheTargetCoroutine;
    float targetChangeInterval = 5f;
    bool areTargetsAllDead = false;


    void SpreadTarget()
    {
        if (!isStageClear.Value && TryToGetNextTargetIndex())
        {
            enemyCommander.SpreadTarget(playerCharacters[currentTargetIdx]);
        }
    }

    IEnumerator ChangeTheTarget()
    {
        if (spawnedPlayers == 1 || isStageClear.Value)
            yield break;


        WaitForSeconds changeTime = new WaitForSeconds(targetChangeInterval);

        while (true)
        {
            SpreadTarget();

            yield return changeTime;
        }
    }

    private bool TryToGetNextTargetIndex()
    {
        if (spawnedPlayers == 1) //플레이어가 한 명일 경우
            return true;

        if (spawnedPlayers <= 0)
            return false;
        currentTargetIdx = (currentTargetIdx + 1) % spawnedPlayers;

        int findCount = 0;

        if (!isTargetAlive[currentTargetIdx]) //다음 캐릭터가 죽었을 경우
        {
            for(findCount=1;findCount<=spawnedPlayers;findCount++) //살아있는 플레이어의 수 만큼 반복 찾기
            {
                if (isTargetAlive[(currentTargetIdx + findCount) % spawnedPlayers])//살아있는 다음 타겟 바로 설정
                {
                    currentTargetIdx += findCount;
                    currentTargetIdx %= spawnedPlayers;
                    return true;
                }
            }
            

            if (findCount == spawnedPlayers + 1) //플레이어가 다 죽으면
            {
                areTargetsAllDead = true;

                foreach (var enemy in spawnedEnemies) //적들에게 전달
                {
                    enemy.AreTargetsAllDead = true;
                }
                return false;
            }
        }
        return true;//살아있으면 바로
    }
    public void KillAllThePlayers()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var player in playerCharacters)
            {
                if (player != null && !player.isDead.Value) //서버에서 나간 캐릭터와 죽은 캐릭터는 제외
                {
                    //TODO: 나중에 혹시 이어하기 기능을 추가하게 된다면 각 플레이어에서 GameOver코루틴으로 대기시간 준 뒤 OnGameOver부르기(현재는 목숨 세 개로 켠왕식)
                    player.GetComponent<IDeath>().Death(false, 987654321);
                }
            }
        }
    }

    public void OnStageClear()
    {

    }
}
