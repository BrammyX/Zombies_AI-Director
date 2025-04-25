using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/States/Attack State")]
public class AttackState : AIState
{
    public override AIState Tick(AIManager manager)
    {
        if (manager.target == null)
            return SwitchState(manager, manager.idleState);

        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.target.position);

        if (distanceToTarget > manager.attackRange)
            return SwitchState(manager, manager.pursueState);

        Vector3 lookDirection = (manager.target.position - manager.transform.position);
        Vector3.Normalize(lookDirection);
        lookDirection.y = 0;
        manager.transform.rotation = Quaternion.Slerp(manager.transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 5f);
        
        manager.navMeshAgent.isStopped = true;

        if (!manager.animator.GetBool("isAttacking"))
        {
            float animationMutliplier = Mathf.Clamp(manager.finalChaseSpeed, 1f, 2);
            manager.animator.SetFloat("AnimMutliplier", animationMutliplier);
            manager.animator.SetBool("isAttacking", true);
        }

        return this;
    }

    protected override void ResetStateFlags(AIManager manager)
    {
        base.ResetStateFlags(manager);

        manager.animator.SetBool("isAttacking", false);
        manager.navMeshAgent.isStopped = false;
    }
}
