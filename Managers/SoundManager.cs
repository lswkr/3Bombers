using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource[] bgmAudios;
    [SerializeField] private AudioSource[] eventAudios;
    [SerializeField] private AudioClip[] clips;

    [SerializeField] private Slider bgmVolume;
    [SerializeField] private Slider eventVolume;

    [SerializeField] private Button TurnOnButton;
    [SerializeField] private GameObject soundWindow;

    private void Start()
    {
        TurnOnButton.onClick.AddListener(TurnOnSoundWindow);
        bgmVolume.onValueChanged.AddListener(BgmVolume);
        eventVolume.onValueChanged.AddListener(EventVolume);

        foreach (var audio in bgmAudios)
        {
            bgmVolume.value = audio.volume;
        }

        foreach (var audio in eventAudios)
        {
            eventVolume.value = audio.volume;
        }
    }

    public void SoundPlay(string clipName)
    {
        foreach(var audio in bgmAudios)
        {
            if(!audio.isPlaying)
            {
                foreach(var clip in clips)
                {
                    if(clip.name.Equals(clipName))
                    {
                        audio.clip = clip;
                        audio.loop = true;
                        audio.Play();
                        return;
                    }
                }
                Debug.LogError($"{clipName} 파일을 찾을 수 없습니다.");
            }
        }
        Debug.LogError("재생가능한 AudioSource가 없습니다.");
    }

    public void SoundOneShot(string clipName)
    {
        foreach (var audio in eventAudios)
        {
            if (!audio.isPlaying)
            {
                foreach (var clip in clips)
                {
                    if (clip.name.Equals(clipName))
                    {
                        audio.PlayOneShot(clip);
                        return;
                    }
                }
                Debug.LogError($"{clipName} 파일을 찾을 수 없습니다.");
            }
        }
        Debug.LogError("재생가능한 AudioSource가 없습니다.");
    }

    private void BgmVolume(float volume)
    {
        foreach(var audio in bgmAudios)
        {
            audio.volume = volume;
        }
    }

    private void EventVolume(float volume)
    {
        foreach(var audio in eventAudios)
        {
            audio.volume = volume;
        }
    }

    private void TurnOnSoundWindow()
    {
        if(soundWindow.activeSelf)
        {
            soundWindow.SetActive(false);
        }
        else
        {
            soundWindow.SetActive(true);
        }
    }
}
