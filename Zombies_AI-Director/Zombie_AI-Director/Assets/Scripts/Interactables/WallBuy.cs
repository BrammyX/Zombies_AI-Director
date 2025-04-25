using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBuy : Interactable
{
    FPSController fpsController;

    public void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            fpsController = player.GetComponent<FPSController>();
        }
    }

    protected override void Interact()
    {
        if (fpsController == null)
            return;

        finalAmmoCost = Mathf.RoundToInt(baseAmmoCost * AIDirectior.Instance.currentConfig.ammoPriceModifier);

        fpsController.TryToBuyWeapon(weapon, weaponCost, finalAmmoCost);
    }
}
