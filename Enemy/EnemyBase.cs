using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static Constants;

public abstract class EnemyBase : NetworkBehaviour, IDeath
{
    private Animator anim;
    private Collider coll;
    protected Vector3 targetPosition;

    [SerializeField] protected float walkSpeed = 10f;
    protected Rigidbody rb;

    protected bool isDead = false;

    protected bool isPlayerDead = false;

    [SerializeField] protected float traceDistance;
    [SerializeField] protected float requireDistance = 5f;
    [SerializeField] protected float breakTime = 5f;
    protected bool isTracing = false;
    protected bool isBreakTime = false;

    protected bool isRunningAwayFromBomb = false;
    protected bool canIRunAway = true;
    [SerializeField] protected float runningAwayTime = 2f;
    [SerializeField] protected float runningAwayCooltime = 2f;

    protected bool isInitiated = false;
    protected IEnumerator RunningAwayCooltimer()
    {
        yield return new WaitForSeconds(runningAwayCooltime);

        canIRunAway = true;
    }

    public void OnPositionSpread(Coord coord)
    {
        //상태 변환시키기
        if (!isRunningAwayFromBomb && canIRunAway) //도망치고 있지 않고 도망칠 수 있는 상태라면
        {
            targetPosition.x = coord.C;
            targetPosition.z = coord.R;
            timer = 0;
            canIRunAway = false;
            isRunningAwayFromBomb = true;
            StartCoroutine(RunningAwayCooltimer());
        }
    }

    protected bool isSatisfied;

    private int targetsCount;
    private PlayerCharacter[] targets; //멀티플레이일 경우 플레이어 개수가 여러 개이므로 target변수를 변환시켜야한다.
    protected PlayerCharacter target;
    protected bool[] isTargetAlive;
    public bool AreTargetsAllDead { get; set; }
    int currentTargetIdx = 0;

    protected NavMeshAgent agent;

    [SerializeField] protected float initWalkSpeed;

    protected float timer = 0;
    protected float repositionTime = 2;

    EnemyCommander enemyCommander;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        agent.speed = initWalkSpeed;
        walkSpeed = initWalkSpeed;

        rb.isKinematic = true;
        rb.useGravity = true;
    }

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        coll = GetComponent<Collider>();
    }
    public void InitEnemy(EnemyCommander enemyCommander)
    {
        this.enemyCommander = enemyCommander;
        enemyCommander.onSpreadTarget += SetTarget;
        isInitiated = true;
        timer = repositionTime;

    }
    public void SetTarget(PlayerCharacter target)
    {
        this.target = target;
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
        if(IsServer)
            enemyCommander.onSpreadTarget -= SetTarget;
    }
    protected void IsSatisfied()
    {
        if (!AreTargetsAllDead)
        {
            if (Vector3.Distance(target.transform.position, transform.position) < requireDistance)
            {
                StartCoroutine(OnSatisfied());
                isSatisfied = true;
            }
        }
    }

    protected Action onSatisfied;
    public Action onDead;

    public bool IsDead { get; private set; }
    protected abstract IEnumerator OnSatisfied();

    protected virtual void Update()
    {
        if (!isInitiated || isDead) //Init안 되거나 죽었거나
        {
            return;
        }
        SetAnimation();
    }
    void SetAnimation()
    {
        if (agent.speed > 0.1f)
            anim.SetFloat("Speed", agent.speed, 0.1f, Time.deltaTime);
        else
            anim.SetFloat("Speed", 0, 0.1f, Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDead && other.CompareTag("Player"))
        {
            if (IsServer)
            {
                other.GetComponent<IDeath>().Death(false, 987654321);
            }
        }
    }


    public void Death(bool fromPlayer, ulong clientID)
    {
        if (isDead)
            return;

        agent.speed = 0;
        agent.isStopped = true;
        agent.ResetPath();
        agent.speed = 0f;

        anim.SetTrigger("Death");
        coll.enabled = false;
        isDead = true;

        if (IsServer)
        {
            enemyCommander.onSpreadTarget -= SetTarget;
            onDead?.Invoke();
            onDead = null;

            if (fromPlayer)
            {
                RpcParams rpcParams = default;
                rpcParams.Send.Target = RpcTarget.Single(clientID, RpcTargetUse.Temp);
                GetScoreRpc(rpcParams);
            }
        }
        StartCoroutine(DestroyDelay());
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void GetScoreRpc(RpcParams rpcParams = default)
    {
        DataManager.Instance.GetScore(50);
    }

    IEnumerator DestroyDelay()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
    protected float Distance(float r, float c, float rr, float cc)
    {
        float dist = Vector2.Distance(new Vector2(r, c), new Vector2(rr, cc));
        return dist;
    }

    protected float Distance(Transform a, Transform b)
    {
        float dist = Vector2.Distance(new Vector2(a.position.z, a.position.x), new Vector2(b.position.z, b.position.x));
        return dist;
    }
}
