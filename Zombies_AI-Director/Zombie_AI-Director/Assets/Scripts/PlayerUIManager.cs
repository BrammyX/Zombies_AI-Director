using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance;

    [SerializeField] private Image bloodOverlay;
    [SerializeField] private Image hitMarkerOverlay;
    [SerializeField] private TextMeshProUGUI ammoReserveText;
    [SerializeField] private TextMeshProUGUI ammoMagText;
    [SerializeField] private TextMeshProUGUI currentRoundText;
    [SerializeField] private TextMeshProUGUI interactableText;
    [SerializeField] private TextMeshProUGUI pointText;

    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private TextMeshProUGUI totatKillsText;
    [SerializeField] private TextMeshProUGUI pointsEarnedText;
    [SerializeField] private TextMeshProUGUI damagedTakenText;
    [SerializeField] private TextMeshProUGUI roundsCompletedText;
    [SerializeField] private TextMeshProUGUI shotsFiredText;
    [SerializeField] private TextMeshProUGUI shotsHitText;
    [SerializeField] private TextMeshProUGUI timeSurvivedText;

    public float maxOverlayAlpha = 200;
    public float fadeOutSpeed = 1.5f;
    public float targetAlpha;
    public float currentAlpha;

    private float showTime;

    private int startPoints;
    private int targetPoints;
    private float lerpDuration = 0.5f;
    private float lerpTimer;
    private bool isLerping = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeOutSpeed);

        Color c = bloodOverlay.color;
        c.a = Mathf.Clamp01(currentAlpha / 255f);
        bloodOverlay.color = c;

        if (hitMarkerOverlay.enabled)
        {
            showTime -= Time.deltaTime;
            if (showTime <= 0)
            {
                hitMarkerOverlay.enabled = false;
            }
        }

        if (isLerping)
        {
            lerpTimer += Time.deltaTime;
            float t = lerpTimer / lerpDuration;
            int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, targetPoints, t));
            pointText.text = currentPoints.ToString();

            if (t >= 1f)
            {
                pointText.text = targetPoints.ToString();
                isLerping = false;
            }
        }
    }

    public void UpdateAmmo(int current, int reserve)
    {
        ammoMagText.text = current.ToString();
        ammoReserveText.text = reserve.ToString();
    }

    public void UpdateRound(int currentRound)
    {
        currentRoundText.text = currentRound.ToString();
    }

    public void FadeInOverlay(int health, int maxHealth)
    {
        float healthPercent = (float)health / maxHealth;

        targetAlpha = Mathf.Lerp(maxOverlayAlpha, 0f, healthPercent);
    }

    public void FadeOutOverlay()
    {
        targetAlpha = 0;
    }

    public void ShowHitMarkerOverlay()
    {
        hitMarkerOverlay.enabled = true;
        showTime = 0.1f;
    }

    public void UpdateInteractText(string promptMessage)
    {
        interactableText.text = promptMessage;
    }

    public void UpdatePointText(int newPoints)
    {
        startPoints = int.TryParse(pointText.text, out int current) ? current : 0;
        targetPoints = newPoints;
        lerpTimer = 0f;
        isLerping = true;
    }

    public void ShowMetricsUI()
    {
        gameOverUI.SetActive(true);

        totatKillsText.text = "Total Kills: " + GameManager.Instance.totalKills;
        pointsEarnedText.text = "Total Points: " + GameManager.Instance.pointsEarned;
        damagedTakenText.text = "Total Damage Taken: " + GameManager.Instance.damagedTaken;
        roundsCompletedText.text = "Total Rounds Completed: " + GameManager.Instance.roundsCompleted;
        shotsFiredText.text = "Total Shots Fired: " + GameManager.Instance.shotsFired;
        shotsHitText.text = "Total Shots Hit: " + GameManager.Instance.shotsHit;
        timeSurvivedText.text = "Total Time Survived: " + GameManager.Instance.timeSurvived;
    }

}
