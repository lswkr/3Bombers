using Unity.Netcode;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    private bool fromPlayer = true;
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
}
