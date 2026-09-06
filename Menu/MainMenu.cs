using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;

    [SerializeField] GameObject makeRoomCanvas;
    [SerializeField] GameObject findRoomCanvas;

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] Button createRoomButton;

    private void Start()
    {
        if(GameObject.FindAnyObjectByType<NetworkManager>()==null)
        {
            SceneManager.LoadScene("ConnectingScene");
            return;
        }

        hostButton.onClick.AddListener(() => { if(!makeRoomCanvas.activeSelf) makeRoomCanvas.SetActive(true); });
        clientButton.onClick.AddListener(() => { if (!findRoomCanvas.activeSelf) findRoomCanvas.SetActive(true); });

        createRoomButton.onClick.AddListener(async () => { await HostSingleton.Instance.StartHostAsync((roomNameInputField.text.IsNullOrEmpty()? "room": roomNameInputField.text)); });
    }

}
