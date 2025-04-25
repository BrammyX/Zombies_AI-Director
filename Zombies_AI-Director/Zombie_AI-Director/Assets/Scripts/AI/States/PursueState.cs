using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/States/Pursue State")]
public class PursueState : AIState
{
    public override AIState Tick(AIManager manager)
    {
        if (manager.target == null)
            return SwitchState(manager, manager.idleState);

        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.target.position);

        if (distanceToTarget <= manager.attackRange)
            return SwitchState(manager, manager.attackState);

        if (!manager.navMeshAgent.enabled)
            manager.navMeshAgent.enabled = true;

        manager.navMeshAgent.speed = manager.finalChaseSpeed;
        manager.navMeshAgent.SetDestination(manager.target.position);

        return this;
    }
}
