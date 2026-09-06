using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeEvent : MonoBehaviour
{
    [SerializeField] private Image fadeImage;

    public void Fade(float fadeTime, Color fadeColor, bool isFade)
    {
        fadeImage.gameObject.SetActive(true);

        StartCoroutine(FadeRoutine(fadeTime, fadeColor, isFade));
    }

    public IEnumerator FadeRoutine(float fadeTime, Color fadeColor, bool isFade)
    {
        float timer = 0f;
        float percent = 0f;

        while(percent<1f)
        {
            timer += Time.deltaTime;
            percent = timer / fadeTime;

            float alphaValue = isFade ? percent : 1-percent;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alphaValue);
            yield return null;
        }
        if (!isFade)
            fadeImage.gameObject.SetActive(false);
    }
}
