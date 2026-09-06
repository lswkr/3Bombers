using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ServerSingleton : MonoBehaviour
{
    private static ServerSingleton instance;
    public static ServerSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<ServerSingleton>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("ServerSingleton");
                    instance = obj.AddComponent<ServerSingleton>();

                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    public Dictionary<ulong, UserInfo> connectedClients =
        new Dictionary<ulong, UserInfo>();

    public void Init()
    {
        //서버 인스턴스 생성용 빈 함수
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private void ApprovalCheck
        (NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = Encoding.UTF8.GetString(request.Payload);
        UserInfo userInfo = JsonUtility.FromJson<UserInfo>(payload);
        connectedClients[request.ClientNetworkId] = userInfo;
        Debug.Log($"userInfo: {request.ClientNetworkId} {userInfo.playerNickname}");
        response.Approved = true;
        response.CreatePlayerObject = false; //대기창에 보낼 것이므로 생성 안 함
    }

    private void OnClientDisconnect(ulong clientID)
    {
        if (connectedClients.ContainsKey(clientID))
        {
            connectedClients.Remove(clientID);
        }
        else
        {
            Debug.LogWarning($"{clientID}클라이언트가 연결을 끊었지만, 유저데이터를 찾을 수 없습니다.");
        }
    }
    public UserInfo GetUserInfo(ulong clientID)
    {
        foreach (var a in connectedClients)
        {
            Debug.Log($"연결된 클라이언트: {a.Key}, {a.Value}");
        }
        return connectedClients[clientID];
    }
}
