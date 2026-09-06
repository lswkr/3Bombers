using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    FadeEvent fadeEvent;
    private void Start()
    {
        fadeEvent = FindFirstObjectByType<FadeEvent>();

    }
    private void OnEnable()
    {
        DataManager.Instance.onGameOver += OnGameOver;
        DataManager.Instance.onGameComplete += OnGameComplete;

    }

    private void OnDisable()
    {
        DataManager.Instance.onGameOver -= OnGameOver;
        DataManager.Instance.onGameComplete -= OnGameComplete;

    }
    void OnGameOver()
    {
        StartCoroutine(GameOverRoutine());
    }
    void OnGameComplete()
    {
        StartCoroutine(GameCompleteRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        fadeEvent.Fade(2, Color.black, true);
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(5);
    }

    IEnumerator GameCompleteRoutine()
    {
        fadeEvent.Fade(2, Color.white, true);
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(4);
    }
}
