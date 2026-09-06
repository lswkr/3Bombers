using UnityEngine;

public class CountUp : Item
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isApplied)
            {
                isApplied = true;
                other.GetComponent<StatusComponent>().OnCountUp();
                StartCoroutine(DisappearDelay());
            }
        }
    }
}
