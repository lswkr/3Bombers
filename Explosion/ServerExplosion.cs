using UnityEngine;

public class ServerExplosion : Explosion
{
    //서버에서만 생성
    private ulong clientID;
    public ulong ClientID
    {
        get
        { return clientID; }
        set
        {
            clientID = value;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        IDeath deadable = other.GetComponent<IDeath>();
        if (deadable != null)
        {
            deadable.Death(FromPlayer, ClientID);
        }
    }
}
