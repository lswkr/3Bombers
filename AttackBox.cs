using System;
using UnityEngine;

public class AttackBox : MonoBehaviour
{
    float curDamage;
    int curCombo;
    public Action isHit;
    public void ComboSetter(int combo)
    {
        curCombo = combo;
        Debug.Log(curCombo);
        curDamage *= (1f + ((float)combo * 0.1f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            isHit?.Invoke();
        }
    }

}
