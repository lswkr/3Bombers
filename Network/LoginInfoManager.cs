using Firebase.Database;
using Newtonsoft.Json;
using System;
using UnityEngine;

[Serializable]
public class UserScore
{
    public string playerNickname;
    public int score;

    public UserScore(string playerNickname, int score)
    {
        this.playerNickname = playerNickname;
        this.score = score;
    }
}

public class LoginInfoManager : MonoBehaviour
{
    private static LoginInfoManager instance;
    public static LoginInfoManager Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindAnyObjectByType<LoginInfoManager>();
                
                if(instance == null)
                {
                    GameObject obj = new GameObject("LoginInfoManager");
                    instance = obj.AddComponent<LoginInfoManager>();

                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    string playerID;

    private FirebaseDatabase database;
    private DatabaseReference reference;

    string playerNickname;
    public string PlayerNickname
    {
        get
        {
            return playerNickname;
        }
        private set
        {
            playerNickname = value;
        }
    }
    public void Init(string playerID, string playerNickname)
    {

        database = FirebaseDatabase.GetInstance(Constants.FIREBASEURL);
        reference = database.RootReference;

        this.playerID = playerID;
        this.playerNickname = playerNickname;
    }

    public void SetUserInfo(ref string playerID, ref string playerNickname)
    {
        playerID = this.playerID;
        playerNickname = this.playerNickname;
    }


    public async void SaveScore(string playerNickname, int score)
    {
        try
        {
            Debug.Log($"{playerNickname} 점수등록: {score}");
            UserScore userScore = new UserScore(playerNickname, score);
            string jsonData = JsonConvert.SerializeObject(userScore);

            await reference.Child("UserScore").Push().SetRawJsonValueAsync(jsonData).ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.LogError($"저장 실패: {task.Exception}");
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("파이어베이스 등록 성공!");
                }
            });
        }

        catch (Exception ex)
        {
            Debug.LogError($"점수 저장 중 에러가 발생했습니다.{ex}");
        }
    }
}
