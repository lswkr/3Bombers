using UnityEngine;

public class ClientExplosion : Explosion
{
    [SerializeField] GameObject explosionPrefab;
    private void OnEnable()
    {
        explosionPrefab.SetActive(true);
    }
}
