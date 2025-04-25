using Demo.Scripts.Runtime.Character;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class maxAmmoPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FPSController fPSController = other.GetComponent<FPSController>();
            if (fPSController != null)
            {
                fPSController.RefillAllAmmo();
            }

            Destroy(gameObject);
        }
    }
}
