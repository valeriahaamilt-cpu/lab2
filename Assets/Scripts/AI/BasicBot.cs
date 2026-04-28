using UnityEngine;

namespace ProjectBreachpoint
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class BasicBot : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.8f;
        [SerializeField] private float reactionTime = 0.25f;
        [SerializeField] private float aimError = 0.035f;
        [SerializeField] private float sightRange = 42f;
        [SerializeField] private int botIndex;

        private PlayerHealth health;
        private CharacterController controller;
        private WeaponController weaponController;
        private RoundManager roundManager;
        private BombManager bombManager;
        private IronworksMapGenerator map;
        private EconomyManager economy;
        private WeaponDatabase weapons;
        private PlayerHealth target;
        private Vector3 objective;
        private float verticalVelocity;
        private float nextThinkTime;
        private float firstSawTargetTime;
        private int lastBuyRound = -1;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            controller = GetComponent<CharacterController>();
            weaponController = GetComponent<WeaponController>();
        }

        private void Update()
        {
            if (health == null || !health.IsAlive || roundManager == null)
            {
                return;
            }

            if (roundManager.CurrentPhase == RoundPhase.BuyPhase)
            {
                BuyForRound();
                return;
            }

            if (roundManager.CurrentPhase != RoundPhase.Live && roundManager.CurrentPhase != RoundPhase.BombPlanted)
            {
                return;
            }

            Think();
            Act();
        }

        public void Setup(int index, RoundManager round, BombManager bomb, IronworksMapGenerator generatedMap, EconomyManager economyManager, WeaponDatabase database)
        {
            botIndex = index;
            roundManager = round;
            bombManager = bomb;
            map = generatedMap;
            economy = economyManager;
            weapons = database;
        }

        private void BuyForRound()
        {
            if (lastBuyRound == roundManager.RoundNumber)
            {
                return;
            }

            lastBuyRound = roundManager.RoundNumber;
            if (health.Team == Team.Defenders)
            {
                economy.TryBuyDefuseKit(health);
            }

            economy.TryBuyLightArmor(health);
            WeaponData rifle = botIndex % 2 == 0 ? weapons.GetWeapon("dmr22_guardian") : weapons.GetWeapon("vgr17_vanguard");
            economy.TryBuyWeapon(health, rifle);
        }

        private void Think()
        {
            if (Time.time < nextThinkTime)
            {
                return;
            }

            nextThinkTime = Time.time + 0.12f;
            PlayerHealth newTarget = FindVisibleEnemy();
            if (newTarget != target)
            {
                target = newTarget;
                firstSawTargetTime = Time.time;
            }

            objective = ChooseObjective();
        }

        private void Act()
        {
            if (target != null && target.IsAlive)
            {
                AimAt(target.transform.position + Vector3.up * 1.25f);
                if (Time.time - firstSawTargetTime >= reactionTime)
                {
                    weaponController.BotFireAt(target.transform.position + Vector3.up * 1.25f, aimError);
                }

                MoveTowards(transform.position - transform.forward * 0.2f);
                return;
            }

            if (health.Team == Team.Attackers)
            {
                if (bombManager.State == BombState.Dropped)
                {
                    MoveTowards(bombManager.BombWorldPosition);
                    if (Vector3.Distance(transform.position, bombManager.BombWorldPosition) < 2f)
                    {
                        bombManager.TryPickup(health);
                    }
                    return;
                }

                if (bombManager.IsCarrier(health) && bombManager.GetSiteForPosition(transform.position) != null)
                {
                    AimAt(transform.position + transform.forward);
                    if (bombManager.TryBotPlant(health))
                    {
                        return;
                    }
                }
            }
            else if (roundManager.CurrentPhase == RoundPhase.BombPlanted)
            {
                MoveTowards(bombManager.BombWorldPosition);
                if (Vector3.Distance(transform.position, bombManager.BombWorldPosition) < 2.1f)
                {
                    bombManager.TryBotDefuse(health);
                }
                return;
            }

            MoveTowards(objective);
        }

        private Vector3 ChooseObjective()
        {
            if (roundManager.CurrentPhase == RoundPhase.BombPlanted)
            {
                if (health.Team == Team.Attackers)
                {
                    return map.GetCoverPoint(botIndex).position;
                }

                return bombManager.BombWorldPosition;
            }

            return map.GetObjectiveTarget(health.Team, botIndex).position;
        }

        private PlayerHealth FindVisibleEnemy()
        {
            PlayerHealth best = null;
            float bestDistance = sightRange;
            for (int i = 0; i < roundManager.Players.Count; i++)
            {
                PlayerHealth candidate = roundManager.Players[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team == health.Team)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < bestDistance && HasLineOfSight(candidate))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private bool HasLineOfSight(PlayerHealth candidate)
        {
            Vector3 from = transform.position + Vector3.up * 1.45f;
            Vector3 to = candidate.transform.position + Vector3.up * 1.2f;
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            RaycastHit[] hits = Physics.RaycastAll(from, direction.normalized, distance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                PlayerHealth hitPlayer = hits[i].collider.GetComponentInParent<PlayerHealth>();
                if (hitPlayer == health)
                {
                    continue;
                }

                return hitPlayer == candidate;
            }

            return true;
        }

        private void MoveTowards(Vector3 destination)
        {
            Vector3 flat = destination - transform.position;
            flat.y = 0f;

            if (flat.sqrMagnitude > 0.25f)
            {
                Vector3 direction = flat.normalized;
                controller.Move(direction * moveSpeed * Time.deltaTime);
                AimAt(transform.position + direction);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += -18f * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void AimAt(Vector3 point)
        {
            Vector3 flat = point - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized), Time.deltaTime * 10f);
            }
        }
    }
}
