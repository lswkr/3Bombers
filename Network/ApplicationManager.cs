using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationManager : MonoBehaviour
{
    async void Start()
    {
        DontDestroyOnLoad(gameObject);

        await LaunchInMode();
    }

    async Task LaunchInMode()
    {
        bool authenticated = await ClientSingleton.Instance.InitAsync();

        if(authenticated)
        {
            Debug.Log("클라이언트가 호스트에 연결될 준비가 되었습니다");
            GotoMenu();
        }
        else
        {
            Debug.LogError("클라이언트 인증에 실패했습니다.");
        }
    }

    public void GotoMenu()
    {
        SceneManager.LoadScene("MenuScene"); //방만들기, 대기방으로 이동
    }
}
