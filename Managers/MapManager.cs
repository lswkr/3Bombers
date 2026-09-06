using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using static Constants;


/*
 * 주의사항: 
 * 서버 폭발 생성 함수Rpc를 호출하면 그와 같은 위치의 클라이언트 폭발 함수Rpc도 호출하게 된다.
 * 원래는 서버 측 폭발이 발생할 때마다 Rpc를 통해 클라이언트 폭발을 일으키려고 했으나 
 * 많은 양과 짧은 간격 때문에 Rpc에 대한 부담이 있기에 이와 같은 방식을 택했다.
 * 
 * 따라서 tileMap의 상태는 서버와 클라이언트 모두 다 같이 변형할 수 있도록 해야한다.
 * 
 * 다만 safeMap같은 경우, 폭발 범위와는 상관이 없는 것이이게 서버에 관리 전권을 맡긴다.
 */

public class MapManager : NetworkBehaviour
{
    //도망 경로 찾기 로직
    private int[,] explosionMap = new int[MAPSIZE, MAPSIZE];

    private List<EnemyBase> spawnedEnemies = new List<EnemyBase>();
    
    private Coord[] safeMap = new Coord[400];
    Queue<Coord> savedSafePositions = new Queue<Coord>();
    float clearTime = 4; //설치+폭발 시간

    //[Rpc(SendTo.Server)]
    public void CheckExplosionMap(int r, int c, int range)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        for (int i = c - 1; i >= Mathf.Max(0, c - range); i--) //왼쪽
        {
            if (GetTileState(r, i) == TileState.Wall)
            {
                break;
            }

            if (GetTileState(r, i) == TileState.Block)
                break;
            explosionMap[r, i]++;
        }

        for (int i = c; i <= Mathf.Min(mapC - 1, c + range); i++) //오른쪽
        {
            if (GetTileState(r, i) == TileState.Wall)
            {
                break;
            }

            if (GetTileState(r, i) == TileState.Block)
                break;
            explosionMap[r, i]++;
        }

        for (int i = r + 1; i <= Mathf.Min(mapR - 1, r + range); i++) //위
        {
            if (GetTileState(i, c) == TileState.Wall)
            {
                break;
            }

            if (GetTileState(r, i) == TileState.Block)
                break;
            explosionMap[i, c]++;
        }

        for (int i = r - 1; i >= Mathf.Max(0, r - range); i--) //아래
        {
            if (GetTileState(i, c) == TileState.Wall)
            {
                break;
            }

            if (GetTileState(r, i) == TileState.Block)
                break;
            explosionMap[i, c]++;
        }

        SpreadSafePositionToEnemiesInStage();
    }

    private void SpreadSafePositionToEnemiesInStage()
    {
        int safeMapLength = GetSafePosition(safeMap);

        for (int i = safeMapLength - 1; i >= 0; i--)
        {
            int idx = UnityEngine.Random.Range(0, i + 1);

            Coord temp = safeMap[i];
            safeMap[i] = safeMap[idx];
            safeMap[idx] = temp;
        }
        int positionIdx = 0;

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
            {
                if (positionIdx >= safeMapLength)
                    break;

                spawnedEnemies[i].OnPositionSpread(safeMap[positionIdx++]); //적들에게 무작위로 위치 선정시키기
            }
        }
        StartCoroutine(UncheckExplosionMap(safeMapLength));
    }

    private int GetSafePosition(Coord[] safePositions)
    {
        int count = 0;

        for (int r = 1; r <= MAPSIZE - 2; r++)
        {
            for (int c = 1; c <= MAPSIZE - 2; c++)
            {
                if (explosionMap[r, c] == 0)
                {
                    safePositions[count++] = new Coord(r, c);
                    savedSafePositions.Enqueue(new Coord(r, c));
                }
            }
        }
        return count;
    }

    IEnumerator UncheckExplosionMap(int positionCount)
    {
        yield return new WaitForSeconds(clearTime);

        for (int i = 0; i < positionCount; i++)
        {
            if (savedSafePositions.Count == 0)
            {
                break;
            }

            Coord curPos = savedSafePositions.Dequeue();

            if (explosionMap[curPos.R, curPos.C] > 0)
            {
                explosionMap[curPos.R, curPos.C]--;
            }
        }
    }

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject blockPrefab;

    [SerializeField] private GameObject runnerPrefab;
    [SerializeField] private GameObject bomberPrefab;

    [SerializeField] private GameObject portalPrefab;
    private GameObject portal = null;

    [SerializeField] public int mapR = MAPSIZE;
    [SerializeField] public int mapC = MAPSIZE;

    public static int mapRange = MAPSIZE;

    private int stageNum = 0;

    [SerializeField] private TextMeshProUGUI timeText;

    private float tileSize = 1f;
    private GameObject[,] tileArray;

    //실험용 오브젝트
    [SerializeField] GameObject emptyfab;

    int totalEnemyCount;
    private int currentDeadEnemy = 0;

    private void CountDeadEnemy()
    {
        
    }

    public void OnAllEnemiesEliminated()
    {
        if (!IsServer)
            return;

        portal = Instantiate(portalPrefab, new Vector3(MAPSIZE - 2, 1, MAPSIZE - 2), Quaternion.identity);
        portal.GetComponent<Portal>().onComeIn += OnStageCompleted;
        portal.GetComponent<NetworkObject>().Spawn();
    }
    private void Update()
    {

    }

    private void OnStageCompleted()
    {
        if(IsServer)
            portal.GetComponent<NetworkObject>().Despawn();
        StageManager.Instance.OnStageComplete();

        //StageManager.Instance.onPlayerDie -= OnPlayerDie;
        //StageManager.Instance.onPlayerRevive -= OnPlayerRevive;

    }

    /// </summary>
    #region 폭탄
    //[SerializeField] private GameObject bombPrefab;

    //private GameObject CreateBomb()
    //{
    //    GameObject bomb = Instantiate(bombPrefab);
    //    bomb.name = "Bomb";
    //    bomb.GetComponent<NetworkObject>().Spawn();
    //    return bomb;
    //}

    //private void OnBombGet(GameObject bomb)
    //{
    //    bomb.SetActive(true);
    //}

    //private void OnBombRelease(GameObject bomb)
    //{
    //    bomb.SetActive(false);
    //}
    #endregion

    #region 폭발
    public ObjectPool<ServerExplosion> serverPlayerExplosionPool;
    public ObjectPool<ServerExplosion> serverEnemyExplosionPool;
    public ObjectPool<GameObject> clientExplosionPool;

    [SerializeField] private GameObject serverPlayerExplosionPrefab;
    [SerializeField] private GameObject serverEnemyExplosionPrefab;
    [SerializeField] private GameObject clientExplosionPrefab;

    private ServerExplosion CreateServerPlayerExplosion()
    {
        ServerExplosion explosion = Instantiate(serverPlayerExplosionPrefab).GetComponent<ServerExplosion>();
        explosion.name = "ServerPlayerExplosion";
        return explosion;
    }

    private ServerExplosion CreateServerEnemyExplosion()
    {
        ServerExplosion explosion = Instantiate(serverEnemyExplosionPrefab).GetComponent<ServerExplosion>();
        explosion.name = "ServerEnemyExplosion";
        return explosion;
    }

    private GameObject CreateClientExplosion()
    {
        GameObject explosion = Instantiate(clientExplosionPrefab);
        explosion.name = "ClientExplosion";
        return explosion;
    }

    private void OnServerExplosionGet(ServerExplosion explosion)
    {
        explosion.gameObject.SetActive(true);
    }

    private void OnServerExplosionRelease(ServerExplosion explosion)
    {
        explosion.gameObject.SetActive(false);
    }

    private void OnClientExplosionGet(GameObject explosion)
    {
        explosion.SetActive(true);
    }

    private void OnClientExplosionRelease(GameObject explosion)
    {
        explosion.SetActive(false);
    }
    #endregion

    [SerializeField] private TileState[,] tileState = new TileState[20, 20];

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //StageManager.Instance.onPlayerDie += OnPlayerDie;
        //StageManager.Instance.onPlayerRevive += OnPlayerRevive;

        Init();
        CreateField();

        //player = Instantiate(DataManager.Instance.characters[DataManager.Instance.characterIndex], new Vector3(2, 0, 2), Quaternion.identity);
    }



    private void Init()
    {
        tileArray = new GameObject[mapR, mapC];

        serverPlayerExplosionPool = new ObjectPool<ServerExplosion>
        (
            CreateServerPlayerExplosion,
            OnServerExplosionGet,
            OnServerExplosionRelease
        );

        serverEnemyExplosionPool = new ObjectPool<ServerExplosion>
        (
            CreateServerEnemyExplosion,
            OnServerExplosionGet,
            OnServerExplosionRelease
        );

        clientExplosionPool = new ObjectPool<GameObject>
        (
            CreateClientExplosion,
            OnClientExplosionGet,
            OnClientExplosionRelease
        );
        stageNum = StageManager.Instance.stageNum;
    }

    private void CreateField() //서버 클라 모두 맵 생성
    {
        float offsetX = tileSize / 2f;
        float offsetY = tileSize / 2f;

        for (int r = 0; r < mapR; r++) // R
        {
            for (int c = 0; c < mapC; c++) //C
            {

                float posZ = transform.position.z + r * tileSize;
                float posX = transform.position.x + c * tileSize;

                if (Stage.totalMap[stageNum, r, c] == 0)
                {
                    CreateTile(r, c);
                }
                else if (Stage.totalMap[stageNum, r, c] == 1)
                {
                    tileState[r, c] = TileState.Wall;
                    GameObject wallObj = Instantiate(wallPrefab, transform);
                    wallObj.name = $"Wall{posX}_{posZ}";
                    wallObj.transform.position = new Vector3(posX, 0, posZ);
                    tileArray[r, c] = wallObj;
                }
                else if (Stage.totalMap[stageNum, r, c] == 2)
                {
                    if (IsServer)
                    {
                        GameObject blockObj = Instantiate(blockPrefab, transform);

                        blockObj.name = $"block{posX}_{posZ}";
                        blockObj.transform.position = new Vector3(posX, 0, posZ);
                        tileArray[r, c] = blockObj;
                        blockObj.GetComponent<Block>().SetCoordinate(r, c);
                        blockObj.GetComponent<NetworkObject>().Spawn();
                    }
                    CreateTile(r, c);
                    tileState[r, c] = TileState.Block;
                }

                else if (Stage.totalMap[stageNum, r, c] == 3 || Stage.totalMap[stageNum, r, c] == 7) //Runner생성
                {
                    //타일생성
                    CreateTile(r, c);

                    if (IsServer)
                    {
                        enemySpawnInfoQueue.Enqueue(new EnemySpawnInfo((EnemyType)(Stage.totalMap[stageNum, r, c]), new Coord(r, c)));
                        //StartCoroutine(SpawnEnemy(bomberPrefab, r, c));
                    }
                }
            }
        }
    }

    //서버 폭탄이 호출
    public void ExplodeServer(int r, int c, int range, bool fromPlayer, ulong clientID)
    {
        StartCoroutine(ExplodeServerRight(r, c, range, fromPlayer, clientID));
        StartCoroutine(ExplodeServerLeft(r, c, range, fromPlayer, clientID));
        StartCoroutine(ExplodeServerUp(r, c, range, fromPlayer, clientID));
        StartCoroutine(ExplodeServerDown(r, c, range, fromPlayer, clientID));

        ExplodeClientRpc(r, c, range, fromPlayer);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ExplodeClientRpc(int r, int c, int range, bool fromPlayer)
    {
        StartCoroutine(ExplodeClientRight(r, c, range, fromPlayer));
        StartCoroutine(ExplodeClientLeft(r, c, range, fromPlayer));
        StartCoroutine(ExplodeClientUp(r, c, range, fromPlayer));
        StartCoroutine(ExplodeClientDown(r, c, range, fromPlayer));
    }

    public IEnumerator ServerExploding(int r, int c, ServerExplosion explosion, bool fromPlayer, ulong clientID)
    {
        tileState[r, c] = TileState.Exploding;

        yield return new WaitForSeconds(1f);

        tileState[r, c] = TileState.Empty;

        if (fromPlayer)
            serverPlayerExplosionPool.Release(explosion);
        else
            serverEnemyExplosionPool.Release(explosion);
    }

    public IEnumerator ClientExploding(int r, int c, GameObject explosion)
    {
        //서버 폭발과 같이 움직이기 위해 클라이언트 측에서도 타일의 상태를 같이 해줘야 한다.
        tileState[r, c] = TileState.Exploding;

        yield return new WaitForSeconds(1f);

        tileState[r, c] = TileState.Empty;

        clientExplosionPool.Release(explosion);
    }

    public IEnumerator ExplodeServerRight(int r, int c, int range, bool fromPlayer, ulong clientID)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);

        for (int i = c; i <= Mathf.Min(mapC - 1, c + range); i++)
        {
            if (GetTileState(r, i) == TileState.Wall)
            {
                break;
            }

            bool isBlock = GetTileState(r, i) == TileState.Block;

            yield return explosionSpreadSec;
            ServerExplosion explosion;
            if (fromPlayer)
            {
                explosion = serverPlayerExplosionPool.Get();
                explosion.ClientID = clientID;
                explosion.FromPlayer = true;
            }
            else
            {
                explosion = serverEnemyExplosionPool.Get();
                explosion.FromPlayer = false;
            }

            explosion.transform.position = new Vector3(i, 0, r);

            StartCoroutine(ServerExploding(r, i, explosion, fromPlayer, clientID));

            if (isBlock)
                break;
        }
        yield break;
    }

    public IEnumerator ExplodeServerLeft(int r, int c, int range, bool fromPlayer, ulong clientID)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);
        for (int i = c - 1; i >= Mathf.Max(0, c - range); i--)
        {
            if (GetTileState(r, i) == TileState.Wall)
            {
                break;
            }

            bool isBlock = GetTileState(r, i) == TileState.Block;

            yield return explosionSpreadSec;

            
            ServerExplosion explosion;
            if (fromPlayer)
            {
                explosion = serverPlayerExplosionPool.Get();
                explosion.ClientID = clientID;
                explosion.FromPlayer = true;
            }
            else
            {
                explosion = serverEnemyExplosionPool.Get();
                explosion.FromPlayer = false;
            }

            explosion.transform.position = new Vector3(i, 0, r);

            StartCoroutine(ServerExploding(r, i, explosion, fromPlayer, clientID));
 
            if (isBlock)
            {
                break;
            }
        }
    }

    public IEnumerator ExplodeServerUp(int r, int c, int range, bool fromPlayer, ulong clientID)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);
        for (int i = r + 1; i <= Mathf.Min(mapR - 1, r + range); i++)
        {
            if (GetTileState(i, c) == TileState.Wall)
            {
                break;
            }

            bool isBlock = GetTileState(i, c) == TileState.Block;

            yield return explosionSpreadSec;

            ServerExplosion explosion;
            if (fromPlayer)
            {
                explosion = serverPlayerExplosionPool.Get();
                explosion.ClientID = clientID;
                explosion.FromPlayer = true;
            }
            else
            {
                explosion = serverEnemyExplosionPool.Get();
                explosion.FromPlayer = false;
            }

            explosion.transform.position = new Vector3(c, 0, i);

            StartCoroutine(ServerExploding(i, c, explosion, fromPlayer, clientID));
            
            if (isBlock)
                break;
        }
    }

    public IEnumerator ExplodeServerDown(int r, int c, int range, bool fromPlayer, ulong clientID)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);
        for (int i = r - 1; i >= Mathf.Max(0, r - range); i--)
        {
            if (GetTileState(i, c) == TileState.Wall)
            {
                break;
            }

            bool isBlock = GetTileState(i, c) == TileState.Block;

            yield return explosionSpreadSec;


            ServerExplosion explosion;
            
            if (fromPlayer)
            {
                explosion = serverPlayerExplosionPool.Get();
                explosion.ClientID = clientID;
                explosion.FromPlayer = true;
            }
            else
            {
                explosion = serverEnemyExplosionPool.Get();
                explosion.FromPlayer = false;
            }

            explosion.transform.position = new Vector3(c, 0, i);

            StartCoroutine(ServerExploding(i, c, explosion, fromPlayer, clientID));

            if (isBlock)
                break;
        }
    }

    public IEnumerator ExplodeClientRight(int r, int c, int range, bool fromPlayer)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);
        for (int i = c; i <= Mathf.Min(mapC - 1, c + range); i++)
        {
            if (GetTileState(r, i) == TileState.Wall)
            {
                break;
            }
            bool isBlock = GetTileState(r, i) == TileState.Block;
            yield return explosionSpreadSec;

            GameObject explosion;
            explosion = clientExplosionPool.Get();
            explosion.transform.position = new Vector3(i, 0, r);

            StartCoroutine(ClientExploding(r, i, explosion));

            
            if (isBlock)
                break;
        }
        yield break;
    }

    public IEnumerator ExplodeClientLeft(int r, int c, int range, bool fromPlayer)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);
        for (int i = c - 1; i >= Mathf.Max(0, c - range); i--)
        {
            if (GetTileState(r, i) == TileState.Wall)
            {
                break;
            }
            bool isBlock = GetTileState(r, i) == TileState.Block;
            yield return explosionSpreadSec;


            GameObject explosion;
            explosion = clientExplosionPool.Get();
            explosion.transform.position = new Vector3(i, 0, r);

            StartCoroutine(ClientExploding(r, i, explosion));

            if (isBlock)
                break;

        }
    }

    public IEnumerator ExplodeClientUp(int r, int c, int range, bool fromPlayer)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);
        for (int i = r + 1; i <= Mathf.Min(mapR - 1, r + range); i++)
        {
            if (GetTileState(i, c) == TileState.Wall)
            {
                break;
            }

            bool isBlock = GetTileState(i, c) == TileState.Block;
            yield return explosionSpreadSec;


            GameObject explosion;
            explosion = clientExplosionPool.Get();
            explosion.transform.position = new Vector3(c, 0, i);

            StartCoroutine(ClientExploding(i, c, explosion));

            if (isBlock)
                break;
        }
    }

    public IEnumerator ExplodeClientDown(int r, int c, int range, bool fromPlayer)
    {
        WaitForSeconds explosionSpreadSec = new WaitForSeconds(0.05f);
        for (int i = r - 1; i >= Mathf.Max(0, r - range); i--)
        {
            if (GetTileState(i, c) == TileState.Wall)
            {
                break;
            }

            bool isBlock = GetTileState(i, c) == TileState.Block;
            yield return explosionSpreadSec;

            GameObject explosion;
            explosion = clientExplosionPool.Get();
            explosion.transform.position = new Vector3(c, 0, i);


            StartCoroutine(ClientExploding(i, c, explosion));


            if (isBlock)
                break;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BombPlantedClientRpc(int r, int c)
    {
        tileState[r, c] = TileState.Bomb;
    }
    public void BombPlanted(int r, int c)
    {
        //서버와 클라이언트의 tileState 통일성을 위해 Enemy가 서버에서 호출할 경우 클라이언트 측에 Rpc를 호출해준다.
        //다행히 Server, Client Exploding에서 폭발 이후 둘 다 State처리를 해줘 여전히 따로 Empty해줄 필요는 없다.
        if (IsServer)
        {
            tileState[r, c] = TileState.Bomb;
            BombPlantedClientRpc(r, c);
        }
    }

    public TileState GetTileState(int r, int c)
    {
        return tileState[r, c];
    }

    private void CreateTile(int r, int c)
    {
        tileState[r, c] = TileState.Empty;
        GameObject tileObj = Instantiate(tilePrefab, transform);
        if ((r + c) % 2 == 0)
            tileObj.GetComponent<Renderer>().material.color = Color.black;
        else
            tileObj.GetComponent<Renderer>().material.color = Color.white;

        tileObj.name = $"tile{c}_{r}";
        tileObj.transform.position = new Vector3(c, 0, r);
        tileArray[r, c] = tileObj;
        tileObj.GetComponent<Tile>().SetTilePosition((int)r, (int)c);
    }
    //private IEnumerator SpawnEnemy(GameObject enemyPrefab, int r, int c)
    //{
    //    //시작할 때 뜸 들이기
    //    totalEnemyCount++;
    //    yield return new WaitForSeconds(5f);

    //    tileState[r, c] = TileState.Empty;

    //    if (IsServer)
    //    {
    //        GameObject enemyObj = Instantiate(enemyPrefab, new Vector3(c, 0, r), Quaternion.identity);
    //        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
    //        spawnedEnemies.Add(enemy);
    //        enemy.onDead += CountDeadEnemy;

    //        enemyObj.GetComponent<NetworkObject>().Spawn();
    //    }

    //}

    Queue<EnemySpawnInfo> enemySpawnInfoQueue = new Queue<EnemySpawnInfo>();
    public Queue<EnemySpawnInfo> GetScheduledEnemies()
    {
        Queue<EnemySpawnInfo> returnQueue = new Queue<EnemySpawnInfo>(enemySpawnInfoQueue);
        return returnQueue;
    }

    private void OnDrawGizmos()
    {
        for(int i=0;i<20;i++)
        {
            for(int j=0;j<20;j++)
            {
                if (tileState[i,j]== TileState.Block)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(new Vector3(j, .5f, i), new Vector3(1, 1, 1));
                }
                else if (tileState[i,j]==TileState.Bomb)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(new Vector3(j, .5f, i), new Vector3(1, 1, 1));
                }
                else if (tileState[i, j] == TileState.Empty)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(new Vector3(j, .5f, i), new Vector3(1, 1, 1));
                }
                else if (tileState[i, j] == TileState.Exploding)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(new Vector3(j, .5f, i), new Vector3(1, 1, 1));
                }
            }
        }
    }
}

