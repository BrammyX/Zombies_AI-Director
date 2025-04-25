using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Rig;

using Demo.Scripts.Runtime.Item;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Diagnostics;

namespace Demo.Scripts.Runtime.Character
{
    public enum FPSAimState
    {
        None,
        Ready,
        Aiming,
        PointAiming
    }

    public enum FPSActionState
    {
        None,
        PlayingAnimation,
        WeaponChange,
        AttachmentEditing
    }

    [RequireComponent(typeof(CharacterController), typeof(FPSMovement))]
    public class FPSController : MonoBehaviour , IDamageable
    {
        [SerializeField] private FPSControllerSettings settings;

        [Header("Health")]
        public int maxHealth;
        public int currentHealth;
        public int regenAmount;
        public float regenDelay;
        private bool isDead;

        [Header("Points")]
        public int points;
        public event Action<int> OnPointsChanged;

        [Header("Footsteps")]
        [SerializeField] private float baseStepSpeed = 0.5f;
        [SerializeField] private float crouchStepMultipler = 1.5f;
        [SerializeField] private float sprintStepMultipler = 0.6f;
        public AudioClip[] footstepsConcrete;
        public float footStepTimer = 0;
        private float GetCurrentOffset => isCrouching ? baseStepSpeed * crouchStepMultipler : IsSprinting() ? baseStepSpeed * sprintStepMultipler : baseStepSpeed;

        private bool isCrouching = false;

        public AudioSource audioSource;

        public int Health { get => currentHealth; set => currentHealth = value; }

        public LayerMask mask;

        private FPSMovement _movementComponent;
        private Camera cam;

        private Transform _weaponBone;
        private Vector2 _playerInput;

        private int _activeWeaponIndex;
        private int _previousWeaponIndex;

        private FPSAimState _aimState;
        private FPSActionState _actionState;

        private Animator _animator;

        private FPSAnimator _fpsAnimator;
        private UserInputController _userInput;

        private List<FPSItem> _instantiatedWeapons;
        private Vector2 _lookDeltaInput;

        private RecoilPattern _recoilPattern;
        private int _sensitivityMultiplierPropertyIndex;

        private static int _fullBodyWeightHash = Animator.StringToHash("FullBodyWeight");
        private static int _proneWeightHash = Animator.StringToHash("ProneWeight");
        private static int _inspectStartHash = Animator.StringToHash("InspectStart");
        private static int _inspectEndHash = Animator.StringToHash("InspectEnd");
        private static int _slideHash = Animator.StringToHash("Sliding");

        private bool _isLeaning;
        private Interactable currentInteractable = null;

        private void PlayTransitionMotion(FPSAnimatorLayerSettings layerSettings)
        {
            if (layerSettings == null)
            {
                return;
            }
            
            _fpsAnimator.LinkAnimatorLayer(layerSettings);
        }

        private bool IsSprinting()
        {
            return _movementComponent.MovementState == FPSMovementState.Sprinting;
        }
        
        private bool HasActiveAction()
        {
            return _actionState != FPSActionState.None;
        }

        private bool IsAiming()
        {
            return _aimState is FPSAimState.Aiming or FPSAimState.PointAiming;
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Application.targetFrameRate = 120;

            _fpsAnimator = GetComponent<FPSAnimator>();
            _fpsAnimator.Initialize();
            cam = GetComponentInChildren<Camera>();
            audioSource = GetComponent<AudioSource>();

            _weaponBone = GetComponentInChildren<KRigComponent>().GetRigTransform(settings.weaponBone);
            _animator = GetComponent<Animator>();
            
            _userInput = GetComponent<UserInputController>();
            _recoilPattern = GetComponent<RecoilPattern>();

            InitializeMovement();
            InitializeWeapons();

            _actionState = FPSActionState.None;
            EquipWeapon();

            _sensitivityMultiplierPropertyIndex = _userInput.GetPropertyIndex("SensitivityMultiplier");

            maxHealth = 100;
            currentHealth = maxHealth;
            regenAmount = 1;
            regenDelay = 5f;

            isDead = false;

            points = 500;
            OnPointsChanged += PlayerUIManager.Instance.UpdatePointText;
            PlayerUIManager.Instance.UpdatePointText(points);     
        }
       
        private void Update()
        {
            if (currentHealth <= 0)
            {
                PlayerUIManager.Instance.ShowMetricsUI();
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = settings.timeScale;
            UpdateLookInput();
            OnMovementUpdated();

            regenDelay += Time.deltaTime;

            Interact();
            RegenHealth();
            HandleFootSteps();
        }

        public void ResetActionState()
        {
            _actionState = FPSActionState.None;
        }

        #region Initalize
        private void InitializeMovement()
        {
            _movementComponent = GetComponent<FPSMovement>();

            _movementComponent.onJump = () => { PlayTransitionMotion(settings.jumpingMotion); };
            _movementComponent.onLanded = () => { PlayTransitionMotion(settings.jumpingMotion); };

            _movementComponent.onCrouch = OnCrouch;
            _movementComponent.onUncrouch = OnUncrouch;

            _movementComponent.onSprintStarted = OnSprintStarted;
            _movementComponent.onSprintEnded = OnSprintEnded;

            _movementComponent.onSlideStarted = OnSlideStarted;

            _movementComponent._slideActionCondition += () => !HasActiveAction();
            _movementComponent._sprintActionCondition += () => !HasActiveAction();
            _movementComponent._proneActionCondition += () => !HasActiveAction();

            _movementComponent.onStopMoving = () =>
            {
                PlayTransitionMotion(settings.stopMotion);
            };

            _movementComponent.onProneEnded = () =>
            {
                _userInput.SetValue(FPSANames.PlayablesWeight, 1f);
            };
        }

        private void InitializeWeapons()
        {
            _instantiatedWeapons = new List<FPSItem>();

            foreach (var prefab in settings.weaponPrefabs)
            {
                var weapon = Instantiate(prefab, transform.position, Quaternion.identity);
                var weaponTransform = weapon.transform;

                weaponTransform.parent = _weaponBone;
                weaponTransform.localPosition = Vector3.zero;
                weaponTransform.localRotation = Quaternion.identity;

                _instantiatedWeapons.Add(weapon.GetComponent<FPSItem>());

                weapon.gameObject.SetActive(false);
            }
        }

        #endregion

        public void Interact()
        {
            PlayerUIManager.Instance.UpdateInteractText(string.Empty);

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, 2, mask))
            {
                if (hitInfo.collider.GetComponent<Interactable>() != null) 
                {
                    currentInteractable = hitInfo.collider.GetComponent<Interactable>();

                    if (currentInteractable.weapon != null)
                    {
                        string weaponName = currentInteractable.weapon.name;
                        int weaponCost = currentInteractable.weaponCost;
                        int ammoCost = currentInteractable.finalAmmoCost;

                        bool hasWeapon = false;

                        foreach (var weapon in _instantiatedWeapons)
                        {
                            if (weapon is Weapon existingWeapon && existingWeapon.weaponID == currentInteractable.weapon.weaponID)
                            {
                                hasWeapon = true;
                                break;
                            }
                        }

                        string promptMessage = hasWeapon ? $"Press [E] to buy ammo for {weaponName} for {ammoCost} points" : $"Press [E] to buy {weaponName} for {weaponCost} points";
                        PlayerUIManager.Instance.UpdateInteractText(promptMessage);
                    }
                    else
                    {
                        PlayerUIManager.Instance.UpdateInteractText(currentInteractable.promptMessage);
                    }
                }
                else
                {
                    currentInteractable = null;
                }
            }
        }

        public void AddPoints(int amount)
        {
            points += amount;
            OnPointsChanged?.Invoke(points);
        }

        public void SubtractPoints(int amount)
        {
            points -= amount;
            OnPointsChanged?.Invoke(points);
        }

        public void RefillAllAmmo()
        {
            foreach (var weapon in _instantiatedWeapons)
            {
                if (weapon is Weapon existingWeapon)
                {
                    existingWeapon.RefillAmmo();
                }
            }
        }

        public bool TryToBuyWeapon(Weapon weaponPrefab, int weaponCost, int ammoCost)
        {
            bool canBuyWeapon = points >= weaponCost;
            bool canBuyAmmo = points >= ammoCost;

            if (!canBuyWeapon && !canBuyAmmo) return false;

            

            if (canBuyAmmo)
            {
                foreach (var weapon in _instantiatedWeapons)
                {
                    if (weapon is Weapon existingWeapon && existingWeapon.weaponID == weaponPrefab.weaponID)
                    {
                        if (existingWeapon.ammoInReserve == existingWeapon.ammoCapacity)
                            return false;

                        existingWeapon.RefillAmmo();
                        SubtractPoints(ammoCost);
                        return true;
                    }
                }
            }

            if (canBuyWeapon)
            {
                if (_instantiatedWeapons.Count >= 2)
                {
                    var currentWeapon = GetActiveItem();

                    _instantiatedWeapons.Remove(currentWeapon);
                    Destroy(currentWeapon.gameObject);

                    _activeWeaponIndex = 0;
                    _previousWeaponIndex = 0;
                }

                var newWeaponObj = Instantiate(weaponPrefab.gameObject, _weaponBone);
                var newWeapon = newWeaponObj.GetComponent<Weapon>();
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localRotation = Quaternion.identity;
                newWeaponObj.SetActive(false);

                _instantiatedWeapons.Add(newWeapon);
                StartWeaponChange(_instantiatedWeapons.IndexOf(newWeapon));

                SubtractPoints(weaponCost);
                return true;
            }  
            
            return false;
        }

 
        #region Equiping
        private void UnequipWeapon()
        {
            DisableAim();
            _actionState = FPSActionState.WeaponChange;
            GetActiveItem().OnUnEquip();
        }

        private void EquipWeapon()
        {
            if (_instantiatedWeapons.Count == 0) return;

            _instantiatedWeapons[_previousWeaponIndex].gameObject.SetActive(false);
            GetActiveItem().gameObject.SetActive(true);
            GetActiveItem().OnEquip(gameObject);

            _actionState = FPSActionState.None;
        }

        public FPSItem GetActiveItem()
        {
            if (_instantiatedWeapons.Count == 0) return null;
            return _instantiatedWeapons[_activeWeaponIndex];
        }

        public bool HasWeapon(FPSItem weapon)
        {
            return _instantiatedWeapons.Contains(weapon);
        }

        private void StartWeaponChange(int newIndex)
        {
            if (newIndex == _activeWeaponIndex || newIndex > _instantiatedWeapons.Count - 1)
            {
                return;
            }

            UnequipWeapon();

            OnFireReleased();
            Invoke(nameof(EquipWeapon), settings.equipDelay);

            _previousWeaponIndex = _activeWeaponIndex;
            _activeWeaponIndex = newIndex;
        }
        #endregion

        #region Fire and Aim
        private void DisableAim()
        {
            if (GetActiveItem().OnAimReleased()) _aimState = FPSAimState.None;
        }

        private void OnFirePressed()
        {
            if (currentHealth <= 0)
                return;

            if (_instantiatedWeapons.Count == 0 || HasActiveAction()) return;
            GetActiveItem().OnFirePressed();
        }

        private void OnFireReleased()
        {
            if (_instantiatedWeapons.Count == 0) return;
            GetActiveItem().OnFireReleased();
        }

        #endregion

        #region Sprint & Slide
        private void OnSlideStarted()
        {
            _animator.CrossFade(_slideHash, 0.2f);
        }

        private void OnSprintStarted()
        {
            OnFireReleased();
            DisableAim();

            _aimState = FPSAimState.None;

            _userInput.SetValue(FPSANames.StabilizationWeight, 0f);
            _userInput.SetValue("LookLayerWeight", 0.3f);
        }

        private void OnSprintEnded()
        {
            _userInput.SetValue(FPSANames.StabilizationWeight, 1f);
            _userInput.SetValue("LookLayerWeight", 1f);
        }
        #endregion

        #region Crouch
        private void OnCrouch()
        {
            isCrouching = true;
            PlayTransitionMotion(settings.crouchingMotion);
        }

        private void OnUncrouch()
        {
            isCrouching = false;
            PlayTransitionMotion(settings.crouchingMotion);
        }
        #endregion

        #region Movement & Look
        private void UpdateLookInput()
        {
            if (isDead)
                return;

            float scale = _userInput.GetValue<float>(_sensitivityMultiplierPropertyIndex);

            float deltaMouseX = _lookDeltaInput.x * settings.sensitivity * scale;
            float deltaMouseY = -_lookDeltaInput.y * settings.sensitivity * scale;

            _playerInput.y += deltaMouseY;
            _playerInput.x += deltaMouseX;

            if (_recoilPattern != null)
            {
                _playerInput += _recoilPattern.GetRecoilDelta();
                deltaMouseX += _recoilPattern.GetRecoilDelta().x;
            }

            float proneWeight = _animator.GetFloat(_proneWeightHash);
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);

            transform.rotation *= Quaternion.Euler(0f, deltaMouseX, 0f);

            _userInput.SetValue(FPSANames.MouseDeltaInput, new Vector4(deltaMouseX, deltaMouseY));
            _userInput.SetValue(FPSANames.MouseInput, new Vector4(_playerInput.x, _playerInput.y));
        }

        private void OnMovementUpdated()
        {
            if (isDead)
                return;

            float playablesWeight = 1f - _animator.GetFloat(_fullBodyWeightHash);
            _userInput.SetValue(FPSANames.PlayablesWeight, playablesWeight);
        }

        private void HandleFootSteps()
        {
            if (_movementComponent.MovementState == FPSMovementState.InAir) return;
            if (_movementComponent.MovementState == FPSMovementState.Idle) return;

            footStepTimer -= Time.deltaTime;

            if (footStepTimer <= 0)
            {
                audioSource.PlayOneShot(footstepsConcrete[UnityEngine.Random.Range(0, footstepsConcrete.Length - 1)], 0.1f);

                footStepTimer = GetCurrentOffset;
            }
        }

        #endregion

        #region Health
        public void TakeDamage(int damage, FPSController fPSController)
        {
            currentHealth -= damage;
            regenDelay = 0f;
            PlayerUIManager.Instance.FadeInOverlay(currentHealth, maxHealth);
            GameManager.Instance.AddDamageTaken(damage);

            
        }

        public void RegenHealth()
        {
            if (regenDelay > 5f && currentHealth < maxHealth)
            {
                currentHealth += regenAmount * Time.deltaTime > 0 ? regenAmount : 0;
                currentHealth = Mathf.Min(currentHealth, maxHealth);

                PlayerUIManager.Instance.FadeOutOverlay();
            }
        }

        #endregion

        #region Inputs
#if ENABLE_INPUT_SYSTEM
        public void OnReload()
        {
            if (IsSprinting() || HasActiveAction() || !GetActiveItem().OnReload()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }

        public void OnThrowGrenade()
        {
            if (IsSprinting()|| HasActiveAction() || !GetActiveItem().OnGrenadeThrow()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }
        
        public void OnFire(InputValue value)
        {
            if (IsSprinting()) return;
            
            if (value.isPressed)
            {
                OnFirePressed();
                return;
            }
            
            OnFireReleased();
        }

        public void OnAim(InputValue value)
        {
            if (IsSprinting()) return;

            if (value.isPressed && !IsAiming())
            {
                if (GetActiveItem().OnAimPressed()) _aimState = FPSAimState.Aiming;
                PlayTransitionMotion(settings.aimingMotion);
                return;
            }

            if (!value.isPressed && IsAiming())
            {
                DisableAim();
                PlayTransitionMotion(settings.aimingMotion);
            }
        }

        public void OnChangeWeapon()
        {
            if (_movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || _instantiatedWeapons.Count == 0) return;
            
            StartWeaponChange(_activeWeaponIndex + 1 > _instantiatedWeapons.Count - 1 ? 0 : _activeWeaponIndex + 1);
        }

        public void OnLook(InputValue value)
        {
            _lookDeltaInput = value.Get<Vector2>();
        }

        public void OnLean(InputValue value)
        {
            _userInput.SetValue(FPSANames.LeanInput, value.Get<float>() * settings.leanAngle);
            PlayTransitionMotion(settings.leanMotion);
        }

        public void OnCycleScope()
        {
            if (!IsAiming()) return;
            
            GetActiveItem().OnCycleScope();
            PlayTransitionMotion(settings.aimingMotion);
        }

        public void OnChangeFireMode()
        {
            GetActiveItem().OnChangeFireMode();
        }

        public void OnToggleAttachmentEditing()
        {
            if (HasActiveAction() && _actionState != FPSActionState.AttachmentEditing) return;
            
            _actionState = _actionState == FPSActionState.AttachmentEditing 
                ? FPSActionState.None : FPSActionState.AttachmentEditing;

            if (_actionState == FPSActionState.AttachmentEditing)
            {
                _animator.CrossFade(_inspectStartHash, 0.2f);
                return;
            }
            
            _animator.CrossFade(_inspectEndHash, 0.3f);
        }

        public void OnDigitAxis(InputValue value)
        {
            if (!value.isPressed || _actionState != FPSActionState.AttachmentEditing) return;
            GetActiveItem().OnAttachmentChanged((int) value.Get<float>());
        }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed && currentInteractable != null)
            {
                currentInteractable.BaseInteract();
            }
        }
#endif
    }
    #endregion
}