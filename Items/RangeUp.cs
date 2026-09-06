using UnityEngine;

public class RangeUp : Item
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isApplied)
            {
                isApplied = true;
                other.GetComponent<StatusComponent>().OnRangeUp();
                StartCoroutine(DisappearDelay());
            }
        }
    }
}
