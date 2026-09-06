using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour, IDeath
{
    protected bool isApplied = false;
    

    float jumpDelta = 0;

    [SerializeField] private float rotateSpeed = 30f;
    [SerializeField] private float jumpDist = 3f;
    [SerializeField] private float jumpPower = 5f;
    
    protected void Start()
    {
        
    }

    private void Update()
    {
        if (!isApplied)
        {
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        }
        else
        {
            jumpDelta += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, transform.position + jumpDist * Vector3.up, jumpDelta/jumpDist);
        }
    }
    protected IEnumerator DisappearDelay()
    {
        yield return new WaitForSeconds(2f);
        
        if(IsServer)
            gameObject.GetComponent<NetworkObject>().Despawn();
    }

    public void Death(bool fromPlayer, ulong clientID)
    {
        if(IsServer)
            gameObject.GetComponent<NetworkObject>().Despawn();
    }
}
