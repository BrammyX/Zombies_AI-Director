using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/States/Idle")]
public class IdleState : AIState
{
    public override AIState Tick(AIManager manager)
    {
        if (manager.target != null)
        {
            return SwitchState(manager, manager.pursueState);
        }

        return this;
    }
}
