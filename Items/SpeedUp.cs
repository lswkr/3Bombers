using UnityEngine;

public class SpeedUp : Item
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isApplied)
            {
                isApplied = true;
                other.GetComponent<StatusComponent>().OnSpeedUp();
                StartCoroutine(DisappearDelay());
            }
        }
    }
}
