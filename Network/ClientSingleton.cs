using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;


public class ClientSingleton : MonoBehaviour
{

    JoinAllocation allocation;

    private static ClientSingleton instance;

    public static ClientSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<ClientSingleton>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("ClientSingleton");
                    instance = obj.AddComponent<ClientSingleton>();

                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }
    private string playerID;
    private string playerNickname;
    public void SetNickname(string nickname)
    {
        this.playerNickname = nickname;
    }
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        AuthState state = await Authenticator.DoAuth();

        if (state == AuthState.Authenticated)
        {
            Debug.Log("클라이언트 인증에 성공했습니다.");
            return true;
        }

        Debug.LogError($"클라이언트 인증에 실패했습니다 {state}");
        return false;
    }

    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log("호스트에 성공적으로 들어왔습니다.");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"호스트 입장에 실패했습니다: {ex.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        // Payload에 플레이어 이름과 ID를 담아서 보내도록 설정

        string playerID = "";
        string playerNickname = "";

        LoginInfoManager.Instance.SetUserInfo(ref playerID, ref playerNickname);

        UserInfo UserInfo = new UserInfo(playerID, playerNickname);

        string payload = JsonUtility.ToJson(UserInfo);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartClient();//클라이언트 입장
    }
}
