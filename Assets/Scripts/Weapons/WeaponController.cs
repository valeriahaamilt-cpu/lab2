using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectBreachpoint
{
    public sealed class WeaponController : MonoBehaviour
    {
        private sealed class WeaponInstance
        {
            public WeaponData Data;
            public int AmmoInMagazine;
            public int ReserveAmmo;
        }

        [SerializeField] private Transform aimSource;
        [SerializeField] private Transform viewModelRoot;
        [SerializeField] private bool controlledByPlayer;

        private readonly Dictionary<WeaponSlot, WeaponInstance> inventory = new Dictionary<WeaponSlot, WeaponInstance>();
        private PlayerHealth owner;
        private FirstPersonController movement;
        private RecoilSystem recoil;
        private RoundManager roundManager;
        private WeaponInstance currentWeapon;
        private float nextFireTime;
        private bool reloading;
        private GameObject viewModel;
        private LineRenderer tracer;

        public WeaponData CurrentWeapon
        {
            get { return currentWeapon != null ? currentWeapon.Data : null; }
        }

        public int CurrentAmmo
        {
            get { return currentWeapon != null ? currentWeapon.AmmoInMagazine : 0; }
        }

        public int ReserveAmmo
        {
            get { return currentWeapon != null ? currentWeapon.ReserveAmmo : 0; }
        }

        public bool IsReloading
        {
            get { return reloading; }
        }

        public Transform AimSource
        {
            get { return aimSource; }
            set { aimSource = value; }
        }

        private void Awake()
        {
            owner = GetComponent<PlayerHealth>();
            movement = GetComponent<FirstPersonController>();
            recoil = GetComponent<RecoilSystem>();
            if (recoil == null)
            {
                recoil = gameObject.AddComponent<RecoilSystem>();
            }

            roundManager = FindObjectOfType<RoundManager>();
        }

        private void Update()
        {
            if (!controlledByPlayer || owner == null || !owner.IsAlive)
            {
                return;
            }

            if (Cursor.visible)
            {
                return;
            }

            HandleWeaponInput();
        }

        public void Initialize(PlayerHealth newOwner, Transform newAimSource, bool isPlayer)
        {
            owner = newOwner;
            aimSource = newAimSource;
            controlledByPlayer = isPlayer;
            movement = GetComponent<FirstPersonController>();
            recoil = GetComponent<RecoilSystem>();
            if (recoil == null)
            {
                recoil = gameObject.AddComponent<RecoilSystem>();
            }

            roundManager = FindObjectOfType<RoundManager>();
        }

        public void SetViewModelRoot(Transform root)
        {
            viewModelRoot = root;
        }

        public void EquipWeapon(WeaponData data)
        {
            if (data == null)
            {
                return;
            }

            WeaponInstance instance = new WeaponInstance
            {
                Data = data,
                AmmoInMagazine = data.MagazineSize,
                ReserveAmmo = data.ReserveAmmo
            };

            inventory[data.Slot] = instance;
            EquipSlot(data.Slot);
        }

        public bool HasWeapon(string id)
        {
            foreach (WeaponInstance instance in inventory.Values)
            {
                if (instance.Data.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasSlot(WeaponSlot slot)
        {
            return inventory.ContainsKey(slot);
        }

        public void EquipSlot(WeaponSlot slot)
        {
            WeaponInstance instance;
            if (!inventory.TryGetValue(slot, out instance))
            {
                return;
            }

            currentWeapon = instance;
            reloading = false;
            nextFireTime = Time.time + instance.Data.EquipTime * 0.15f;
            recoil.SetWeapon(instance.Data);
            BuildViewModel(instance.Data);
        }

        public void OnOwnerDied()
        {
            reloading = false;
        }

        public void BotFireAt(Vector3 targetPoint, float botInaccuracy)
        {
            if (currentWeapon == null || owner == null || !owner.IsAlive)
            {
                return;
            }

            Vector3 origin = GetOrigin();
            Vector3 direction = (targetPoint - origin).normalized;
            Fire(direction, botInaccuracy, false);
        }

        public void Reload()
        {
            if (currentWeapon == null || reloading || currentWeapon.Data.Category == WeaponCategory.Knife)
            {
                return;
            }

            if (currentWeapon.AmmoInMagazine >= currentWeapon.Data.MagazineSize || currentWeapon.ReserveAmmo <= 0)
            {
                return;
            }

            StartCoroutine(ReloadRoutine(currentWeapon));
        }

        private void HandleWeaponInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                EquipSlot(WeaponSlot.Primary);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                EquipSlot(WeaponSlot.Pistol);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                EquipSlot(WeaponSlot.Knife);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Reload();
            }

            if (currentWeapon == null || reloading)
            {
                return;
            }

            bool wantsFire = currentWeapon.Data.FireMode == FireMode.Automatic
                ? Input.GetMouseButton(0)
                : Input.GetMouseButtonDown(0);

            if (wantsFire)
            {
                Fire(GetAimDirection(), GetPlayerInaccuracy(), true);
            }
        }

        private IEnumerator ReloadRoutine(WeaponInstance weapon)
        {
            reloading = true;
            yield return new WaitForSeconds(weapon.Data.ReloadTime);

            int needed = weapon.Data.MagazineSize - weapon.AmmoInMagazine;
            int loaded = Mathf.Min(needed, weapon.ReserveAmmo);
            weapon.AmmoInMagazine += loaded;
            weapon.ReserveAmmo -= loaded;
            reloading = false;
        }

        private void Fire(Vector3 baseDirection, float extraInaccuracy, bool useRecoil)
        {
            if (roundManager != null && roundManager.CurrentPhase != RoundPhase.Live && roundManager.CurrentPhase != RoundPhase.BombPlanted)
            {
                return;
            }

            WeaponData data = currentWeapon.Data;
            if (Time.time < nextFireTime)
            {
                return;
            }

            if (data.Category != WeaponCategory.Knife && currentWeapon.AmmoInMagazine <= 0)
            {
                Reload();
                return;
            }

            nextFireTime = Time.time + 1f / Mathf.Max(0.1f, data.FireRate);

            if (data.Category != WeaponCategory.Knife)
            {
                currentWeapon.AmmoInMagazine--;
            }

            float spread = GetSpread(extraInaccuracy);
            Vector2 recoilPattern = useRecoil && recoil != null ? recoil.ApplyShot(data) : Vector2.zero;
            if (!controlledByPlayer)
            {
                spread += recoilPattern.magnitude * 0.003f;
            }

            int pellets = Mathf.Max(1, data.Pellets);
            for (int i = 0; i < pellets; i++)
            {
                Vector3 direction = ApplySpread(baseDirection, spread);
                PerformRaycast(direction, data);
            }

            DrawTracer(GetOrigin(), GetOrigin() + baseDirection * Mathf.Min(data.Range, 45f));
        }

        private float GetSpread(float extraInaccuracy)
        {
            WeaponData data = currentWeapon.Data;
            if (data.Category == WeaponCategory.Knife)
            {
                return 0f;
            }

            float spread = data.BaseSpread;
            if (recoil != null && recoil.RecoilIndex == 0)
            {
                spread *= Mathf.Lerp(0.2f, 1f, 1f - data.FirstShotAccuracy);
            }

            spread += extraInaccuracy;
            if (recoil != null)
            {
                spread += recoil.GetSprayPenalty();
            }

            return Mathf.Clamp(spread, 0f, 0.35f);
        }

        private float GetPlayerInaccuracy()
        {
            if (movement == null || currentWeapon == null)
            {
                return 0f;
            }

            WeaponData data = currentWeapon.Data;
            float penalty = movement.AccuracyMovementPenalty * data.MovementSpreadPenalty;
            if (!movement.IsGrounded)
            {
                penalty += data.JumpSpreadPenalty;
            }

            if (movement.IsCrouching)
            {
                penalty *= 1f - data.CrouchAccuracyBonus;
            }

            return penalty;
        }

        private Vector3 GetOrigin()
        {
            return aimSource != null ? aimSource.position : transform.position + Vector3.up * 1.5f;
        }

        private Vector3 GetAimDirection()
        {
            return aimSource != null ? aimSource.forward : transform.forward;
        }

        private Vector3 ApplySpread(Vector3 direction, float spread)
        {
            if (spread <= 0.0001f)
            {
                return direction.normalized;
            }

            Vector2 random = Random.insideUnitCircle * spread;
            Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Vector3 spreadDirection = rotation * new Vector3(random.x, random.y, 1f);
            return spreadDirection.normalized;
        }

        private void PerformRaycast(Vector3 direction, WeaponData data)
        {
            Vector3 origin = GetOrigin();
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, data.Range, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Hitbox hitbox = hits[i].collider.GetComponent<Hitbox>();
                PlayerHealth victim = hitbox != null ? hitbox.Owner : hits[i].collider.GetComponentInParent<PlayerHealth>();

                if (victim == owner)
                {
                    continue;
                }

                if (victim != null && victim.IsAlive)
                {
                    float distanceFactor = Mathf.Clamp01(hits[i].distance / data.Range);
                    float baseDamage = data.BaseDamage * Mathf.Lerp(1f, 1f - data.DamageFalloff, distanceFactor);
                    DamageInfo info = new DamageInfo
                    {
                        Attacker = owner,
                        Victim = victim,
                        Weapon = data,
                        HitZone = hitbox != null ? hitbox.Zone : HitZone.Chest,
                        HitPoint = hits[i].point,
                        HitDirection = direction,
                        BaseDamage = baseDamage,
                        WasHeadshot = hitbox != null && hitbox.Zone == HitZone.Head
                    };
                    victim.TakeDamage(info);
                    CreateImpact(hits[i].point, hits[i].normal);
                    break;
                }

                if (hits[i].collider.isTrigger)
                {
                    continue;
                }

                CreateImpact(hits[i].point, hits[i].normal);
                break;
            }
        }

        private void BuildViewModel(WeaponData data)
        {
            if (!controlledByPlayer || viewModelRoot == null)
            {
                return;
            }

            if (viewModel != null)
            {
                Destroy(viewModel);
            }

            viewModel = new GameObject(data.DisplayName + " ViewModel");
            viewModel.transform.SetParent(viewModelRoot, false);
            viewModel.transform.localPosition = new Vector3(0.28f, -0.22f, 0.62f);
            viewModel.transform.localRotation = Quaternion.identity;

            Color color = data.Category == WeaponCategory.Pistol ? new Color(0.12f, 0.12f, 0.14f) : new Color(0.08f, 0.1f, 0.11f);
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;

            CreatePart("Body", viewModel.transform, new Vector3(0.18f, 0.12f, 0.44f), Vector3.zero, material);
            CreatePart("Barrel", viewModel.transform, new Vector3(0.07f, 0.07f, 0.36f), new Vector3(0f, 0.02f, 0.35f), material);
            CreatePart("Grip", viewModel.transform, new Vector3(0.1f, 0.22f, 0.09f), new Vector3(0f, -0.17f, -0.08f), material);

            if (data.Slot == WeaponSlot.Primary)
            {
                CreatePart("Stock", viewModel.transform, new Vector3(0.15f, 0.12f, 0.24f), new Vector3(0f, 0f, -0.32f), material);
                CreatePart("Magazine", viewModel.transform, new Vector3(0.1f, 0.24f, 0.11f), new Vector3(0f, -0.18f, 0.06f), material);
            }
        }

        private static void CreatePart(string name, Transform parent, Vector3 scale, Vector3 localPosition, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localScale = scale;
            part.transform.localPosition = localPosition;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void CreateImpact(Vector3 point, Vector3 normal)
        {
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = "Bullet Impact";
            impact.transform.position = point + normal * 0.01f;
            impact.transform.localScale = Vector3.one * 0.06f;
            Renderer renderer = impact.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.72f, 0.22f);
            }

            Destroy(impact.GetComponent<Collider>());
            Destroy(impact, 1.2f);
        }

        private void DrawTracer(Vector3 from, Vector3 to)
        {
            if (tracer == null)
            {
                GameObject go = new GameObject("Weapon Tracer");
                go.transform.SetParent(transform, false);
                tracer = go.AddComponent<LineRenderer>();
                tracer.positionCount = 2;
                tracer.startWidth = 0.018f;
                tracer.endWidth = 0.002f;
                tracer.material = new Material(Shader.Find("Sprites/Default"));
                tracer.startColor = new Color(1f, 0.75f, 0.25f, 0.75f);
                tracer.endColor = new Color(1f, 0.75f, 0.25f, 0f);
            }

            tracer.enabled = true;
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, to);
            CancelInvoke("HideTracer");
            Invoke("HideTracer", 0.035f);
        }

        private void HideTracer()
        {
            if (tracer != null)
            {
                tracer.enabled = false;
            }
        }
    }
}
