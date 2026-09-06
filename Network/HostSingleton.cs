using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[Serializable]
public class UserInfo
{
    public string playerID;
    public string playerNickname;

    public UserInfo(string playerID, string playerNickname)
    {
        this.playerID = playerID;
        this.playerNickname = playerNickname;
    }
}
public class HostSingleton : MonoBehaviour
{
    private static HostSingleton instance;
    public static HostSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<HostSingleton>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("HostSingleton");
                    instance = obj.AddComponent<HostSingleton>();

                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    const int MAXPLAYERS = 4;

    Allocation allocation;

    string joinCode;
    string lobbyID;

    public async Task StartHostAsync(string roomName)
    {
        ServerSingleton.Instance.Init(); //서버 싱글톤 가동시키기

        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MAXPLAYERS); //최대 4명 할당
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); //joinCode

            Debug.Log($"방이 생겼습니다. 조인코드:{joinCode}");
        }
        catch (RelayServiceException ex)
        {
            Debug.Log($"방 파기에 실패했습니다: {ex.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = allocation.ToRelayServerData("dtls"); //dtls방식
        transport.SetRelayServerData(relayServerData);

        //로비 정보 보내기
        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
            lobbyOptions.IsPrivate = false;
            lobbyOptions.Data = new Dictionary<string, DataObject>
            {
                {
                    "joinCode",
                    new DataObject(DataObject.VisibilityOptions.Member, joinCode)
                },
                {
                    "roomName",
                    new DataObject(DataObject.VisibilityOptions.Public, roomName)
                }

            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(joinCode, MAXPLAYERS, lobbyOptions);
            lobbyID = lobby.Id;

            StartCoroutine(LobbyHeartbeatCoroutine(15f));//15초에 호스트의 존재 알리기
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"로비 정보를 보내는 것에 실패했습니다. {ex.Message}");
        }

        //TODO: 로그인 정보 받은 후 닉네임 보내기
        //UserData userData = new UserData(
        //    AuthenticationService.Instance.PlayerName ?? "Unknown",
        //    AuthenticationService.Instance.PlayerId);
        //string payload = JsonUtility.ToJson(userData);
        //byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        //NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        string playerID = "";
        string playerNickname = "";

        LoginInfoManager.Instance.SetUserInfo(ref playerID, ref playerNickname);

        UserInfo UserInfo = new UserInfo(playerID, playerNickname);

        string payload = JsonUtility.ToJson(UserInfo);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SelectScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single); //먼저 가있기
    }

    IEnumerator LobbyHeartbeatCoroutine(float waitTimeSecond)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSecond);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyID);
            yield return delay;
        }
    }

    public async void DestroyLobby()
    {

        NetworkManager.Singleton.Shutdown();

        //string playerId = AuthenticationService.Instance.PlayerId;
        //await LobbyService.Instance.RemovePlayerAsync(lobbyID, playerId);
        await LobbyService.Instance.DeleteLobbyAsync(lobbyID);

    }
}
