using UnityEngine;

namespace ProjectBreachpoint
{
    public enum Team
    {
        Attackers,
        Defenders
    }

    public enum RoundPhase
    {
        Warmup,
        BuyPhase,
        Live,
        BombPlanted,
        RoundEnd
    }

    public enum WinReason
    {
        None,
        AttackersEliminated,
        DefendersEliminated,
        TimerExpired,
        BombExploded,
        BombDefused
    }

    public enum WeaponCategory
    {
        Pistol,
        Rifle,
        Knife
    }

    public enum WeaponSlot
    {
        Primary,
        Pistol,
        Knife
    }

    public enum FireMode
    {
        SemiAuto,
        Automatic
    }

    public enum HitZone
    {
        Head,
        Chest,
        Stomach,
        Arms,
        Legs
    }

    public enum BombState
    {
        Carried,
        Dropped,
        Planting,
        Planted,
        Defusing,
        Exploded,
        Defused
    }

    public enum BuyCategory
    {
        Weapons,
        Armor,
        Equipment
    }

    public static class TeamExtensions
    {
        public static Team Opposite(this Team team)
        {
            return team == Team.Attackers ? Team.Defenders : Team.Attackers;
        }

        public static Color TeamColor(this Team team)
        {
            return team == Team.Attackers ? new Color(0.95f, 0.45f, 0.18f) : new Color(0.1f, 0.45f, 0.95f);
        }
    }
}
