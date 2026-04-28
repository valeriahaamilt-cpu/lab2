using System.Collections.Generic;
using UnityEngine;

namespace ProjectBreachpoint
{
    [System.Serializable]
    public sealed class WeaponData
    {
        public string Id;
        public string DisplayName;
        public WeaponCategory Category;
        public WeaponSlot Slot;
        public FireMode FireMode;
        public int Price;
        public int MagazineSize;
        public int ReserveAmmo;
        public float FireRate;
        public float ReloadTime;
        public float EquipTime;
        public float BaseDamage;
        public float ArmorPenetration;
        public float HeadMultiplier = 4f;
        public float ChestMultiplier = 1f;
        public float StomachMultiplier = 1.15f;
        public float ArmMultiplier = 0.85f;
        public float LegMultiplier = 0.75f;
        public float Range = 120f;
        public float DamageFalloff = 0.18f;
        public float FirstShotAccuracy = 0.98f;
        public float BaseSpread = 0.015f;
        public float MovementSpreadPenalty = 0.08f;
        public float JumpSpreadPenalty = 0.18f;
        public float CrouchAccuracyBonus = 0.35f;
        public float RecoilRecoverySpeed = 9f;
        public float RecoilResetDelay = 0.28f;
        public float CameraKick = 1f;
        public int Pellets = 1;
        public int KillReward = 300;
        public string Description;
        public Vector2[] RecoilPattern;
    }

    public sealed class WeaponDatabase : MonoBehaviour
    {
        private readonly List<WeaponData> weapons = new List<WeaponData>();
        private readonly Dictionary<string, WeaponData> byId = new Dictionary<string, WeaponData>();

        public IReadOnlyList<WeaponData> Weapons
        {
            get { return weapons; }
        }

        private void Awake()
        {
            if (weapons.Count == 0)
            {
                LoadDefaultWeapons();
            }
        }

        public WeaponData GetWeapon(string id)
        {
            WeaponData weapon;
            return byId.TryGetValue(id, out weapon) ? weapon : null;
        }

        public WeaponData GetStarterPistol()
        {
            return GetWeapon("px9_sidearm");
        }

        public WeaponData GetDefaultRifle()
        {
            return GetWeapon("vgr17_vanguard");
        }

        public void LoadDefaultWeapons()
        {
            weapons.Clear();
            byId.Clear();

            Add(new WeaponData
            {
                Id = "px9_sidearm",
                DisplayName = "PX-9 Sidearm",
                Category = WeaponCategory.Pistol,
                Slot = WeaponSlot.Pistol,
                FireMode = FireMode.SemiAuto,
                Price = 0,
                MagazineSize = 12,
                ReserveAmmo = 48,
                FireRate = 5.2f,
                ReloadTime = 1.45f,
                EquipTime = 0.35f,
                BaseDamage = 29f,
                ArmorPenetration = 0.42f,
                FirstShotAccuracy = 0.94f,
                BaseSpread = 0.011f,
                MovementSpreadPenalty = 0.045f,
                JumpSpreadPenalty = 0.12f,
                RecoilRecoverySpeed = 12f,
                RecoilResetDelay = 0.23f,
                CameraKick = 0.75f,
                KillReward = 300,
                Description = "Balanced starter sidearm with crisp first-shot accuracy.",
                RecoilPattern = Pattern(0.5f, 0.15f, -0.1f, 0.25f, -0.2f, 0.2f)
            });

            Add(new WeaponData
            {
                Id = "raptor45",
                DisplayName = "Raptor-45",
                Category = WeaponCategory.Pistol,
                Slot = WeaponSlot.Pistol,
                FireMode = FireMode.SemiAuto,
                Price = 700,
                MagazineSize = 7,
                ReserveAmmo = 35,
                FireRate = 2.4f,
                ReloadTime = 1.75f,
                EquipTime = 0.42f,
                BaseDamage = 54f,
                ArmorPenetration = 0.62f,
                FirstShotAccuracy = 0.9f,
                BaseSpread = 0.018f,
                MovementSpreadPenalty = 0.065f,
                JumpSpreadPenalty = 0.16f,
                RecoilRecoverySpeed = 8f,
                RecoilResetDelay = 0.42f,
                CameraKick = 1.35f,
                KillReward = 300,
                Description = "Hard-hitting precision pistol with punishing recoil.",
                RecoilPattern = Pattern(1.0f, 0.25f, -0.2f, 0.35f, -0.35f, 0.25f)
            });

            Add(new WeaponData
            {
                Id = "vgr17_vanguard",
                DisplayName = "VGR-17 Vanguard",
                Category = WeaponCategory.Rifle,
                Slot = WeaponSlot.Primary,
                FireMode = FireMode.Automatic,
                Price = 2900,
                MagazineSize = 30,
                ReserveAmmo = 90,
                FireRate = 9.2f,
                ReloadTime = 2.25f,
                EquipTime = 0.75f,
                BaseDamage = 36f,
                ArmorPenetration = 0.78f,
                FirstShotAccuracy = 0.91f,
                BaseSpread = 0.014f,
                MovementSpreadPenalty = 0.09f,
                JumpSpreadPenalty = 0.22f,
                RecoilRecoverySpeed = 7.5f,
                RecoilResetDelay = 0.32f,
                CameraKick = 1.05f,
                KillReward = 300,
                Description = "Power rifle with strong armor penetration and a learnable climb.",
                RecoilPattern = Pattern(0.75f, 0.08f, 0.9f, -0.04f, 1.1f, 0.1f, 1.2f, 0.18f, 1.05f, -0.26f, 0.85f, -0.35f, 0.75f, 0.42f, 0.65f, 0.5f, 0.55f, -0.48f, 0.45f, -0.54f)
            });

            Add(new WeaponData
            {
                Id = "dmr22_guardian",
                DisplayName = "DMR-22 Guardian",
                Category = WeaponCategory.Rifle,
                Slot = WeaponSlot.Primary,
                FireMode = FireMode.Automatic,
                Price = 3100,
                MagazineSize = 25,
                ReserveAmmo = 75,
                FireRate = 8.1f,
                ReloadTime = 2.1f,
                EquipTime = 0.7f,
                BaseDamage = 33f,
                ArmorPenetration = 0.74f,
                FirstShotAccuracy = 0.96f,
                BaseSpread = 0.009f,
                MovementSpreadPenalty = 0.075f,
                JumpSpreadPenalty = 0.2f,
                RecoilRecoverySpeed = 8.5f,
                RecoilResetDelay = 0.27f,
                CameraKick = 0.82f,
                KillReward = 300,
                Description = "Stable defender rifle with clean first-shot accuracy.",
                RecoilPattern = Pattern(0.55f, -0.04f, 0.68f, 0.02f, 0.78f, -0.12f, 0.82f, 0.18f, 0.65f, 0.25f, 0.48f, -0.26f)
            });

            Add(new WeaponData
            {
                Id = "edge_knife",
                DisplayName = "Edge Knife",
                Category = WeaponCategory.Knife,
                Slot = WeaponSlot.Knife,
                FireMode = FireMode.SemiAuto,
                Price = 0,
                MagazineSize = 1,
                ReserveAmmo = 0,
                FireRate = 1.4f,
                ReloadTime = 0f,
                EquipTime = 0.25f,
                BaseDamage = 55f,
                ArmorPenetration = 1f,
                Range = 2.2f,
                BaseSpread = 0f,
                KillReward = 1500,
                Description = "Silent close-range fallback blade.",
                RecoilPattern = Pattern(0f, 0f)
            });
        }

        private void Add(WeaponData weapon)
        {
            weapons.Add(weapon);
            byId[weapon.Id] = weapon;
        }

        private static Vector2[] Pattern(params float[] values)
        {
            int count = Mathf.Max(1, values.Length / 2);
            Vector2[] pattern = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                pattern[i] = new Vector2(values[i * 2], values[i * 2 + 1]);
            }

            return pattern;
        }
    }
}
