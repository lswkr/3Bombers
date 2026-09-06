using System;
using UnityEngine.UI;

[Serializable]
public class UserData
{
    public string playerID;
    public string playerPassword;
    public string playerNickname;
    public UserData(string playerID, string playerPassword, string playerNickname)
    {
        this.playerID = playerID;
        this.playerPassword = playerPassword;
        this.playerNickname = playerNickname;
    }
}
