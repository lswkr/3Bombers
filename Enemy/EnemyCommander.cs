using System;
using UnityEngine;

public class EnemyCommander : MonoBehaviour
{
    public Action<PlayerCharacter> onSpreadTarget;
    
    public void SpreadTarget(PlayerCharacter targetPlayer)
    {
        onSpreadTarget?.Invoke(targetPlayer);
    }
}
