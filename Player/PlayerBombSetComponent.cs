using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using static Constants;

public class PlayerBombSetComponent : NetworkBehaviour
{
    //MapManager
    MapManager mapManager;
    public void Init()
    {
        mapManager = FindFirstObjectByType<MapManager>();
    }

    //맵 범위
    private NetworkVariable<bool> isDead =
        new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Coord[] distCoord = new Coord[9] { new Coord(-1, -1), new Coord(-1, 0), new Coord(-1, 1), new Coord(0, 1), new Coord(1, 1), new Coord(1, 0), new Coord(1, -1), new Coord(0, -1), new Coord(0, 0) };


    //폭탄 설치 위치
    private Coord[] positions = new Coord[4];
    [SerializeField] GameObject bombPrefab;
    [SerializeField] int explosionRange = 1;

    //폭탄 풀
    private ObjectPool<GameObject> bombPool;

    //폭탄설치개수 리밋
    private int bombLimit = 1;//실험용
    private int currentBombCount = 0;

    [Rpc(SendTo.Owner)]
    void BombRetrieveRpc()
    {
        currentBombCount = Mathf.Max(0, currentBombCount - 1);
    }

    public void Plant(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (isDead.Value || !StageManager.IsStageStart.Value) return;

        float r = transform.position.z;
        float c = transform.position.x;

        int plantR = (int)r;//세로
        int plantC = (int)c;//가로

        float minDistance = 1000000000;

        for (int i = 0; i < 9; i++)
        {
            int rr = (int)r + distCoord[i].R;
            int cc = (int)c + distCoord[i].C;
            if (rr < 0 || rr >= MAPSIZE || cc < 0 || cc >= MAPSIZE)
            {
                continue;
            }

            float dist = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(rr - r), 2) + Mathf.Pow(Mathf.Abs(cc - c), 2));

            if (minDistance > dist)
            {
                plantR = rr;
                plantC = cc;
                minDistance = dist;
            }
        }
        if (mapManager == null)
        {
            Debug.LogError("MapManager doesn't exist");
        }
        if (mapManager.GetTileState(plantR, plantC) == TileState.Bomb)
        {
            return;
        }
        if (currentBombCount >= bombLimit)
        {
            return;
        }
        currentBombCount++;

        PlantRpc(plantR, plantC); //Owner에서 하므로 서버에 RPC보내기
        
    }

    [Rpc(SendTo.Server)]
    void PlantRpc(int r, int c)
    {
        // 주의: 폭탄은 주인공 Range 때문에 현 위치에서 건드려야 함
        Bomb bomb = Instantiate(bombPrefab).GetComponent<Bomb>();
        bomb.onExplode += BombRetrieveRpc;
        bomb.transform.position = new Vector3(c, 1f, r); //y값은 폭탄의 반지름에 맞춰서
        bomb.FromPlayer = true;

        bomb.gameObject.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        
        mapManager.BombPlanted(r, c);//서버
        
        StartCoroutine(bomb.Explode(r, c, explosionRange));
    }


    public void PlayerCountChanged(int inBombCount)
    {
        bombLimit = inBombCount;
    }
    public void PlayerRangeChanged(int inRange)
    {
        explosionRange = inRange;
    }

    public void OnDie()
    {
        isDead.Value = true;
    }
    public void OnRevive()
    {
        isDead.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
}
