using UnityEngine;

public interface IDeath
{
    public void Death(bool fromPlayer, ulong clientID); //서버에서만 호출되도록 설계하기

}
