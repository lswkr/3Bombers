using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameClearSceneManager : MonoBehaviour
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
        SceneManager.LoadScene(0);
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
