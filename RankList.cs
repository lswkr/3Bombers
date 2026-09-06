using Firebase.Database;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class RankList : MonoBehaviour
{
    private FirebaseDatabase database;
    private DatabaseReference reference;

    [SerializeField] private Transform rankItemParent;
    [SerializeField] private RankUIItem rankItemPrefab;
    [SerializeField] private TextMeshProUGUI indicatorText;

    private void OnEnable()
    {
        if (database == null && reference == null)
        {
            database = FirebaseDatabase.GetInstance(Constants.FIREBASEURL);
            reference = database.RootReference;
        }

        indicatorText.gameObject.SetActive(false);

        GetRank();
    }

    private void OnDisable()
    {
        Transform parentTrans = rankItemParent.transform;
        for (int i = parentTrans.childCount - 1; i >= 0; i--)
        {
            Destroy(parentTrans.GetChild(i).gameObject);
        }
    }


    private async void GetRank()
    {
        try
        {
            DataSnapshot snapshot = await reference.Child("UserScore").OrderByChild("score").LimitToLast(10).GetValueAsync();

            if (!snapshot.Exists)
            {
                indicatorText.gameObject.SetActive(true);
                return;
            }

            List<UserScore> topScore = new List<UserScore>();

            foreach (var child in snapshot.Children)
            {
                UserScore userScore = JsonUtility.FromJson<UserScore>(child.GetRawJsonValue());

                topScore.Add(userScore);
            }
            if(topScore.Count==0)
            {
                indicatorText.gameObject.SetActive(true);
                return; 
            }
            topScore.Reverse();

            for (int i = 1; i <= topScore.Count; i++)
            {
                RankUIItem rankUIItem = Instantiate(rankItemPrefab, rankItemParent);
                rankUIItem.SetItem(i, topScore[i - 1].playerNickname, topScore[i - 1].score);
            }
        }
        catch(Exception ex)
        {
            Debug.LogError($"에러가 발생했습니다.{ex}");
        }

    }

}
