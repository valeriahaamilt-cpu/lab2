using UnityEngine;

namespace ProjectBreachpoint
{
    public struct DamageInfo
    {
        public PlayerHealth Attacker;
        public PlayerHealth Victim;
        public WeaponData Weapon;
        public HitZone HitZone;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public float BaseDamage;
        public bool WasHeadshot;
    }

    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] private PlayerHealth owner;
        [SerializeField] private HitZone hitZone = HitZone.Chest;

        public PlayerHealth Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        public HitZone Zone
        {
            get { return hitZone; }
            set { hitZone = value; }
        }

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponentInParent<PlayerHealth>();
            }
        }
    }

    public static class ArmorSystem
    {
        public static float ApplyArmor(float damage, float armorPenetration, ref int armor, bool affectsArmor)
        {
            if (!affectsArmor || armor <= 0)
            {
                return damage;
            }

            float armorBlocked = Mathf.Clamp01(1f - armorPenetration);
            float prevented = damage * armorBlocked * 0.55f;
            int armorCost = Mathf.CeilToInt(prevented * 0.65f);
            armor = Mathf.Max(0, armor - armorCost);
            return Mathf.Max(1f, damage - prevented);
        }
    }

    public static class DamageCalculator
    {
        public static int CalculateDamage(DamageInfo info, PlayerHealth victim)
        {
            float multiplier = GetZoneMultiplier(info.Weapon, info.HitZone);
            float damage = info.BaseDamage * multiplier;

            bool armoredZone = info.HitZone == HitZone.Chest || info.HitZone == HitZone.Stomach || info.HitZone == HitZone.Arms;
            if (info.HitZone == HitZone.Head)
            {
                if (victim.HasHelmet)
                {
                    damage = ArmorSystem.ApplyArmor(damage, info.Weapon.ArmorPenetration, ref victim.Armor, true);
                    if (info.Weapon.BaseDamage < 85f)
                    {
                        damage *= 0.82f;
                    }
                }
            }
            else
            {
                damage = ArmorSystem.ApplyArmor(damage, info.Weapon.ArmorPenetration, ref victim.Armor, armoredZone);
            }

            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        private static float GetZoneMultiplier(WeaponData weapon, HitZone zone)
        {
            switch (zone)
            {
                case HitZone.Head:
                    return weapon.HeadMultiplier;
                case HitZone.Stomach:
                    return weapon.StomachMultiplier;
                case HitZone.Arms:
                    return weapon.ArmMultiplier;
                case HitZone.Legs:
                    return weapon.LegMultiplier;
                default:
                    return weapon.ChestMultiplier;
            }
        }
    }
}
