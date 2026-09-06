using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bomb : NetworkBehaviour, IDeath
{
    MapManager mapManager;

    private ParticleSystem ps;
    private bool fromPlayer = true;

    //public ulong clientID;
    public bool FromPlayer
    {
        get
        {
            return fromPlayer;
        }
        set
        {
            fromPlayer = value;
        }
    }

    private MeshRenderer mesh;
    [SerializeField] private Collider triggerBox;
    public Action onExplode;
    private int R, C;

    private int explosionRange = 0;
    public void SetRange(int range)
    {
        explosionRange = range;
    }
    private bool isExploded = false; //폭발이 폭탄에 닿았을 경우 터져야 하는데 코루틴이 먼저 돌아가고 있으므로 이미 터진 경우 코루틴에서 돌아가지 않도록
    private void Awake()
    {
        mapManager = FindFirstObjectByType<MapManager>();

        mesh = GetComponent<MeshRenderer>();

        ps = GetComponent<ParticleSystem>();

        nonTriggerBox.enabled = false;
    }

    private void OnEnable()
    {
        triggerBox.enabled = true;
        mesh.enabled = true;
        isExploded = false;
    }

    //폭탄은 PlayerBombSetComponent에서 서버Rpc를 사용해 생성한 뒤, 이 코루틴을 호출하므로 Server에서 작동
    public IEnumerator Explode(int r, int c, int range)
    {
        R = r;
        C = c;
        explosionRange = range;
        
        mapManager.CheckExplosionMap(r, c, range);

        yield return new WaitForSeconds(2f);

        if (!isExploded)
        {
            Death(fromPlayer, OwnerClientId);
        }
    }

    public void Death(bool fromPlayer, ulong clientID) //폭발에 닿았을 때에도 호출
    {
        if (isExploded)
            return;
        ps.Play();

        onExplode?.Invoke();
        onExplode = null;
        //coll.enabled = false;
        //mesh.enabled = false;
        isExploded = true;

        if (IsServer)
        {
            mapManager.ExplodeServer(R, C, explosionRange, fromPlayer, clientID);
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [SerializeField] Collider nonTriggerBox;
    [SerializeField] LayerMask characterLayer;

    //생성 후 위에 있는 캐릭터들 물러나기 전까지 trigger==false인 박스를 켜지않기위해
    private Collider[] overlappedCharacters = new Collider[10];
    private HashSet<Collider> overlappedCharatersHash = new HashSet<Collider>();
    private Vector3 halfExtent = new Vector3(.5f, .5f, .5f);
    bool isCaptured = false;

    private void Update()
    {
        if (isCaptured)
            return;

        int overlappedCount = 0;
        overlappedCount = Physics.OverlapBoxNonAlloc(transform.position, halfExtent, overlappedCharacters, Quaternion.identity, characterLayer);

        if (overlappedCount > 0)
        {
            for(int i=0;i<overlappedCount; i++)
            {
                overlappedCharatersHash.Add(overlappedCharacters[i]);
                Physics.IgnoreCollision(overlappedCharacters[i], nonTriggerBox, true);
            }
        }
        isCaptured = true;
        nonTriggerBox.enabled = true;

    }

    //서버, 클라 모두 필요
    private void OnTriggerExit(Collider other)
    {
        if(overlappedCharatersHash.Contains(other))
        {
            Physics.IgnoreCollision(other, nonTriggerBox, false);
        }
    }
    
    void OnDisable() //오브젝트 풀로 바꿀 상황대비
    {
        foreach(var overlappedCharacter in overlappedCharacters)
        {
            if(overlappedCharacter!=null)
            {
                Physics.IgnoreCollision(overlappedCharacter, nonTriggerBox, false);
            }
        }
        overlappedCharatersHash.Clear();
        nonTriggerBox.enabled = false;
    }
}



