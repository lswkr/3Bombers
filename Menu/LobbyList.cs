using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyList : MonoBehaviour
{
    [SerializeField] private Transform lobbyItemParent;
    [SerializeField] private LobbyItem lobbyItemPrefab;

    [SerializeField] private Button refreshButton;
    
    bool isRefreshing = false;
    bool isJoining = false;


    private void OnEnable()
    {
        RefreshList();
    }

    private void Start()
    {
        refreshButton.onClick.AddListener(()=>RefreshList()); 
    }
    private async void RefreshList()
    {
        if (isRefreshing) return;
        isRefreshing = true;
        refreshButton.interactable = false;
        try
        {
            Debug.Log("로비 찾는 중");
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 10; //최대 10개 로비
            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    QueryFilter.FieldOptions.AvailableSlots,
                    "0",
                    QueryFilter.OpOptions.GT
                    ),

                new QueryFilter(
                    QueryFilter.FieldOptions.IsLocked,
                    "0",
                    QueryFilter.OpOptions.EQ
                    )
            };

            QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);

            foreach (Transform child in lobbyItemParent) //새로고침 시 기존에 있던 것들 파괴
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in lobbies.Results)
            {
                LobbyItem item = Instantiate(lobbyItemPrefab, lobbyItemParent);
                item.SetItem(this, lobby);
            }
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"로비 리스트를 가져오는 것에 실패했습니다. {ex.Message}");
        }

        isRefreshing = false;
        refreshButton.interactable = true;
    }

    public async void JoinAsync(Lobby lobby)
    {
        if (isJoining) return;
        isJoining = true;

        try
        {
            Lobby joiningLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["joinCode"].Value;
            await ClientSingleton.Instance.StartClientAsync(joinCode);
        }

        catch(LobbyServiceException ex)
        {
            Debug.LogError($"로비에 들어가는 것에 실패했습니다. {ex.Message}");
        }
        isJoining = false;
    }
}
