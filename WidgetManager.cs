using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WidgetManager : SingletonCore<WidgetManager>
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject[] lifeimages;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;

    private Coroutine scoreCoroutine;
    private bool isRaisingScore = false;
    private int preScore;
    private int currentScore;
    public Sprite[] characterSprites;

    private void Start()
    {
        //if (IsOwner)
        //{
        //    for (int i = 0; i < lifeimages.Length; i++)
        //    {
        //        lifeimages[i].transform.GetChild(0).GetComponent<Image>().sprite = DataManager.Instance.characterSprites[/*DataManager.Instance.characterIndex*/0];
        //    }
        //}
    }

    public void InitClientWidget(int idx)
    {
        for (int i = 0; i < lifeimages.Length; i++)
        {
            lifeimages[i].transform.GetChild(0).GetComponent<Image>().sprite = characterSprites[idx];
        }
    }
    private void OnEnable()
    {
        //DataManager.Instance.onGetScore += RaiseScore;
    }
    private void OnDisable()
    {
        //DataManager.Instance.onGetScore -= RaiseScore;
    }

    public void UpdateLifeWidget(int index)
    {
        for (int i = 0; i < 3; i++)
        {
            lifeimages[i].gameObject.SetActive(false);
        }
        for (int i = 0; i <= index; i++)
        {
            lifeimages[i].gameObject.SetActive(true);
        }
    }

    public void RaiseScore(int prescore, int newscore)
    {

        if (isRaisingScore)
        {
            StopCoroutine(scoreCoroutine);
        }

        scoreCoroutine = StartCoroutine(RaiseScoreRoutine(prescore, newscore));

    }
    private IEnumerator RaiseScoreRoutine(int preScore, int newScore)
    {
        isRaisingScore = true;

        float duration = 0.5f;
        float elapsed = 0f;

        while(elapsed<duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            int curScore = (int)Mathf.Lerp(preScore, newScore, progress);
            scoreText.text = $"{curScore}";
            yield return null;
        }
        scoreText.text = $"{newScore}";
        isRaisingScore = false;
    }

    public void SpawnGameOverUI()
    {
        //TODO: GameOverUI¶ç¿ì±â
    }

    public void UpdateTimer(int timeLeft)
    {
        if (timerText == null)
            return;

        if (timeLeft <= 5)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.black;

        }

        timerText.text = timeLeft.ToString();
    }
}
