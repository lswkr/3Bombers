using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class LobbyItem : MonoBehaviour
{
    [SerializeField] TMP_Text lobbyTitleText;
    [SerializeField] TMP_Text lobbyPlayerCountText;
    [SerializeField] Button joinButton;

    LobbyList lobbyList;
    Lobby lobby;

    public void SetItem(LobbyList lobbyList, Lobby lobby)
    {
        lobbyTitleText.text = lobby.Data["roomName"].Value.IsNullOrEmpty()? "재밌는 게임 한 판 ^^": lobby.Data["roomName"].Value;
        lobbyPlayerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        this.lobbyList = lobbyList;
        this.lobby = lobby;

        joinButton.onClick.AddListener(()=>OnClickJoinButton());
    }

    public void OnClickJoinButton()
    {
        lobbyList.JoinAsync(lobby);
    }
}
