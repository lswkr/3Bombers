using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static Constants;

public class Bomber : EnemyBase
{
    [SerializeField] GameObject bombPrefab;

    private Coord[] distCoord = new Coord[9] { new Coord(-1, -1), new Coord(-1, 0), new Coord(-1, 1), new Coord(0, 1), new Coord(1, 1), new Coord(1, 0), new Coord(1, -1), new Coord(0, -1), new Coord(0, 0) };

    private MapManager mapManager;
    private float mapRange;
    private int explosionRange = 2;
    private bool fromPlayer = false;

    bool repositioning = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        mapManager = FindFirstObjectByType<MapManager>();
    }

    private void Plant()
    {
        //설치 시 맵이 Empty면 설치. - MapManager에서 IsTileEmpty()
        float r = transform.position.z;
        float c = transform.position.x;
        // Debug.Log($"현재 위치: x: {c}, y: {r}");

        int plantR = (int)r;//세로
        int plantC = (int)c;//가로

        float minDistance = 1000000000;

        for (int i = 0; i < 9; i++)
        {
            int rr = (int)r + distCoord[i].R;
            int cc = (int)c + distCoord[i].C;
            if (rr < 0 || rr >= mapRange || cc < 0 || cc >= mapRange)
            {
                continue;
            }

            float dist = Distance(r, c, rr, cc);

            if (minDistance > dist)
            {
                plantR = rr;
                plantC = cc;
                minDistance = dist;
            }
        }

        //Enemy는 모두 서버에서 생성되므로 따로 처리해 줄 필요없다고 생각하나 일단 예외처리
        if(IsServer)
        {

            Bomb bomb = Instantiate(bombPrefab).GetComponent<Bomb>();

            if (!bomb)
            {
                Debug.LogError("Bomber의 BombPrefab이 설정되지 않았습니다.");
                return;
            }

            bomb.transform.position = new Vector3(plantC, 1, plantR);
            bomb.FromPlayer = false;

            bomb.gameObject.GetComponent<NetworkObject>().Spawn();

            //mapManager.CheckExplosionMapRpc(plantR, plantC, explosionRange);

            mapManager.BombPlanted(plantR, plantC);
            StartCoroutine(bomb.Explode(plantR, plantC, explosionRange));

            //폭탄 설치 후 도망
            int escapeR = 20 - (int)r;
            int escapeC = 20 - (int)c;

            walkSpeed = 2 * initWalkSpeed;
            agent.speed = 2 * initWalkSpeed;
            targetPosition = new Vector3(escapeC, 0, escapeR);
        }
    }


    protected override IEnumerator OnSatisfied()
    {
        Plant();

        yield return new WaitForSeconds(1f);
        walkSpeed = initWalkSpeed;
        agent.speed = initWalkSpeed;
        isBreakTime = true;

        yield return new WaitForSeconds(breakTime);
        isBreakTime = false;
        if (isSatisfied)
        {
            isSatisfied = false;
        }
    }



    protected override void Update()
    {
        base.Update();

        if (isDead || !isInitiated)
        {
            return;
        }


        float dist = Distance(transform, target.transform);
        timer += Time.deltaTime;

        if (isRunningAwayFromBomb) //도망가는 동안
        {
            if (timer > runningAwayTime) //도망 시간 다 끝나면
            {
                StartCoroutine(RunningAwayCooltimer());

                isRunningAwayFromBomb = false;
                timer = 0;
            }

        }

        else if (dist > traceDistance || isBreakTime)
        {
            if (timer > repositionTime)
            {
                if (!repositioning)
                {
                    repositioning = true;
                    int X = Random.Range(1, 20);
                    int Z = Random.Range(1, 20);
                    targetPosition = new Vector3(X, 0, Z);
                    timer = 0;
                }
                repositioning = false;
            }
        }
        else
        {
            if (!isSatisfied && !isDead)
            {
                targetPosition = target.transform.position;
                IsSatisfied();
            }
        }

        agent.SetDestination(targetPosition);
    }
}
