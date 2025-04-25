using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AIDirectior : MonoBehaviour
{
    public static AIDirectior Instance;

    [Header("Config Settings")]
    public AIDirectorConfig currentConfig;
    public bool dynamicAdjustment = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (!dynamicAdjustment)
        {
            currentConfig.ResetToDefaults();
        }
    }

    public void AdjustDifficulty()
    {
        var gameManager = GameManager.Instance;

        if (gameManager.accuracy < 0.5f)
        {
            currentConfig.maxAmmoDropChanceModifier = 0.10f;
        }
        else
        {
            currentConfig.maxAmmoDropChanceModifier = 0f;
        }
        
        float finalAmmoPriceModifier = gameManager.accuracy < 0.5f ? 0.6f : 1f;
        currentConfig.ammoPriceModifier = ClampChange(currentConfig.ammoPriceModifier, finalAmmoPriceModifier, currentConfig.maxAmmoPriceChange);

        float finalAIspeed = Mathf.Lerp(0.8f, 2f, gameManager.damageFactor);
        currentConfig.aiSpeedModifier = ClampChange(currentConfig.aiSpeedModifier, finalAIspeed, currentConfig.maxSpeedChange);   // If player took damage, slow AI down based on score.

        int finalAIDamage = Mathf.RoundToInt(Mathf.Lerp(-10, 10, gameManager.damageFactor));
        currentConfig.aiDamageModifier = ClampChange(currentConfig.aiDamageModifier, finalAIDamage, currentConfig.maxDamageChange);   // Changed AI damage based on damaged taken score.

        float finalSpawnRate = Mathf.Lerp(1.5f, 0.6f, gameManager.timeFactor);
        currentConfig.aiSpawnRateModifier = ClampChange(currentConfig.aiSpawnRateModifier, finalSpawnRate, currentConfig.maxSpawnRateChange);   // Speed up or down AI depending on time factor score.

        int finalAIHealth = Mathf.RoundToInt(Mathf.Lerp(-10, 50, gameManager.timeFactor));
        currentConfig.aiHealthModifier = ClampChange(currentConfig.aiHealthModifier, finalAIHealth, currentConfig.maxHealthChange);   // Adds more health to AI if time factor score is good.

        float finalSpawnAmount = Mathf.Lerp(1.0f, 2f, gameManager.timeFactor);
        currentConfig.aiSpawnAmountModifier = ClampChange(currentConfig.aiSpawnAmountModifier, finalSpawnAmount, currentConfig.maxSpawnAmountChange);   // Adds more enemies if time factor score is good.          
    }

    private float ClampChange(float previous, float target, float maxChange)
    {
        return Mathf.Clamp(target, previous - maxChange, previous + maxChange);
    }

    private int ClampChange(int previous, int target, int maxChange)
    {
        return Mathf.Clamp(target, previous - maxChange, previous - maxChange);
    }
}
