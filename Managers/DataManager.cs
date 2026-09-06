using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DataManager : SingletonCore<DataManager>
{
    public List<Tuple<int, string>> rankingList = new List<Tuple<int, string>>();

    public int characterIndex = 0;

    //지울 것들
    //public int stageNum = 0;


    public bool isGameOver = false;

    public Action onDead;
    public Action onRevive;
    public Action onGameOver;
    public Action onGameComplete;
    public Action<int, int> onGetScore;

    private int mapNumber = 2;//맵 개수
    private int rangeLimit = 8;
    private float speedLimit = 15;
    private int countLimit = 10;

    public int RangeLimit { get { return rangeLimit; } private set { rangeLimit = value; } }
    public float SpeedLimit { get { return speedLimit; } private set { speedLimit = value; } }
    public int CountLimit { get { return countLimit; } private set { countLimit = value; } }

    public string userName;

    private int score = 0;
    //public int PlayerLife { get; private set; }

    //public void GameStartInit()
    //{
    //    PlayerLife = MAXLIFE;
    //    score = 0;
    //    //stageNum = 0;
    //    isGameOver = false;
    //}

    //public void OnDead()
    //{
    //    if (PlayerLife <= 0)
    //    {
    //        onGameOver?.Invoke();
    //        SaveScore();
    //        isGameOver = true;
    //        return;
    //    }
    //    PlayerLife--;
    //    //onUpdateLife?.Invoke(PlayerLife);
    //    onDead?.Invoke();
    //}
    
    public void SaveScore(ulong clientID)
    {
        //if (NetworkManager.Singleton.LocalClientId != clientID) return;
        string clientNickname = ServerSingleton.Instance.GetUserInfo(clientID).playerNickname;
        LoginInfoManager.Instance.SaveScore(clientNickname, score);
    }
    public void OnRevive()
    {
        onRevive?.Invoke();
    }

    public void OnStart()
    {
        //onUpdateLife?.Invoke(PlayerLife);
        //onGetScore?.Invoke(score, score);
    }
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void GetScore(int incomeScore)
    {
        int preScore = score;
        score += incomeScore;

        WidgetManager.Instance.RaiseScore(preScore, score);
        //TODO: 스코어 검사 후 목숨 업
    }

    public string OnRankingOn()
    {
        string a = "";

        if (rankingList.Count == 0)
        {
            a = "아직 기록이\n존재하지 않습니다.";
        }
        else
        {
            for (int i = 1; i <= rankingList.Count; i++)
            {
                a += $"{i}  {rankingList[i - 1].Item2}  {rankingList[i - 1].Item1}\n";
            }
        }
        return a;
    }

    public struct PlayerCharacterData : INetworkSerializable
    {
        public int range;
        public int count;
        public float speed;
        public int life;

        public PlayerCharacterData(int range, int count, float speed, int life)
        {
            this.range = range;
            this.count = count;
            this.speed = speed;
            this.life = life;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref range);
            serializer.SerializeValue(ref count);
            serializer.SerializeValue(ref speed);
            serializer.SerializeValue(ref life);
        }
    }


    private Dictionary<ulong, PlayerCharacterData> loadedData = new Dictionary<ulong, PlayerCharacterData>(); //데이터 저장용 딕셔너리

    public void SavePlayerCharacterData(ulong clientID, PlayerCharacterData characterData)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            loadedData[clientID] = characterData;

            //Debug.Log($"ID: {clientID}, 데이터: {characterData.range}, {characterData.count}, {characterData.speed}, {characterData.life}");
        }
    }

    public PlayerCharacterData LoadPlayerCharacterData(ulong clientID)
    {
        if (loadedData.ContainsKey(clientID))
        {
            return loadedData[clientID];
        }

        return new PlayerCharacterData(-1, -1, -1, -1);
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name.Contains("Stage"))
        {
            GetScore(0);
        }
    }

    public void OnGameComplete()
    {
        score = 0;
    }
}
