using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class RankUIItem : MonoBehaviour
{
    [SerializeField] TMP_Text rankText;
    [SerializeField] TMP_Text playerNicknameText;
    [SerializeField] TMP_Text scoreText;


    public void SetItem(int rankInt, string nickname, int score)
    {
        rankText.text = rankInt.ToString();
        playerNicknameText.text = nickname;
        scoreText.text = score.ToString();
    }

}
