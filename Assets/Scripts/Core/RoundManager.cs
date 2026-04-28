using System.Collections.Generic;
using UnityEngine;

namespace ProjectBreachpoint
{
    public sealed class RoundManager : MonoBehaviour
    {
        [Header("Timers")]
        [SerializeField] private float buyPhaseDuration = 15f;
        [SerializeField] private float roundDuration = 115f;
        [SerializeField] private float roundEndDelay = 5f;

        private readonly List<PlayerHealth> players = new List<PlayerHealth>();
        private IronworksMapGenerator map;
        private BombManager bombManager;
        private EconomyManager economyManager;
        private WeaponDatabase weaponDatabase;
        private float timer;
        private bool sideSwitched;

        public RoundPhase CurrentPhase { get; private set; }
        public float PhaseTimeRemaining { get { return timer; } }
        public int RoundNumber { get; private set; }
        public int AttackersScore { get; private set; }
        public int DefendersScore { get; private set; }
        public Team LastRoundWinner { get; private set; }
        public WinReason LastWinReason { get; private set; }
        public bool MatchComplete { get; private set; }

        public IReadOnlyList<PlayerHealth> Players
        {
            get { return players; }
        }

        private void Update()
        {
            if (CurrentPhase == RoundPhase.Warmup || players.Count == 0)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (CurrentPhase == RoundPhase.BuyPhase && timer <= 0f)
            {
                BeginLiveRound();
            }
            else if (CurrentPhase == RoundPhase.Live)
            {
                CheckLiveWinConditions();
                if (timer <= 0f && CurrentPhase == RoundPhase.Live)
                {
                    EndRound(Team.Defenders, WinReason.TimerExpired);
                }
            }
            else if (CurrentPhase == RoundPhase.BombPlanted)
            {
                CheckPostPlantWinConditions();
            }
            else if (CurrentPhase == RoundPhase.RoundEnd && timer <= 0f && !MatchComplete)
            {
                BeginRound();
            }
        }

        public void StartMatch(List<PlayerHealth> matchPlayers, IronworksMapGenerator generatedMap, BombManager bomb, EconomyManager economy, WeaponDatabase database)
        {
            players.Clear();
            players.AddRange(matchPlayers);
            map = generatedMap;
            bombManager = bomb;
            economyManager = economy;
            weaponDatabase = database;
            MatchComplete = false;
            RoundNumber = 0;
            AttackersScore = 0;
            DefendersScore = 0;
            sideSwitched = false;

            for (int i = 0; i < players.Count; i++)
            {
                economyManager.RegisterPlayer(players[i]);
            }

            CurrentPhase = RoundPhase.Warmup;
            BeginRound();
        }

        public void NotifyBombPlanted()
        {
            if (CurrentPhase == RoundPhase.Live)
            {
                CurrentPhase = RoundPhase.BombPlanted;
                timer = bombManager.BombTimeRemaining;
            }
        }

        public void NotifyBombExploded()
        {
            EndRound(Team.Attackers, WinReason.BombExploded);
        }

        public void NotifyBombDefused(PlayerHealth defuser)
        {
            if (economyManager != null)
            {
                economyManager.AwardDefuse(defuser);
            }

            EndRound(Team.Defenders, WinReason.BombDefused);
        }

        public string GetPhaseText()
        {
            if (CurrentPhase == RoundPhase.BuyPhase)
            {
                return "BUY PHASE";
            }

            if (CurrentPhase == RoundPhase.Live)
            {
                return "LIVE";
            }

            if (CurrentPhase == RoundPhase.BombPlanted)
            {
                return "BOMB PLANTED";
            }

            if (CurrentPhase == RoundPhase.RoundEnd)
            {
                if (MatchComplete)
                {
                    return "MATCH COMPLETE: " + LastRoundWinner + " win";
                }

                return LastRoundWinner + " win: " + LastWinReason;
            }

            return "WARMUP";
        }

        private void BeginRound()
        {
            RoundNumber++;
            if (!sideSwitched && RoundNumber == 13)
            {
                SwitchSides();
            }

            CurrentPhase = RoundPhase.BuyPhase;
            timer = buyPhaseDuration;
            LastWinReason = WinReason.None;

            ResetPlayers();
            if (bombManager != null)
            {
                bombManager.ResetBombForRound(ChooseBombCarrier());
            }
        }

        private void BeginLiveRound()
        {
            CurrentPhase = RoundPhase.Live;
            timer = roundDuration;
        }

        private void EndRound(Team winner, WinReason reason)
        {
            if (CurrentPhase == RoundPhase.RoundEnd)
            {
                return;
            }

            CurrentPhase = RoundPhase.RoundEnd;
            timer = roundEndDelay;
            LastRoundWinner = winner;
            LastWinReason = reason;

            if (winner == Team.Attackers)
            {
                AttackersScore++;
            }
            else
            {
                DefendersScore++;
            }

            if (economyManager != null)
            {
                economyManager.AwardRoundEnd(winner, reason);
            }

            if (AttackersScore >= 13 || DefendersScore >= 13)
            {
                MatchComplete = true;
                timer = 99999f;
            }
        }

        private void ResetPlayers()
        {
            int attackerIndex = 0;
            int defenderIndex = 0;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerHealth player = players[i];
                Transform spawn = player.Team == Team.Attackers
                    ? map.GetAttackerSpawn(attackerIndex++)
                    : map.GetDefenderSpawn(defenderIndex++);

                player.ResetForRound(spawn.position, spawn.rotation);
                EnsureStarterWeapons(player);
            }
        }

        private void EnsureStarterWeapons(PlayerHealth player)
        {
            WeaponController controller = player.GetComponent<WeaponController>();
            if (controller == null || weaponDatabase == null)
            {
                return;
            }

            if (!controller.HasSlot(WeaponSlot.Pistol))
            {
                controller.EquipWeapon(weaponDatabase.GetStarterPistol());
            }

            if (!controller.HasSlot(WeaponSlot.Knife))
            {
                controller.EquipWeapon(weaponDatabase.GetWeapon("edge_knife"));
            }

            controller.EquipSlot(controller.HasSlot(WeaponSlot.Primary) ? WeaponSlot.Primary : WeaponSlot.Pistol);
        }

        private PlayerHealth ChooseBombCarrier()
        {
            PlayerHealth humanAttacker = null;
            PlayerHealth fallback = null;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Team != Team.Attackers)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = players[i];
                }

                if (players[i].IsHumanPlayer)
                {
                    humanAttacker = players[i];
                }
            }

            return humanAttacker != null ? humanAttacker : fallback;
        }

        private void CheckLiveWinConditions()
        {
            int aliveAttackers = CountAlive(Team.Attackers);
            int aliveDefenders = CountAlive(Team.Defenders);

            if (aliveDefenders == 0)
            {
                EndRound(Team.Attackers, WinReason.DefendersEliminated);
            }
            else if (aliveAttackers == 0)
            {
                EndRound(Team.Defenders, WinReason.AttackersEliminated);
            }
        }

        private void CheckPostPlantWinConditions()
        {
            if (CountAlive(Team.Defenders) == 0)
            {
                EndRound(Team.Attackers, WinReason.DefendersEliminated);
            }
        }

        private int CountAlive(Team team)
        {
            int count = 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Team == team && players[i].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private void SwitchSides()
        {
            sideSwitched = true;
            for (int i = 0; i < players.Count; i++)
            {
                players[i].Team = players[i].Team.Opposite();
            }
        }
    }
}
