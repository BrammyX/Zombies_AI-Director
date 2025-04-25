using Demo.Scripts.Runtime.Character;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class AIManager : MonoBehaviour, IDamageable
{
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform target;
    [HideInInspector] public NavMeshAgent navMeshAgent;
    CharacterController characterController;
    SphereCollider sphereCollider;
    RoundManager roundManager;

    [Header("Current State")]
    [SerializeField] AIState currentState;

    [Header("States")]
    public IdleState idleState;
    public PursueState pursueState;
    public AttackState attackState;

    [Header("Stats")]
    public int baseMaxHealth;
    public int finalMaxHealth;
    public int currentHealth;
    public bool isDead = false;

    [Header("Movement")]
    public float baseChaseSpeed;
    public float finalChaseSpeed;

    [Header("Attack")]
    public float attackRange;
    public int finalAttackDamage;
    public int baseAttackDamage;

    [Header("Points")]
    public int hitPoints = 10;
    public int killPoints = 50;

    [Header("Pick Up")]
    public GameObject maxAmmoPickupPrefab;
    public float baseDropChance = 0.01f;
    public float finalDropChance;

    private float deathTimer;

    public float randomSeed;
    public int Health { get => currentHealth; set => currentHealth = value; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        target = Camera.main.transform;
        navMeshAgent = GetComponent<NavMeshAgent>();
        characterController = GetComponent<CharacterController>();
        sphereCollider = GetComponentInChildren<SphereCollider>();

        idleState = Instantiate(idleState);
        pursueState = Instantiate(pursueState);
        attackState = Instantiate(attackState);

        currentState = idleState;

        baseChaseSpeed = 0.9f;
        baseMaxHealth = 100;
        baseAttackDamage = 25;
        attackRange = 2f;

        finalChaseSpeed = baseChaseSpeed;
        finalAttackDamage = baseAttackDamage;
        finalMaxHealth = baseMaxHealth;
        currentHealth = baseMaxHealth;

        randomSeed = Random.Range(0.1f, 0.3f);
        sphereCollider.enabled = false;
    }

    private void FixedUpdate()
    {
        ProcessStateMachine();   
    }

    private void Update()
    {
        if (isDead)
        {
            deathTimer += Time.deltaTime;
            
            if (deathTimer >= 30f)
            {
                Destroy(gameObject);
            }

            return;
        }

        UpdateAnimator();

        if (currentHealth <= 0)
        {
            isDead = true;
            TriggerDeathAnimation();
            characterController.enabled = false;
            navMeshAgent.enabled = false;
            roundManager.AIDied();
            DisableDamageCollider();
            DropChancePickUps();
        }
    }

    private void ProcessStateMachine()
    {
        if (isDead)
            return;

        AIState nextState = currentState?.Tick(this);

        if (nextState != null) 
        {
            currentState = nextState;
        }
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", finalChaseSpeed + randomSeed);
    }

    public void TakeDamage(int damage, FPSController fpsController)
    {
        currentHealth -= damage;

        if (fpsController != null)
        {
            fpsController.AddPoints(hitPoints);
            GameManager.Instance.AddPoints(hitPoints);
        }

        if (currentHealth <= 0 && fpsController != null)
        {
            fpsController.AddPoints(killPoints);
            GameManager.Instance.AddKill();
            GameManager.Instance.AddPoints(killPoints);
        }
    }

    private void TriggerDeathAnimation()
    {
        int deathIndex = Random.Range(0, 2);

        if (deathIndex == 0)
        {
            animator.Play("AI Death");
        }
        else
        {
            animator.Play("AI Death 2");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();

        if (other.tag != "Player")
            return;

        if (damageable != null)
        {
            damageable.TakeDamage(finalAttackDamage, null);

            sphereCollider.enabled = false;
        }
    }

    public void EnableDamageCollider()
    {
        sphereCollider.enabled = true;
    }

    public void DisableDamageCollider()
    {
        sphereCollider.enabled = false;
    }

    public void SetRoundManager(RoundManager manager)
    {
        roundManager = manager;
        ScaleSpeedWithRound(manager.currentRound);

        finalAttackDamage = baseAttackDamage + AIDirectior.Instance.currentConfig.aiDamageModifier;
        finalMaxHealth = Mathf.Max(baseMaxHealth + AIDirectior.Instance.currentConfig.aiHealthModifier, 1);

        currentHealth = finalMaxHealth;

    }

    private void ScaleSpeedWithRound(int round)
    {
        float SpeedIncreasePerRound = 0.15f;

        float scaleSpeed = baseChaseSpeed + (round - 1) * SpeedIncreasePerRound;
        scaleSpeed = Mathf.Min(scaleSpeed, 2.5f);

        float aiModifier = AIDirectior.Instance.currentConfig.aiSpeedModifier;
        finalChaseSpeed = scaleSpeed * aiModifier;

        navMeshAgent.speed = finalChaseSpeed;
    }

    private void DropChancePickUps()
    {
        finalDropChance = baseDropChance + AIDirectior.Instance.currentConfig.maxAmmoDropChanceModifier;

        if (Random.value <= finalDropChance)
        {
            Instantiate(maxAmmoPickupPrefab, transform.position + new Vector3(0, 0.7f, 0), Quaternion.identity);
        }
    }

}