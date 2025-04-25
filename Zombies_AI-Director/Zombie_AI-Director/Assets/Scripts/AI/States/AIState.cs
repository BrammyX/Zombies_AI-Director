using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIState : ScriptableObject
{
    public virtual AIState Tick(AIManager manager)
    {
        return this;
    }

    protected virtual AIState SwitchState(AIManager manager, AIState newState) 
    {
        ResetStateFlags(manager);
        return newState;
    }

    protected virtual void ResetStateFlags(AIManager manager) 
    {

    }
}
