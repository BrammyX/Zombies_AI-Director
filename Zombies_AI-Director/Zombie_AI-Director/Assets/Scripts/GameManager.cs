using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Tracked Game Metrics")]
    public int totalKills;
    public int pointsEarned;
    public int damagedTaken;
    public int roundsCompleted;
    public int shotsFired;
    public int shotsHit;
    public float timeSurvived;

    [Header("Tracked Round Metrics")]
    public float timeToCompleteRound;
    public int damageTakenThisRound;
    public int shotsFiredThisRound;
    public int shotsHitThisRound;

    [Header("Scores")]
    public float timeFactor;
    public float damageFactor;
    public float accuracy;
    public float accuracyFactor;
    public float totalScore;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    public void Update()
    {
        timeSurvived += Time.deltaTime;
        timeToCompleteRound += Time.deltaTime;

    }

    public void AddKill()
    {
        totalKills++;
    }

    public void AddPoints(int amount)
    {
        pointsEarned += amount;
    }

    public void AddDamageTaken(int amount)
    {
        damagedTaken += amount;
        damageTakenThisRound += amount;
    }

    public void ShotFired()
    {
        shotsFired++;
        shotsFiredThisRound++;
    }

    public void ShotHit()
    {
        shotsHit++;
        shotsHitThisRound++;
    }

    public void RoundCompleted()
    {
        roundsCompleted++;

        if (roundsCompleted > 1)
        {
            CalculatePerformanceScore();

            if (AIDirectior.Instance.dynamicAdjustment)
            {
                AIDirectior.Instance.AdjustDifficulty();
            }
        }
        else
        {
            AIDirectior.Instance.currentConfig.ResetToDefaults(); 
        }
        
        timeToCompleteRound = 0;
        damageTakenThisRound = 0;
        shotsFiredThisRound = 0;
        shotsHitThisRound = 0;
    }

    public float CalculatePerformanceScore()
    {
        timeFactor = Mathf.Clamp(60f / timeToCompleteRound, 0f, 1f);
        damageFactor = Mathf.Clamp01(1f - (damageTakenThisRound / 100f));
        accuracy = (float)shotsHitThisRound / shotsFiredThisRound;
        accuracyFactor = Mathf.Clamp01(accuracy);

        totalScore = (timeFactor * 0.33f) + (damageFactor * 0.33f) + (accuracyFactor * 0.33f);

        return totalScore;
    }
}
