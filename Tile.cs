using UnityEngine;

public class Tile : MonoBehaviour
{
    private int r, c;

    public void SetTilePosition(int r,int c)
    {
        this.r = r;
        this.c = c;
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if(other.CompareTag("Player"))
    //    {
    //        other.GetComponent<PlayerMovement>().GetTilePosition(r,c);
    //    }
    //}
}
