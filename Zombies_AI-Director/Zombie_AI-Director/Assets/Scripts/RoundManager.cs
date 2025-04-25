using Demo.Scripts.Runtime.Character;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [Header("Spawn Info")]
    public GameObject aiPrefab;
    public List<Transform> spawnPoints;

    [Header("Round Settings")]
    public float spawnRoundDelay = 5f; 
    public int currentRound = 0;
    public int aiRemaining;
    public int aiToSpawm;
    public bool isSpawning = false;
    public bool roundFinsihed = false;


    private void Start()
    {
        StartNextRound();
    }

    private void Update()
    {
        if (!isSpawning && aiRemaining <= 0 && roundFinsihed)
        {
            StartNextRound();
        }
    }

    private void StartNextRound()
    {
        roundFinsihed = false;
        StartCoroutine(NextRoundDelay());
    }

    private IEnumerator SpawnAI()
    {
        isSpawning = true;

        for (int i = 0; i < aiToSpawm; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            GameObject ai = Instantiate(aiPrefab, spawnPoint.position, Quaternion.identity);

            AIManager aiManager = ai.GetComponent<AIManager>();
            if (aiManager != null)
            {
                aiManager.SetRoundManager(this);
            }

            float minSpawnDelay = 0.5f * AIDirectior.Instance.currentConfig.aiSpawnRateModifier;
            float maxSpawnDelay = 2f * AIDirectior.Instance.currentConfig.aiSpawnRateModifier;

            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
        }

        isSpawning = false;
    }

    private IEnumerator NextRoundDelay()
    {
        yield return new WaitForSeconds(spawnRoundDelay);

        currentRound++;     
        aiToSpawm = CalculateAIForRound(currentRound);
        aiRemaining = aiToSpawm;
        PlayerUIManager.Instance.UpdateRound(currentRound);
        GameManager.Instance.RoundCompleted();
        StartCoroutine(SpawnAI());
        roundFinsihed = true;
    }

    public void AIDied()
    {
        aiRemaining--;
    }

    private int CalculateAIForRound(int round)
    {
        int baseAmount = Mathf.FloorToInt(10.5f * Mathf.Pow(1.1f, round));

        float multiplier = AIDirectior.Instance.currentConfig.aiSpawnAmountModifier;

        return Mathf.RoundToInt(baseAmount * multiplier);
    }
}
