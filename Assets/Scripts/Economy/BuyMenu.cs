using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBreachpoint
{
    public sealed class BuyMenu : MonoBehaviour
    {
        private sealed class BuyButtonBinding
        {
            public Button Button;
            public Text Label;
            public System.Func<bool> CanBuy;
        }

        [SerializeField] private KeyCode toggleKey = KeyCode.B;

        private readonly List<BuyButtonBinding> bindings = new List<BuyButtonBinding>();
        private PlayerHealth player;
        private FirstPersonController controller;
        private EconomyManager economy;
        private WeaponDatabase weaponDatabase;
        private GameObject panel;
        private Text moneyText;
        private Text hintText;
        private Font font;
        private bool open;

        public bool IsOpen
        {
            get { return open; }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                SetOpen(!open);
            }

            if (open)
            {
                Refresh();
                if (!economy.CanBuy(player) || Input.GetKeyDown(KeyCode.Escape))
                {
                    SetOpen(false);
                }
            }
        }

        public void Setup(PlayerHealth targetPlayer, EconomyManager economyManager, WeaponDatabase database, FirstPersonController firstPersonController)
        {
            player = targetPlayer;
            economy = economyManager;
            weaponDatabase = database;
            controller = firstPersonController;
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildUi();
            SetOpen(false);
        }

        public void SetOpen(bool value)
        {
            if (value && !economy.CanBuy(player))
            {
                value = false;
            }

            open = value;
            if (panel != null)
            {
                panel.SetActive(open);
            }

            if (controller != null)
            {
                controller.SetInputBlocked(open);
            }

            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            Refresh();
        }

        private void BuildUi()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            panel = new GameObject("Buy Menu Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 520f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.045f, 0.94f);

            Text title = CreateText("PROJECT BREACHPOINT ARMORY", panel.transform, 24, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(28f, -28f), new Vector2(520f, 34f), new Vector2(0f, 1f));

            moneyText = CreateText("$800", panel.transform, 22, TextAnchor.MiddleRight);
            SetRect(moneyText.rectTransform, new Vector2(-28f, -28f), new Vector2(170f, 34f), new Vector2(1f, 1f));

            hintText = CreateText("Buy phase only. Replacing occupied slots is allowed.", panel.transform, 14, TextAnchor.MiddleLeft);
            hintText.color = new Color(0.75f, 0.8f, 0.85f);
            SetRect(hintText.rectTransform, new Vector2(28f, -66f), new Vector2(520f, 24f), new Vector2(0f, 1f));

            float y = -112f;
            CreateSection("Weapons", y);
            y -= 42f;
            AddWeaponButton("PX-9 Sidearm", "Starter pistol | 12 rounds | accurate first shot", "px9_sidearm", y);
            y -= 58f;
            AddWeaponButton("Raptor-45", "High damage pistol | slow fire | heavy recoil", "raptor45", y);
            y -= 58f;
            AddWeaponButton("VGR-17 Vanguard", "Power rifle | strong recoil | high penetration", "vgr17_vanguard", y);
            y -= 58f;
            AddWeaponButton("DMR-22 Guardian", "Stable rifle | clean first shot | lower recoil", "dmr22_guardian", y);

            y = -112f;
            CreateSection("Armor / Equipment", y, 395f);
            y -= 42f;
            AddEquipmentButton("Light Armor", "$650 | 100 armor, no helmet", y, 650, delegate { return economy.TryBuyLightArmor(player); }, 395f);
            y -= 58f;
            AddEquipmentButton("Heavy Armor + Helmet", "$1000 | armor plus head protection", y, 1000, delegate { return economy.TryBuyHeavyArmor(player); }, 395f);
            y -= 58f;
            AddEquipmentButton("Defuse Kit", "$400 | defenders defuse in 5s", y, 400, delegate { return economy.TryBuyDefuseKit(player); }, 395f);
        }

        private void AddWeaponButton(string title, string desc, string weaponId, float y)
        {
            WeaponData weapon = weaponDatabase.GetWeapon(weaponId);
            if (weapon == null)
            {
                return;
            }

            AddEquipmentButton(title, "$" + weapon.Price + " | " + desc + "\nDMG " + weapon.BaseDamage + "  ROF " + weapon.FireRate.ToString("0.0") + "  MAG " + weapon.MagazineSize,
                y,
                weapon.Price,
                delegate { return economy.TryBuyWeapon(player, weapon); },
                28f);
        }

        private void AddEquipmentButton(string title, string desc, float y, int price, System.Func<bool> buyAction, float x)
        {
            GameObject go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(x, y), new Vector2(320f, 48f), new Vector2(0f, 1f));
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.15f, 0.17f, 0.95f);
            Button button = go.GetComponent<Button>();

            Text label = CreateText(title + "\n" + desc, go.transform, 13, TextAnchor.MiddleLeft);
            label.color = Color.white;
            SetRect(label.rectTransform, new Vector2(12f, 0f), new Vector2(295f, 44f), new Vector2(0f, 0.5f));

            button.onClick.AddListener(delegate
            {
                if (buyAction())
                {
                    Refresh();
                }
            });

            bindings.Add(new BuyButtonBinding
            {
                Button = button,
                Label = label,
                CanBuy = delegate { return economy.CanBuy(player) && player.Money >= price && (title != "Defuse Kit" || player.Team == Team.Defenders); }
            });
        }

        private void CreateSection(string text, float y, float x = 28f)
        {
            Text label = CreateText(text, panel.transform, 18, TextAnchor.MiddleLeft);
            label.color = new Color(0.93f, 0.74f, 0.34f);
            SetRect(label.rectTransform, new Vector2(x, y), new Vector2(320f, 28f), new Vector2(0f, 1f));
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
            text.verticalOverflow = VerticalWrapMode.Truncate;
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

        private void Refresh()
        {
            if (moneyText != null && player != null)
            {
                moneyText.text = "$" + player.Money;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                bool canBuy = bindings[i].CanBuy();
                bindings[i].Button.interactable = canBuy;
                bindings[i].Label.color = canBuy ? Color.white : new Color(0.55f, 0.58f, 0.6f);
            }
        }
    }
}
