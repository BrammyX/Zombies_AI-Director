using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI Director/Config")]
public class AIDirectorConfig : ScriptableObject
{
    [Header("Weapon Cost Config")]
    public float ammoPriceModifier = 1.0f;

    [Header("AI Config")]
    public float aiSpawnRateModifier = 1.0f;
    public float aiSpawnAmountModifier = 1.0f;
    public float aiSpeedModifier = 1.0f;
    public int aiHealthModifier = 0;
    public int aiDamageModifier = 0;

    [Header("AI Modifier Limits")]
    public float maxAmmoPriceChange = 0.1f;
    public float maxSpeedChange = 0.1f;
    public float maxSpawnRateChange = 0.2f;
    public float maxSpawnAmountChange = 0.2f;
    public int maxHealthChange = 5;
    public int maxDamageChange = 2;

    [Header("Drop Chance")]
    public float maxAmmoDropChanceModifier = 0f;

    public void ResetToDefaults()
    {
        ammoPriceModifier = 1.0f;
        aiSpawnRateModifier = 1.0f;
        aiSpawnAmountModifier = 1.0f;
        aiSpeedModifier = 1.0f;
        aiHealthModifier = 0;
        aiDamageModifier = 0;
    }
}
