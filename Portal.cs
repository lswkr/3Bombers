using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Portal : NetworkBehaviour
{
    bool isTriggered = false;
    public Action onComeIn;
    int playerCount = 0;

    HashSet<GameObject> passedPlayer = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!passedPlayer.Contains(other.gameObject))
            {
                if (IsServer)
                {
                    playerCount++;
                    
                    passedPlayer.Add(other.gameObject);
                    if (passedPlayer.Count == StageManager.Instance.spawnedPlayers)
                    {
                        onComeIn?.Invoke();
                    }
                }
            }
        }
    }
    private void OnDisable()
    {
        onComeIn = null;
    }
}
