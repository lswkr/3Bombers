using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
    public struct UserCharacterSelection
    {
        ulong clientId;
        int characterIndex;
    }

    List<UserCharacterSelection> selectList = new List<UserCharacterSelection>();

    private static PlayerSpawner instance;
    public static PlayerSpawner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlayerSpawner>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("PlayerSpawner");
                    instance = obj.AddComponent<PlayerSpawner>();

                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        if(instance==null)
        {
            instance = this as PlayerSpawner;
            DontDestroyOnLoad(gameObject);
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if(sceneName.Contains("Stage"))
        {
            Debug.Log("게임 시작");
        }
    }
}
