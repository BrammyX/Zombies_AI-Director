using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string promptMessage;
    public Weapon weapon;
    public int weaponCost;
    public int baseAmmoCost;
    public int finalAmmoCost;

    public void BaseInteract()
    {
        Interact();
    }
    
    protected virtual void Interact()
    {

    }
}
