using Unity.Netcode;
using UnityEngine;

public class Block : NetworkBehaviour, IDeath
{
    int R, C;

    public void SetCoordinate(int r, int c)
    {
        R = r;
        C = c;
    }
    public void Death(bool fromPlayer, ulong clientID)
    {
        if (IsServer)
        {
            if (fromPlayer)
            {
                RpcParams rpcParams = default;
                rpcParams.Send.Target = RpcTarget.Single(clientID, RpcTargetUse.Temp);
                GetScoreRpc(rpcParams);
            }
            ItemManager.Instance.SpawnItems(R, C);
            gameObject.GetComponent<NetworkObject>().Despawn();
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void GetScoreRpc(RpcParams rpcParams)
    {
        DataManager.Instance.GetScore(50);
    }
}
