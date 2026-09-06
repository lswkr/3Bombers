using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    [SerializeField] private GameObject instructor;

    [SerializeField] private Button startButton;
    [SerializeField] private Button instructorButton;
    [SerializeField] private Button endButton;
    [SerializeField] private Button rankButton;

    [SerializeField] private GameObject ranking;
    [SerializeField] private TextMeshProUGUI rankingText;

    [SerializeField] private TMP_InputField userName;

    bool isInstructorActive = false;
    private void Start()
    {
        if(startButton!=null)
        {
            startButton.onClick.AddListener(OnStartPressed);
        }
        if (instructorButton != null)
        {
            instructorButton.onClick.AddListener(OnInstructorPressed);
        }
        if (endButton != null)
        {
            endButton.onClick.AddListener(OnEndPressed);
        }
        if(rankButton != null)
        {
            rankButton.onClick.AddListener(OnRankPressed);
        }
    }

    void OnStartPressed()
    {
        DataManager.Instance.userName = userName.text;
        //DataManager.Instance.GameStartInit();
        SceneManager.LoadScene(1);
    }

    void OnInstructorPressed()
    {
        if(instructor.activeSelf)
        {
            instructor.SetActive(false);
        }
        else
        {
            instructor.SetActive(true);
        }
    }

    void OnEndPressed()
    {
#if UNITY_EDITOR
        // 유니티 에디터에서 실행 중일 때
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 앱이나 PC 게임으로 빌드되었을 때
        Application.Quit();
#endif
    }
    void OnRankPressed()
    {
        if (ranking.activeSelf)
        {
            ranking.SetActive(false);
        }
        else
        {
            rankingText.text = DataManager.Instance.OnRankingOn();
            ranking.SetActive(true);
        }
        

    }

}
