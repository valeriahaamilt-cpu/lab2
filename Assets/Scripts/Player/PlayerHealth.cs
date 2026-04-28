using System;
using UnityEngine;

namespace ProjectBreachpoint
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        public static event Action<PlayerHealth, DamageInfo> AnyPlayerDied;
        public static event Action<PlayerHealth, DamageInfo, int> AnyPlayerDamaged;

        [Header("Identity")]
        [SerializeField] private string displayName = "Operator";
        [SerializeField] private Team team = Team.Attackers;
        [SerializeField] private bool isHumanPlayer;

        [Header("Vitals")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int health = 100;

        [Header("Equipment")]
        public int Armor;
        [SerializeField] private bool hasHelmet;
        [SerializeField] private bool hasDefuseKit;

        private WeaponController weaponController;
        private CharacterController characterController;
        private FirstPersonController firstPersonController;

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        public Team Team
        {
            get { return team; }
            set { team = value; }
        }

        public bool IsHumanPlayer
        {
            get { return isHumanPlayer; }
            set { isHumanPlayer = value; }
        }

        public int Health
        {
            get { return health; }
        }

        public bool IsAlive
        {
            get { return health > 0; }
        }

        public bool HasHelmet
        {
            get { return hasHelmet; }
            set { hasHelmet = value; }
        }

        public bool HasDefuseKit
        {
            get { return hasDefuseKit; }
            set { hasDefuseKit = value; }
        }

        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Money { get; set; }
        public bool SurvivedPreviousRound { get; set; }

        private void Awake()
        {
            CacheComponents();
            Money = 800;
        }

        public void ResetForRound(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            CacheComponents();
            gameObject.SetActive(true);
            health = maxHealth;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = spawnPosition;
                transform.rotation = spawnRotation;
                characterController.enabled = true;
            }

            if (firstPersonController != null)
            {
                firstPersonController.enabled = true;
            }

            if (weaponController != null)
            {
                weaponController.enabled = true;
            }

            if (!SurvivedPreviousRound)
            {
                Armor = 0;
                hasHelmet = false;
                hasDefuseKit = false;
            }

            SurvivedPreviousRound = true;
        }

        public void GiveArmor(bool helmet)
        {
            Armor = 100;
            hasHelmet = helmet;
        }

        public void GiveDefuseKit()
        {
            hasDefuseKit = true;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive)
            {
                return;
            }

            if (info.Attacker != null && info.Attacker.Team == team && info.Attacker != this)
            {
                return;
            }

            info.Victim = this;
            int finalDamage = DamageCalculator.CalculateDamage(info, this);
            health = Mathf.Max(0, health - finalDamage);
            AnyPlayerDamaged?.Invoke(this, info, finalDamage);

            if (health <= 0)
            {
                Die(info);
            }
        }

        private void Die(DamageInfo info)
        {
            CacheComponents();
            Deaths++;
            SurvivedPreviousRound = false;
            if (info.Attacker != null && info.Attacker != this)
            {
                info.Attacker.Kills++;
            }

            if (weaponController != null)
            {
                weaponController.OnOwnerDied();
            }

            BombManager bombManager = FindObjectOfType<BombManager>();
            if (bombManager != null)
            {
                bombManager.NotifyCarrierDied(this);
            }

            AnyPlayerDied?.Invoke(this, info);

            if (isHumanPlayer)
            {
                if (characterController != null)
                {
                    characterController.enabled = false;
                }

                if (firstPersonController != null)
                {
                    firstPersonController.enabled = false;
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void CacheComponents()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController>();
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (firstPersonController == null)
            {
                firstPersonController = GetComponent<FirstPersonController>();
            }
        }
    }
}
