using System.Collections;
using UnityEngine;

public class Runner : EnemyBase
{
    protected override void Update()
    {
        base.Update();


        if (isDead || !isInitiated)
        {
            return;
        }


        float dist = Distance(transform, target.transform); //플레이어와의 거리
        timer += Time.deltaTime;

        if (isRunningAwayFromBomb) //도망가는 동안
        {
            if (timer > runningAwayTime) //도망 시간 다 끝나면
            {
                StartCoroutine(RunningAwayCooltimer());

                isRunningAwayFromBomb = false;
                timer = 0;
            }

        }

        else if (dist > traceDistance || isBreakTime) //거리가 멀리 있거나 휴식시간일 경우
        {
            if (timer > repositionTime) //자리 랜덤 재배치
            {
                agent.speed = initWalkSpeed;
                walkSpeed = initWalkSpeed;

                int X = Random.Range(1, 20);
                int Z = Random.Range(1, 20);

                targetPosition = new Vector3(X, 0, Z);
                timer = 0;
            }
        }
        else //범위 내에 있고 휴식시간이 아닐 경우
        {
            if (!isDead) //죽지 않았다면
            {
                agent.speed = initWalkSpeed * 1.5f; //1.5배속
                walkSpeed = initWalkSpeed * 1.5f;

                if (!isSatisfied)
                {
                    IsSatisfied();
                }
                targetPosition = target.transform.position;
            }
        }

        agent.SetDestination(targetPosition);
    }
    protected override IEnumerator OnSatisfied()
    {
        agent.speed = initWalkSpeed * 2f;
        walkSpeed = initWalkSpeed * 2f;
        yield return new WaitForSeconds(3f);
        agent.speed = initWalkSpeed;
        walkSpeed = initWalkSpeed;
        isBreakTime = true;
        yield return new WaitForSeconds(breakTime);
        isBreakTime = false;

        if (isSatisfied)
        {
            isSatisfied = false;
        }
    }
}
