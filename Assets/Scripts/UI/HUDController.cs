using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBreachpoint
{
    public sealed class HUDController : MonoBehaviour
    {
        private struct FeedItem
        {
            public string Text;
            public float Time;
        }

        private readonly List<FeedItem> killFeed = new List<FeedItem>();
        private PlayerHealth player;
        private FirstPersonController movement;
        private WeaponController weapon;
        private RecoilSystem recoil;
        private RoundManager round;
        private BombManager bomb;
        private EconomyManager economy;
        private Font font;

        private Text vitalsText;
        private Text ammoText;
        private Text matchText;
        private Text bombText;
        private Text promptText;
        private Text feedText;
        private Text moneyFeedText;
        private Text hitMarkerText;
        private Image plantFill;
        private Image defuseFill;
        private RectTransform crosshairTop;
        private RectTransform crosshairBottom;
        private RectTransform crosshairLeft;
        private RectTransform crosshairRight;
        private float hitMarkerUntil;

        private void OnEnable()
        {
            PlayerHealth.AnyPlayerDied += HandlePlayerDied;
            PlayerHealth.AnyPlayerDamaged += HandlePlayerDamaged;
        }

        private void OnDisable()
        {
            PlayerHealth.AnyPlayerDied -= HandlePlayerDied;
            PlayerHealth.AnyPlayerDamaged -= HandlePlayerDamaged;
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            UpdateHudText();
            UpdateCrosshair();
            UpdateProgressBars();
            UpdateKillFeed();
        }

        public void Setup(PlayerHealth humanPlayer, RoundManager roundManager, BombManager bombManager, EconomyManager economyManager)
        {
            player = humanPlayer;
            movement = humanPlayer.GetComponent<FirstPersonController>();
            weapon = humanPlayer.GetComponent<WeaponController>();
            recoil = humanPlayer.GetComponent<RecoilSystem>();
            round = roundManager;
            bomb = bombManager;
            economy = economyManager;
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildUi();
        }

        private void BuildUi()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            vitalsText = CreateText("100 HP", canvas.transform, 22, TextAnchor.LowerLeft);
            SetRect(vitalsText.rectTransform, new Vector2(24f, 24f), new Vector2(340f, 80f), new Vector2(0f, 0f));

            ammoText = CreateText("PX-9 | 12 / 48", canvas.transform, 22, TextAnchor.LowerRight);
            SetRect(ammoText.rectTransform, new Vector2(-24f, 24f), new Vector2(360f, 80f), new Vector2(1f, 0f));

            matchText = CreateText("BUY PHASE", canvas.transform, 20, TextAnchor.UpperCenter);
            SetRect(matchText.rectTransform, new Vector2(0f, -18f), new Vector2(600f, 80f), new Vector2(0.5f, 1f));

            bombText = CreateText("Bomb", canvas.transform, 18, TextAnchor.UpperLeft);
            SetRect(bombText.rectTransform, new Vector2(24f, -18f), new Vector2(420f, 110f), new Vector2(0f, 1f));

            promptText = CreateText("", canvas.transform, 18, TextAnchor.MiddleCenter);
            promptText.color = new Color(1f, 0.85f, 0.35f);
            SetRect(promptText.rectTransform, new Vector2(0f, -118f), new Vector2(600f, 42f), new Vector2(0.5f, 0.5f));

            feedText = CreateText("", canvas.transform, 15, TextAnchor.UpperRight);
            SetRect(feedText.rectTransform, new Vector2(-24f, -92f), new Vector2(420f, 220f), new Vector2(1f, 1f));

            moneyFeedText = CreateText("", canvas.transform, 14, TextAnchor.LowerLeft);
            moneyFeedText.color = new Color(0.65f, 1f, 0.65f);
            SetRect(moneyFeedText.rectTransform, new Vector2(24f, 112f), new Vector2(280f, 120f), new Vector2(0f, 0f));

            hitMarkerText = CreateText("X", canvas.transform, 26, TextAnchor.MiddleCenter);
            hitMarkerText.color = new Color(1f, 0.92f, 0.65f);
            SetRect(hitMarkerText.rectTransform, Vector2.zero, new Vector2(60f, 60f), new Vector2(0.5f, 0.5f));

            crosshairTop = CreateCrosshairLine(canvas.transform, new Vector2(2f, 12f));
            crosshairBottom = CreateCrosshairLine(canvas.transform, new Vector2(2f, 12f));
            crosshairLeft = CreateCrosshairLine(canvas.transform, new Vector2(12f, 2f));
            crosshairRight = CreateCrosshairLine(canvas.transform, new Vector2(12f, 2f));

            plantFill = CreateProgress(canvas.transform, new Vector2(0f, -74f), new Color(1f, 0.66f, 0.18f));
            defuseFill = CreateProgress(canvas.transform, new Vector2(0f, -96f), new Color(0.2f, 0.72f, 1f));
        }

        private void UpdateHudText()
        {
            string helmet = player.HasHelmet ? " + HELMET" : string.Empty;
            vitalsText.text = player.Health + " HP\n" + player.Armor + " ARMOR" + helmet + "\n$" + player.Money;

            WeaponData current = weapon.CurrentWeapon;
            ammoText.text = current != null
                ? current.DisplayName + "\n" + weapon.CurrentAmmo + " / " + weapon.ReserveAmmo
                : "Unarmed";

            int aliveAttackers = CountAlive(Team.Attackers);
            int aliveDefenders = CountAlive(Team.Defenders);
            string timerText = round.MatchComplete ? "" : "  " + FormatTime(round.CurrentPhase == RoundPhase.BombPlanted ? bomb.BombTimeRemaining : round.PhaseTimeRemaining);
            matchText.text = round.GetPhaseText() + timerText
                + "\nA " + round.AttackersScore + " - " + round.DefendersScore + " D"
                + "   Round " + round.RoundNumber
                + "   Alive " + aliveAttackers + "v" + aliveDefenders;

            bombText.text = BuildBombText();
            moneyFeedText.text = economy.GetRecentMoneyFeed(player);
            hitMarkerText.enabled = Time.time < hitMarkerUntil;
            promptText.text = BuildPrompt();
        }

        private string BuildBombText()
        {
            if (bomb.State == BombState.Planted || bomb.State == BombState.Defusing)
            {
                return "BREACH CHARGE PLANTED " + bomb.CurrentSiteName + "\nDetonation " + FormatTime(bomb.BombTimeRemaining);
            }

            if (bomb.IsCarrier(player))
            {
                return "You carry the breach charge\nHold E inside Site A or B";
            }

            if (bomb.State == BombState.Dropped)
            {
                return "Charge dropped\nDistance " + Vector3.Distance(player.transform.position, bomb.BombWorldPosition).ToString("0.0") + "m";
            }

            if (bomb.Carrier != null)
            {
                return "Charge carrier: " + bomb.Carrier.DisplayName;
            }

            return "Charge status: " + bomb.State;
        }

        private string BuildPrompt()
        {
            if (!player.IsAlive)
            {
                return "You are down. Round resets soon.";
            }

            if (bomb.IsCarrier(player) && bomb.GetSiteForPosition(player.transform.position) != null && round.CurrentPhase == RoundPhase.Live)
            {
                return "Hold E to plant";
            }

            if (player.Team == Team.Defenders && (bomb.State == BombState.Planted || bomb.State == BombState.Defusing) && Vector3.Distance(player.transform.position, bomb.BombWorldPosition) < 2.5f)
            {
                return player.HasDefuseKit ? "Hold E to defuse (kit)" : "Hold E to defuse";
            }

            if (player.Team == Team.Attackers && bomb.State == BombState.Dropped && Vector3.Distance(player.transform.position, bomb.BombWorldPosition) < 2f)
            {
                return "Hold E to pick up charge";
            }

            if (round.CurrentPhase == RoundPhase.BuyPhase)
            {
                return "Press B to buy";
            }

            return string.Empty;
        }

        private void UpdateCrosshair()
        {
            float gap = 8f;
            if (movement != null)
            {
                gap += movement.AccuracyMovementPenalty * 38f;
            }

            if (recoil != null)
            {
                gap += recoil.RecoilIndex * 1.5f;
            }

            crosshairTop.anchoredPosition = new Vector2(0f, gap);
            crosshairBottom.anchoredPosition = new Vector2(0f, -gap);
            crosshairLeft.anchoredPosition = new Vector2(-gap, 0f);
            crosshairRight.anchoredPosition = new Vector2(gap, 0f);
        }

        private void UpdateProgressBars()
        {
            plantFill.fillAmount = bomb.PlantProgress;
            defuseFill.fillAmount = bomb.DefuseProgress;
            plantFill.transform.parent.gameObject.SetActive(bomb.PlantProgress > 0f);
            defuseFill.transform.parent.gameObject.SetActive(bomb.DefuseProgress > 0f);
        }

        private void UpdateKillFeed()
        {
            for (int i = killFeed.Count - 1; i >= 0; i--)
            {
                if (Time.time - killFeed[i].Time > 6f)
                {
                    killFeed.RemoveAt(i);
                }
            }

            string text = string.Empty;
            for (int i = 0; i < killFeed.Count; i++)
            {
                text += killFeed[i].Text;
                if (i < killFeed.Count - 1)
                {
                    text += "\n";
                }
            }

            feedText.text = text;
        }

        private void HandlePlayerDied(PlayerHealth victim, DamageInfo info)
        {
            string attacker = info.Attacker != null ? info.Attacker.DisplayName : "World";
            string weaponName = info.Weapon != null ? info.Weapon.DisplayName : "Damage";
            killFeed.Add(new FeedItem { Text = attacker + " eliminated " + victim.DisplayName + " [" + weaponName + "]", Time = Time.time });
            if (killFeed.Count > 6)
            {
                killFeed.RemoveAt(0);
            }
        }

        private void HandlePlayerDamaged(PlayerHealth victim, DamageInfo info, int amount)
        {
            if (info.Attacker == player && victim != player)
            {
                hitMarkerUntil = Time.time + 0.12f;
            }
        }

        private int CountAlive(Team team)
        {
            int count = 0;
            for (int i = 0; i < round.Players.Count; i++)
            {
                if (round.Players[i].Team == team && round.Players[i].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private Image CreateProgress(Transform parent, Vector2 position, Color color)
        {
            GameObject root = new GameObject("Progress Root", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetRect(rootRect, position, new Vector2(260f, 12f), new Vector2(0.5f, 0.5f));
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(root.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            SetRect(fillRect, Vector2.zero, new Vector2(260f, 12f), new Vector2(0.5f, 0.5f));
            Image fill = fillObject.GetComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            return fill;
        }

        private RectTransform CreateCrosshairLine(Transform parent, Vector2 size)
        {
            GameObject go = new GameObject("Crosshair Line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, size, new Vector2(0.5f, 0.5f));
            go.GetComponent<Image>().color = new Color(0.8f, 1f, 0.86f, 0.9f);
            return rect;
        }

        private Text CreateText(string value, Transform parent, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return minutes + ":" + secs.ToString("00");
        }
    }
}
