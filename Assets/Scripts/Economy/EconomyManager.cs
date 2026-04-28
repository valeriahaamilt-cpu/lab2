using System.Collections.Generic;
using UnityEngine;

namespace ProjectBreachpoint
{
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField] private int startingMoney = 800;
        [SerializeField] private int maxMoney = 16000;

        private readonly Dictionary<PlayerHealth, List<string>> moneyFeed = new Dictionary<PlayerHealth, List<string>>();
        private int attackerLossStreak;
        private int defenderLossStreak;
        private RoundManager roundManager;

        private void Awake()
        {
            roundManager = FindObjectOfType<RoundManager>();
        }

        private void OnEnable()
        {
            PlayerHealth.AnyPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            PlayerHealth.AnyPlayerDied -= HandlePlayerDied;
        }

        public void RegisterPlayer(PlayerHealth player)
        {
            if (player == null)
            {
                return;
            }

            if (!moneyFeed.ContainsKey(player))
            {
                if (player.Money <= 0)
                {
                    player.Money = startingMoney;
                }

                moneyFeed[player] = new List<string>();
            }
        }

        public bool CanBuy(PlayerHealth player)
        {
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }

            return player != null && player.IsAlive && roundManager != null && roundManager.CurrentPhase == RoundPhase.BuyPhase;
        }

        public bool TryBuyWeapon(PlayerHealth buyer, WeaponData weapon)
        {
            if (weapon == null || !CanBuy(buyer) || buyer.Money < weapon.Price)
            {
                return false;
            }

            WeaponController controller = buyer.GetComponent<WeaponController>();
            if (controller == null)
            {
                return false;
            }

            Spend(buyer, weapon.Price, weapon.DisplayName);
            controller.EquipWeapon(weapon);
            return true;
        }

        public bool TryBuyLightArmor(PlayerHealth buyer)
        {
            if (!CanBuy(buyer) || buyer.Money < 650)
            {
                return false;
            }

            Spend(buyer, 650, "Light Armor");
            buyer.GiveArmor(false);
            return true;
        }

        public bool TryBuyHeavyArmor(PlayerHealth buyer)
        {
            if (!CanBuy(buyer) || buyer.Money < 1000)
            {
                return false;
            }

            Spend(buyer, 1000, "Heavy Armor + Helmet");
            buyer.GiveArmor(true);
            return true;
        }

        public bool TryBuyDefuseKit(PlayerHealth buyer)
        {
            if (!CanBuy(buyer) || buyer.Team != Team.Defenders || buyer.Money < 400)
            {
                return false;
            }

            Spend(buyer, 400, "Defuse Kit");
            buyer.GiveDefuseKit();
            return true;
        }

        public void AwardBombPlantBonus()
        {
            PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].Team == Team.Attackers)
                {
                    AddMoney(players[i], 800, "Bomb Plant Bonus");
                }
            }
        }

        public void AwardDefuse(PlayerHealth defuser)
        {
            AddMoney(defuser, 300, "Bomb Defuse Bonus");
        }

        public void AwardRoundEnd(Team winner, WinReason reason)
        {
            int attackerReward = winner == Team.Attackers ? GetWinReward(reason) : GetLossReward(attackerLossStreak);
            int defenderReward = winner == Team.Defenders ? GetWinReward(reason) : GetLossReward(defenderLossStreak);

            PlayerHealth[] players = Resources.FindObjectsOfTypeAll<PlayerHealth>();
            for (int i = 0; i < players.Length; i++)
            {
                int amount = players[i].Team == Team.Attackers ? attackerReward : defenderReward;
                string label = players[i].Team == winner ? "Round Win" : "Round Loss";
                AddMoney(players[i], amount, label);
            }

            if (winner == Team.Attackers)
            {
                attackerLossStreak = 0;
                defenderLossStreak++;
            }
            else
            {
                defenderLossStreak = 0;
                attackerLossStreak++;
            }
        }

        public string GetRecentMoneyFeed(PlayerHealth player)
        {
            List<string> feed;
            if (player == null || !moneyFeed.TryGetValue(player, out feed) || feed.Count == 0)
            {
                return string.Empty;
            }

            int start = Mathf.Max(0, feed.Count - 4);
            string text = string.Empty;
            for (int i = start; i < feed.Count; i++)
            {
                text += feed[i];
                if (i < feed.Count - 1)
                {
                    text += "\n";
                }
            }

            return text;
        }

        private void HandlePlayerDied(PlayerHealth victim, DamageInfo info)
        {
            if (info.Attacker == null || info.Attacker == victim || info.Weapon == null)
            {
                return;
            }

            AddMoney(info.Attacker, info.Weapon.KillReward, info.Weapon.Category + " Kill");
        }

        private void Spend(PlayerHealth player, int amount, string label)
        {
            player.Money = Mathf.Clamp(player.Money - amount, 0, maxMoney);
            AddFeed(player, "-" + amount + " " + label);
        }

        private void AddMoney(PlayerHealth player, int amount, string label)
        {
            if (player == null)
            {
                return;
            }

            player.Money = Mathf.Clamp(player.Money + amount, 0, maxMoney);
            AddFeed(player, "+" + amount + " " + label);
        }

        private void AddFeed(PlayerHealth player, string text)
        {
            RegisterPlayer(player);
            moneyFeed[player].Add(text);
            if (moneyFeed[player].Count > 12)
            {
                moneyFeed[player].RemoveAt(0);
            }
        }

        private int GetWinReward(WinReason reason)
        {
            if (reason == WinReason.BombExploded)
            {
                return 3500;
            }

            return 3250;
        }

        private static int GetLossReward(int lossStreak)
        {
            switch (Mathf.Clamp(lossStreak, 0, 5))
            {
                case 0:
                    return 1400;
                case 1:
                    return 1900;
                case 2:
                    return 2400;
                case 3:
                    return 2900;
                default:
                    return 3400;
            }
        }
    }
}
