using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class ItemManager : SingletonCore<ItemManager>
{
    [SerializeField] GameObject[] items;
    [SerializeField] int randomRate = 30;

    public void SpawnItems(int r, int c)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (Random.Range(0, 100) <= randomRate)
        {
            StartCoroutine(SpawnItemCoroutine(r, c));
        }
    }

    IEnumerator SpawnItemCoroutine(int r, int c)
    {
        if (!NetworkManager.Singleton.IsServer)
            yield break;

        yield return new WaitForSeconds(1f);

        int spawnIndex = Random.Range(0, items.Length);
        GameObject spawnedItem = Instantiate(items[spawnIndex]);

        if (spawnedItem)
        {
            spawnedItem.transform.position = new Vector3(c, 0, r);
            NetworkObject netItem = spawnedItem.GetComponent<NetworkObject>();
            if (netItem)
            {
                netItem.Spawn();
            }
        }

    }
}
