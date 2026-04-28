using System.Collections.Generic;
using UnityEngine;

namespace ProjectBreachpoint
{
    public sealed class BombSite : MonoBehaviour
    {
        [SerializeField] private string siteName = "A";
        private BoxCollider boxCollider;

        public string SiteName
        {
            get { return siteName; }
            set { siteName = value; }
        }

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
            }

            return boxCollider != null && boxCollider.bounds.Contains(worldPosition);
        }
    }

    public sealed class BombManager : MonoBehaviour
    {
        [Header("Bomb Timings")]
        [SerializeField] private float plantDuration = 4f;
        [SerializeField] private float defuseDuration = 10f;
        [SerializeField] private float defuseKitDuration = 5f;
        [SerializeField] private float bombDuration = 40f;

        private readonly List<BombSite> sites = new List<BombSite>();
        private GameObject bombObject;
        private RoundManager roundManager;
        private EconomyManager economyManager;
        private PlayerHealth activeInteractor;
        private BombSite activeSite;
        private Vector3 actionStartPosition;
        private float actionProgress;
        private float lastActionRequestTime;
        private bool actionIsDefuse;

        public BombState State { get; private set; }
        public PlayerHealth Carrier { get; private set; }
        public float BombTimeRemaining { get; private set; }
        public string CurrentSiteName { get; private set; }
        public Vector3 BombWorldPosition { get; private set; }

        public float PlantProgress
        {
            get { return !actionIsDefuse && activeInteractor != null ? Mathf.Clamp01(actionProgress / plantDuration) : 0f; }
        }

        public float DefuseProgress
        {
            get
            {
                if (actionIsDefuse && activeInteractor != null)
                {
                    float duration = activeInteractor.HasDefuseKit ? defuseKitDuration : defuseDuration;
                    return Mathf.Clamp01(actionProgress / duration);
                }

                return 0f;
            }
        }

        private void Awake()
        {
            roundManager = FindObjectOfType<RoundManager>();
            economyManager = FindObjectOfType<EconomyManager>();
            CreateBombObject();
        }

        private void OnEnable()
        {
            PlayerHealth.AnyPlayerDamaged += HandlePlayerDamaged;
        }

        private void OnDisable()
        {
            PlayerHealth.AnyPlayerDamaged -= HandlePlayerDamaged;
        }

        private void Update()
        {
            UpdateCarriedVisual();
            HandleHumanInput();
            UpdateHeldAction();
            UpdateBombTimer();
        }

        public void Initialize(RoundManager round, EconomyManager economy)
        {
            roundManager = round;
            economyManager = economy;
        }

        public void RegisterSite(BombSite site)
        {
            if (site != null && !sites.Contains(site))
            {
                sites.Add(site);
            }
        }

        public void ResetBombForRound(PlayerHealth carrier)
        {
            CancelAction();
            Carrier = carrier;
            State = BombState.Carried;
            BombTimeRemaining = bombDuration;
            CurrentSiteName = string.Empty;

            if (bombObject != null)
            {
                bombObject.SetActive(false);
                bombObject.transform.SetParent(null);
            }
        }

        public void NotifyCarrierDied(PlayerHealth deadPlayer)
        {
            if (Carrier == deadPlayer)
            {
                DropBomb(deadPlayer.transform.position + Vector3.up * 0.35f);
            }

            if (activeInteractor == deadPlayer)
            {
                CancelAction();
            }
        }

        public bool TryBotPlant(PlayerHealth actor)
        {
            return ContinuePlant(actor);
        }

        public bool TryBotDefuse(PlayerHealth actor)
        {
            return ContinueDefuse(actor);
        }

        public bool IsCarrier(PlayerHealth player)
        {
            return Carrier == player && State == BombState.Carried;
        }

        public bool TryPickup(PlayerHealth player)
        {
            if (State != BombState.Dropped || player == null || !player.IsAlive || Vector3.Distance(player.transform.position, BombWorldPosition) > 2f)
            {
                return false;
            }

            PickUpBomb(player);
            return true;
        }

        public BombSite GetSiteForPosition(Vector3 position)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i].Contains(position))
                {
                    return sites[i];
                }
            }

            return null;
        }

        private void HandleHumanInput()
        {
            PlayerHealth human = FindHumanPlayer();
            if (human == null || !human.IsAlive || roundManager == null || roundManager.CurrentPhase == RoundPhase.RoundEnd)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.G) && Carrier == human && State == BombState.Carried)
            {
                DropBomb(human.transform.position + human.transform.forward * 0.8f + Vector3.up * 0.35f);
            }

            if (Input.GetKey(KeyCode.E))
            {
                if (State == BombState.Carried && Carrier == human)
                {
                    ContinuePlant(human);
                }
                else if (State == BombState.Planted || State == BombState.Defusing)
                {
                    ContinueDefuse(human);
                }
                else if (State == BombState.Dropped && Vector3.Distance(human.transform.position, BombWorldPosition) < 2f)
                {
                    PickUpBomb(human);
                }
            }
        }

        private bool ContinuePlant(PlayerHealth actor)
        {
            if (actor == null || !actor.IsAlive || actor.Team != Team.Attackers || Carrier != actor || roundManager.CurrentPhase != RoundPhase.Live)
            {
                return false;
            }

            BombSite site = GetSiteForPosition(actor.transform.position);
            if (site == null)
            {
                return false;
            }

            BeginOrContinueAction(actor, site, false);
            return true;
        }

        private bool ContinueDefuse(PlayerHealth actor)
        {
            if (actor == null || !actor.IsAlive || actor.Team != Team.Defenders || (State != BombState.Planted && State != BombState.Defusing))
            {
                return false;
            }

            if (Vector3.Distance(actor.transform.position, BombWorldPosition) > 2.4f)
            {
                return false;
            }

            BeginOrContinueAction(actor, null, true);
            return true;
        }

        private void BeginOrContinueAction(PlayerHealth actor, BombSite site, bool defuse)
        {
            if (activeInteractor != actor || actionIsDefuse != defuse)
            {
                activeInteractor = actor;
                activeSite = site;
                actionStartPosition = actor.transform.position;
                actionProgress = 0f;
                actionIsDefuse = defuse;
                State = defuse ? BombState.Defusing : BombState.Planting;
            }

            lastActionRequestTime = Time.time;
        }

        private void UpdateHeldAction()
        {
            if (activeInteractor == null)
            {
                return;
            }

            if (!activeInteractor.IsAlive || Time.time - lastActionRequestTime > 0.16f)
            {
                CancelAction();
                return;
            }

            if (Vector3.Distance(activeInteractor.transform.position, actionStartPosition) > 1.6f)
            {
                CancelAction();
                return;
            }

            if (!actionIsDefuse)
            {
                if (Carrier != activeInteractor || activeSite == null || !activeSite.Contains(activeInteractor.transform.position))
                {
                    CancelAction();
                    return;
                }

                actionProgress += Time.deltaTime;
                if (actionProgress >= plantDuration)
                {
                    CompletePlant();
                }
            }
            else
            {
                if (State != BombState.Defusing || Vector3.Distance(activeInteractor.transform.position, BombWorldPosition) > 2.4f)
                {
                    CancelAction();
                    return;
                }

                float duration = activeInteractor.HasDefuseKit ? defuseKitDuration : defuseDuration;
                actionProgress += Time.deltaTime;
                if (actionProgress >= duration)
                {
                    CompleteDefuse();
                }
            }
        }

        private void CompletePlant()
        {
            State = BombState.Planted;
            CurrentSiteName = activeSite != null ? activeSite.SiteName : "?";
            Carrier = null;
            BombTimeRemaining = bombDuration;
            BombWorldPosition = activeInteractor.transform.position + activeInteractor.transform.forward * 0.35f;

            bombObject.SetActive(true);
            bombObject.transform.SetParent(null);
            bombObject.transform.position = BombWorldPosition;
            bombObject.transform.rotation = Quaternion.identity;

            activeInteractor = null;
            actionProgress = 0f;

            if (economyManager != null)
            {
                economyManager.AwardBombPlantBonus();
            }

            if (roundManager != null)
            {
                roundManager.NotifyBombPlanted();
            }
        }

        private void CompleteDefuse()
        {
            PlayerHealth defuser = activeInteractor;
            State = BombState.Defused;
            activeInteractor = null;
            actionProgress = 0f;
            if (bombObject != null)
            {
                bombObject.SetActive(false);
            }

            if (roundManager != null)
            {
                roundManager.NotifyBombDefused(defuser);
            }
        }

        private void UpdateBombTimer()
        {
            if (State != BombState.Planted && State != BombState.Defusing)
            {
                return;
            }

            BombTimeRemaining -= Time.deltaTime;
            if (BombTimeRemaining <= 0f)
            {
                State = BombState.Exploded;
                SpawnExplosionMarker();
                if (roundManager != null)
                {
                    roundManager.NotifyBombExploded();
                }
            }
        }

        private void DropBomb(Vector3 position)
        {
            if (bombObject == null)
            {
                CreateBombObject();
            }

            State = BombState.Dropped;
            BombWorldPosition = position;
            Carrier = null;
            bombObject.SetActive(true);
            bombObject.transform.SetParent(null);
            bombObject.transform.position = position;
            bombObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void PickUpBomb(PlayerHealth player)
        {
            if (player.Team != Team.Attackers)
            {
                return;
            }

            Carrier = player;
            State = BombState.Carried;
            bombObject.SetActive(false);
        }

        private void CancelAction()
        {
            if (State == BombState.Planting)
            {
                State = Carrier != null ? BombState.Carried : BombState.Dropped;
            }
            else if (State == BombState.Defusing)
            {
                State = BombState.Planted;
            }

            activeInteractor = null;
            activeSite = null;
            actionProgress = 0f;
            actionIsDefuse = false;
        }

        private void HandlePlayerDamaged(PlayerHealth player, DamageInfo info, int damage)
        {
            if (player == activeInteractor)
            {
                CancelAction();
            }
        }

        private void UpdateCarriedVisual()
        {
            if (State == BombState.Carried && Carrier != null)
            {
                BombWorldPosition = Carrier.transform.position;
            }
        }

        private PlayerHealth FindHumanPlayer()
        {
            PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].IsHumanPlayer)
                {
                    return players[i];
                }
            }

            return null;
        }

        private void CreateBombObject()
        {
            if (bombObject != null)
            {
                return;
            }

            bombObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bombObject.name = "Breach Charge";
            bombObject.transform.localScale = new Vector3(0.35f, 0.12f, 0.35f);
            Renderer renderer = bombObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.1f, 0.11f, 0.12f);
            }

            bombObject.SetActive(false);
        }

        private void SpawnExplosionMarker()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Bomb Explosion Marker";
            marker.transform.position = BombWorldPosition + Vector3.up * 1.5f;
            marker.transform.localScale = Vector3.one * 6f;
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.32f, 0.08f, 0.45f);
            }

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Destroy(marker, 2f);
        }
    }
}
