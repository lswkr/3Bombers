using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectCharacter : NetworkBehaviour
{
    [SerializeField] private Transform clearShot;
    [SerializeField] private GameObject[] statImages;
    [SerializeField] private Animator[] anims;
    [SerializeField] private CinemachineCamera introCam;

    private int currentSelectedPlayer = 0;
    private int currentIndex = 0;
    private int characterCount = 3;
    private Dictionary<string, CinemachineCamera> cameraDict = new Dictionary<string, CinemachineCamera>();

    private bool isSelected = false;
    private bool isCamMoving = false;
    private bool isStageMoving = false;

    private void Awake()
    {
        if (clearShot == null)
        {
            Debug.LogError("카메라가 할당되지 않았습니다.");
            return;
        }
        for (int i = 0; i < clearShot.childCount - 1; i++)
        {
            Transform child = clearShot.GetChild(i);
            CinemachineCamera cam = child.GetComponent<CinemachineCamera>();

            if (!cameraDict.ContainsKey(child.name))
            {
                cameraDict.Add(child.name, cam);
            }
        }
    }
    IEnumerator Start()
    {
        for (int i = 0; i < statImages.Length; i++)
        {
            statImages[i].SetActive(false);
        }
        yield return new WaitForSeconds(1);
        introCam.Priority = 0;
        cameraDict["0Cam"].Priority = 10;
        StartCoroutine(MovingCamRoutine());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (isCamMoving || isStageMoving || isSelected)
                return;
            StartCoroutine(MovingCamRoutine());
            int pre = currentIndex++;
            currentIndex %= characterCount;
            SetCameraPriority($"{pre}Cam", $"{currentIndex}Cam");
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (isCamMoving || isStageMoving || isSelected)
                return;
            StartCoroutine(MovingCamRoutine());
            int pre = currentIndex--;
            currentIndex = (characterCount + currentIndex) % characterCount;
            SetCameraPriority($"{pre}Cam", $"{currentIndex}Cam");
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isCamMoving || isStageMoving || isSelected)
                return;

            MultiplayManager.Instance.OnSelectCharacterRpc(NetworkManager.Singleton.LocalClientId, currentIndex);//선택한 캐릭터 인덱스를 Id와 함께 Manager에 추가
            isSelected = true;

            SelectRpc();
            //StartCoroutine(MoveToStageRoutine());
        }
    }

    IEnumerator MovingCamRoutine()
    {
        isCamMoving = true;
        for (int i = 0; i < statImages.Length; i++)
        {
            statImages[i].SetActive(false);
        }
        yield return new WaitForSeconds(1.4f);
        isCamMoving = false;
        statImages[currentIndex].SetActive(true);
    }

    private void SetCameraPriority(string from, string to)
    {
        cameraDict[from].Priority = 0;
        cameraDict[to].Priority = 10;
    }

    IEnumerator MoveToStageRoutine()
    {

        DataManager.Instance.characterIndex = currentIndex;

        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(2);
    }



    private IEnumerator MoveToStage()
    {
        if (!IsServer)
            yield break;

        yield return new WaitForSeconds(2f);

        NetworkManager.Singleton.SceneManager.LoadScene("Stage1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    [Rpc(SendTo.Server)]
    private void SelectRpc()
    {
        currentSelectedPlayer++;

        if (currentSelectedPlayer == NetworkManager.Singleton.ConnectedClients.Count)
        {
            Debug.Log("모두 선택 완료");
            BeforeMoveToStageRpc();
            StartCoroutine(MoveToStage());
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BeforeMoveToStageRpc() //춤추게 시키기
    {
        isStageMoving = true;
        anims[currentIndex].SetTrigger("Dance");
    }
}
