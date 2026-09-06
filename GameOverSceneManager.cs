using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverSceneManager : MonoBehaviour
{
    [SerializeField] Button gotoIntroSceneButton;
    [SerializeField] Button quitButton;
    void Start()
    {
        gotoIntroSceneButton.onClick.AddListener(GoToIntroScene);
        quitButton.onClick.AddListener(Quit);
    }

    private void GoToIntroScene()
    {
        if(NetworkManager.Singleton)
        {
           
            if (NetworkManager.Singleton.IsServer)
            {
                ////NetworkManager.Singleton.
                HostSingleton.Instance.DestroyLobby();
                NetworkManager.Singleton.SceneManager.LoadScene("IntroScene", LoadSceneMode.Single);
            }
            else
            {
                //NetworkManager.Singleton.Shutdown();
                NetworkManager.Singleton.SceneManager.LoadScene("IntroScene", LoadSceneMode.Single);
            }
        }
    }

    private void Quit()
    {
#if UNITY_EDITOR
        // 유니티 에디터에서 실행 중일 때
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 앱이나 PC 게임으로 빌드되었을 때
        Application.Quit();
#endif
    }

}
