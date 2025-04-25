using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using KINEMATION.KAnimationCore.Runtime.Input;

using Demo.Scripts.Runtime.AttachmentSystem;

using System.Collections.Generic;
using Demo.Scripts.Runtime.Character;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace Demo.Scripts.Runtime.Item
{
    public class Weapon : FPSItem
    {
        [Header("Weapon Info")]
        public string weaponID;

        [Header("General")]
        [SerializeField] [Range(0f, 120f)] private float fieldOfView = 90f;
        
        [SerializeField] private FPSAnimationAsset reloadClip;
        [SerializeField] private FPSCameraAnimation cameraReloadAnimation;
        
        [SerializeField] private FPSAnimationAsset grenadeClip;
        [SerializeField] private FPSCameraAnimation cameraGrenadeAnimation;

        [Header("Recoil")]
        [SerializeField] private FPSAnimationAsset fireClip;
        [SerializeField] private RecoilAnimData recoilData;
        [SerializeField] private RecoilPatternSettings recoilPatternSettings;
        [SerializeField] private FPSCameraShake cameraShake;

        [Header("Ammo")]
        [Min(0f)] public int ammoMagazine;
        [Min(0f)] public int ammoCapacity;
        [Min(0f)] public int currentAmmo;
        [Min(0f)] public int ammoInReserve;

        [Header("Fire")]
        [SerializeField] private Transform muzzlePoint;
        [Min(0f)][SerializeField] private float fireRate;
        [SerializeField] private int damage;
        [SerializeField] private int maxPeneration;
        [SerializeField] private float maxDistance;

        [Header("Effects")]
        [SerializeField] private GameObject gunShotEffect;

        [Header("SFX")]
        [SerializeField] private AudioClip gunShotSFX;
        [SerializeField] private AudioClip gunShotEmptySFX;
        [SerializeField] private AudioClip gunRemoveMagSFX;
        [SerializeField] private AudioClip gunInsertMagSFX;
        [SerializeField] private AudioClip gunSlideSFX;
        [SerializeField] private AudioClip gunSwitchModeSFX;

        [Header("Fire Modes")]
        [SerializeField] private bool supportsAuto;
        [SerializeField] private bool supportsBurst;
        [SerializeField] private int burstLength;

        [Header("Attachments")] 
        
        [SerializeField]
        private AttachmentGroup<BaseAttachment> barrelAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        private AttachmentGroup<BaseAttachment> gripAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        private List<AttachmentGroup<ScopeAttachment>> scopeGroups = new List<AttachmentGroup<ScopeAttachment>>();

        private FPSController _fpsController;
        private Animator _controllerAnimator;
        private UserInputController _userInputController;
        private IPlayablesController _playablesController;
        private FPSCameraController _fpsCameraController;
        private AudioSource audioSource;
        
        private FPSAnimator _fpsAnimator;
        private FPSAnimatorEntity _fpsAnimatorEntity;

        private RecoilAnimation _recoilAnimation;
        private RecoilPattern _recoilPattern;
        
        private Animator _weaponAnimator;
        private int _scopeIndex;
        
        private float _lastRecoilTime;
        private int _bursts;
        private FireMode _fireMode = FireMode.Semi;

        private bool hasBeenInitalized = false;

        private static readonly int CurveEquip = Animator.StringToHash("CurveEquip");
        private static readonly int CurveUnequip = Animator.StringToHash("CurveUnequip");

        private void OnActionEnded()
        {
            if (_fpsController == null) return;
            _fpsController.ResetActionState();
        }

        protected void UpdateTargetFOV(bool isAiming)
        {
            float fov = fieldOfView;
            float sensitivityMultiplier = 1f;
            
            if (isAiming && scopeGroups.Count != 0)
            {
                var scope = scopeGroups[_scopeIndex].GetActiveAttachment();
                fov *= scope.aimFovZoom;

                sensitivityMultiplier = scopeGroups[_scopeIndex].GetActiveAttachment().sensitivityMultiplier;
            }

            _userInputController.SetValue("SensitivityMultiplier", sensitivityMultiplier);
            _fpsCameraController.UpdateTargetFOV(fov);
        }

        protected void UpdateAimPoint()
        {
            if (scopeGroups.Count == 0) return;

            var scope = scopeGroups[_scopeIndex].GetActiveAttachment().aimPoint;
            _fpsAnimatorEntity.defaultAimPoint = scope;
        }
        
        protected void InitializeAttachments()
        {
            foreach (var attachmentGroup in scopeGroups)
            {
                attachmentGroup.Initialize(_fpsAnimator);
            }
            
            _scopeIndex = 0;
            if (scopeGroups.Count == 0) return;

            UpdateAimPoint();
            UpdateTargetFOV(false);
        }
        
        public override void OnEquip(GameObject parent)
        {
            if (parent == null) return;

            if (!hasBeenInitalized)
            {
                InitalizeAmmo(ammoMagazine, ammoCapacity);
                hasBeenInitalized = true;
            }             

            PlayerUIManager.Instance.UpdateAmmo(currentAmmo, ammoInReserve);

            _fpsAnimator = parent.GetComponent<FPSAnimator>();
            _fpsAnimatorEntity = GetComponent<FPSAnimatorEntity>();
            
            _fpsController = parent.GetComponent<FPSController>();
            _weaponAnimator = GetComponentInChildren<Animator>();
            
            _controllerAnimator = parent.GetComponent<Animator>();
            _userInputController = parent.GetComponent<UserInputController>();
            _playablesController = parent.GetComponent<IPlayablesController>();
            _fpsCameraController = parent.GetComponentInChildren<FPSCameraController>();
            audioSource = parent.GetComponent<AudioSource>();

            if (overrideController != _controllerAnimator.runtimeAnimatorController)
            {
                _playablesController.UpdateAnimatorController(overrideController);
            }
            
            InitializeAttachments();
            
            _recoilAnimation = parent.GetComponent<RecoilAnimation>();
            _recoilPattern = parent.GetComponent<RecoilPattern>();
            
            _fpsAnimator.LinkAnimatorProfile(gameObject);
            
            barrelAttachments.Initialize(_fpsAnimator);
            gripAttachments.Initialize(_fpsAnimator);
            
            _recoilAnimation.Init(recoilData, fireRate, _fireMode);

            if (_recoilPattern != null)
            {
                _recoilPattern.Init(recoilPatternSettings);
            }
            
            _fpsAnimator.LinkAnimatorLayer(equipMotion);
        }

        public override void OnUnEquip()
        {
            _fpsAnimator.LinkAnimatorLayer(unEquipMotion);
        }

        public override bool OnAimPressed()
        {
            _userInputController.SetValue(FPSANames.IsAiming, true);
            UpdateTargetFOV(true);
            _recoilAnimation.isAiming = true;
            
            return true;
        }

        public override bool OnAimReleased()
        {
            _userInputController.SetValue(FPSANames.IsAiming, false);
            UpdateTargetFOV(false);
            _recoilAnimation.isAiming = false;
            
            return true;
        }

        public override bool OnFirePressed()
        {
            if (Time.unscaledTime - _lastRecoilTime < 60f / fireRate)
            {
                return false;
            }

            _lastRecoilTime = Time.unscaledTime;
            _bursts = burstLength;
            
            OnFire();
            
            return true;
        }

        public override bool OnFireReleased()
        {
            if (_recoilAnimation != null)
            {
                _recoilAnimation.Stop();
            }
            
            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireEnd();
            }
            
            CancelInvoke(nameof(OnFire));
            return true;
        }

        public override bool OnReload()
        {
            if (ammoInReserve <= 0)
                return false;

            if (currentAmmo == ammoMagazine)
                return false;

            if (!FPSAnimationAsset.IsValid(reloadClip))
            {
                return false;
            }
            
            _playablesController.PlayAnimation(reloadClip, 0f);
            
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Rebind();
                _weaponAnimator.Play("Reload", 0);
            }

            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);
            }
            
            Invoke(nameof(FinishReload), reloadClip.clip.length * 0.85f);

            OnFireReleased();
            return true;
        }

        public override bool OnGrenadeThrow()
        {
            if (!FPSAnimationAsset.IsValid(grenadeClip))
            {
                return false;
            }

            _playablesController.PlayAnimation(grenadeClip, 0f);
            
            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraGrenadeAnimation);
            }
            
            Invoke(nameof(OnActionEnded), grenadeClip.clip.length * 0.8f);
            return true;
        }
        
        private void OnFire()
        {
            if (currentAmmo <= 0)
            {
                OnFireReleased();
                audioSource.PlayOneShot(gunShotEmptySFX, 0.3f);
                return;
            }

            FireBullet();
            audioSource.PlayOneShot(gunShotSFX, 0.3f);
            currentAmmo--;
            PlayerUIManager.Instance.UpdateAmmo(currentAmmo, ammoInReserve);

            if (_weaponAnimator != null)
            {
                _weaponAnimator.Play("Fire", 0, 0f);
            }
            
            _fpsCameraController.PlayCameraShake(cameraShake);
            
            if(fireClip != null) _playablesController.PlayAnimation(fireClip);

            if (_recoilAnimation != null && recoilData != null)
            {
                _recoilAnimation.Play();
            }

            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireStart();
            }

            if (_recoilAnimation.fireMode == FireMode.Semi)
            {
                Invoke(nameof(OnFireReleased), 60f / fireRate);
                return;
            }
            
            if (_recoilAnimation.fireMode == FireMode.Burst)
            {
                _bursts--;
                
                if (_bursts == 0)
                {
                    OnFireReleased();
                    return;
                }
            }
            
            Invoke(nameof(OnFire), 60f / fireRate);
        }

        public override void OnCycleScope()
        {
            if (scopeGroups.Count == 0) return;
            
            _scopeIndex++;
            _scopeIndex = _scopeIndex > scopeGroups.Count - 1 ? 0 : _scopeIndex;
            
            UpdateAimPoint();
            UpdateTargetFOV(true);
        }

        private void CycleFireMode()
        {
            audioSource.PlayOneShot(gunSwitchModeSFX, 0.3f);
            if (_fireMode == FireMode.Semi && supportsBurst)
            {
                _fireMode = FireMode.Burst;
                _bursts = burstLength;
                return;
            }

            if (_fireMode != FireMode.Auto && supportsAuto)
            {
                _fireMode = FireMode.Auto;
                return;
            }

            _fireMode = FireMode.Semi;
        }
        
        public override void OnChangeFireMode()
        {
            CycleFireMode();
            _recoilAnimation.fireMode = _fireMode;
        }

        public override void OnAttachmentChanged(int attachmentTypeIndex)
        {
            if (attachmentTypeIndex == 1)
            {
                barrelAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (attachmentTypeIndex == 2)
            {
                gripAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (scopeGroups.Count == 0) return;
            scopeGroups[_scopeIndex].CycleAttachments(_fpsAnimator);
            UpdateAimPoint();
        }

        public override void FinishReload()
        {
            int magSize = ammoMagazine;
            int neededAmmo = magSize - currentAmmo;

            int ammoToLoad = Mathf.Min(neededAmmo, ammoInReserve);

            currentAmmo += ammoToLoad;
            ammoInReserve -= ammoToLoad;

            PlayerUIManager.Instance.UpdateAmmo(currentAmmo, ammoInReserve);

            OnActionEnded();
        }

        public override void InitalizeAmmo(int current, int reserve)
        {
            currentAmmo = Mathf.Clamp(current, 0, ammoMagazine);
            ammoInReserve = Mathf.Clamp(reserve, 0, ammoCapacity);
            PlayerUIManager.Instance.UpdateAmmo(currentAmmo, ammoInReserve);
        }

        public void RefillAmmo()
        {
            currentAmmo = ammoMagazine;
            ammoInReserve = ammoCapacity;
            PlayerUIManager.Instance.UpdateAmmo(currentAmmo, ammoInReserve);
        }

        public override void FireBullet()
        {
            Instantiate(gunShotEffect, muzzlePoint.position, muzzlePoint.rotation);
            GameManager.Instance.ShotFired();

            float remainingDistance = maxDistance;
            int remainingPenetration = maxPeneration;

            RaycastHit[] hits = Physics.RaycastAll(muzzlePoint.position, muzzlePoint.forward, maxDistance);
            System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

            foreach (RaycastHit hit in hits)
            {
                if (remainingPenetration <= 0 || remainingDistance <= 0)
                    break;

                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, _fpsController);
                    remainingPenetration--;
                    PlayerUIManager.Instance.ShowHitMarkerOverlay();
                    GameManager.Instance.ShotHit();
                }

                remainingDistance -= hit.distance;
            }
        }

        public void PlayMagOutSFX()
        {
            audioSource.PlayOneShot(gunRemoveMagSFX, 0.3f);
        }

        public void PlayMagInSFX()
        {
            audioSource.PlayOneShot(gunInsertMagSFX, 0.3f);
        }

        public void PlayGunSlideSFX()
        {
            audioSource.PlayOneShot(gunSlideSFX, 0.3f);
        }

        public string GetWeaponName()
        {
            return weaponID;
        }
    }
}