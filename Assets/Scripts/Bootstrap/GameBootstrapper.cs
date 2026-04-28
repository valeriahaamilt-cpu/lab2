using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBreachpoint
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool bootstrapOnAwake = true;

        private readonly List<PlayerHealth> players = new List<PlayerHealth>();
        private WeaponDatabase weaponDatabase;
        private EconomyManager economyManager;
        private RoundManager roundManager;
        private BombManager bombManager;
        private IronworksMapGenerator mapGenerator;
        private PlayerHealth humanPlayer;

        private void Awake()
        {
            if (bootstrapOnAwake)
            {
                Bootstrap();
            }
        }

        [ContextMenu("Bootstrap Project Breachpoint")]
        public void Bootstrap()
        {
            Application.targetFrameRate = 144;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            CreateLighting();
            CreateManagers();
            CreateMap();
            CreateTeams();
            CreateUi();

            roundManager.StartMatch(players, mapGenerator, bombManager, economyManager, weaponDatabase);
        }

        private void CreateManagers()
        {
            GameObject managers = new GameObject("Project Breachpoint Managers");
            weaponDatabase = managers.AddComponent<WeaponDatabase>();
            roundManager = managers.AddComponent<RoundManager>();
            economyManager = managers.AddComponent<EconomyManager>();
            bombManager = managers.AddComponent<BombManager>();
            bombManager.Initialize(roundManager, economyManager);
        }

        private void CreateMap()
        {
            GameObject map = new GameObject("Ironworks Generated Map");
            mapGenerator = map.AddComponent<IronworksMapGenerator>();
            mapGenerator.Generate();

            for (int i = 0; i < mapGenerator.BombSites.Count; i++)
            {
                bombManager.RegisterSite(mapGenerator.BombSites[i]);
            }
        }

        private void CreateTeams()
        {
            humanPlayer = CreateHumanPlayer("Breach-1", Team.Attackers);
            players.Add(humanPlayer);

            for (int i = 1; i < 5; i++)
            {
                players.Add(CreateBot("Breach-" + (i + 1), Team.Attackers, i));
            }

            for (int i = 0; i < 5; i++)
            {
                players.Add(CreateBot("Aegis-" + (i + 1), Team.Defenders, i));
            }
        }

        private PlayerHealth CreateHumanPlayer(string displayName, Team team)
        {
            GameObject agent = CreateAgentRoot(displayName, team, true);
            Camera camera = CreatePlayerCamera(agent.transform);
            FirstPersonController controller = agent.AddComponent<FirstPersonController>();
            controller.CameraRoot = camera.transform;

            agent.AddComponent<RecoilSystem>();
            WeaponController weapon = agent.AddComponent<WeaponController>();
            Transform viewModelRoot = new GameObject("View Model Root").transform;
            viewModelRoot.SetParent(camera.transform, false);
            weapon.Initialize(agent.GetComponent<PlayerHealth>(), camera.transform, true);
            weapon.SetViewModelRoot(viewModelRoot);
            weapon.EquipWeapon(weaponDatabase.GetStarterPistol());
            weapon.EquipWeapon(weaponDatabase.GetWeapon("edge_knife"));
            weapon.EquipSlot(WeaponSlot.Pistol);

            CreateFirstPersonArms(camera.transform);
            return agent.GetComponent<PlayerHealth>();
        }

        private PlayerHealth CreateBot(string displayName, Team team, int index)
        {
            GameObject agent = CreateAgentRoot(displayName, team, false);
            agent.AddComponent<RecoilSystem>();
            WeaponController weapon = agent.AddComponent<WeaponController>();
            Transform aim = new GameObject("Bot Aim Source").transform;
            aim.SetParent(agent.transform, false);
            aim.localPosition = new Vector3(0f, 1.45f, 0.12f);

            PlayerHealth health = agent.GetComponent<PlayerHealth>();
            weapon.Initialize(health, aim, false);
            weapon.EquipWeapon(weaponDatabase.GetStarterPistol());
            weapon.EquipWeapon(weaponDatabase.GetWeapon("edge_knife"));
            weapon.EquipSlot(WeaponSlot.Pistol);

            BasicBot bot = agent.AddComponent<BasicBot>();
            bot.Setup(index, roundManager, bombManager, mapGenerator, economyManager, weaponDatabase);
            return health;
        }

        private GameObject CreateAgentRoot(string displayName, Team team, bool human)
        {
            GameObject agent = new GameObject(displayName);
            agent.transform.position = Vector3.up * 0.1f;

            CharacterController characterController = agent.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = Vector3.up * 0.9f;
            characterController.skinWidth = 0.04f;

            PlayerHealth health = agent.AddComponent<PlayerHealth>();
            health.DisplayName = displayName;
            health.Team = team;
            health.IsHumanPlayer = human;
            health.Money = 800;

            Hitbox bodyHitbox = agent.AddComponent<Hitbox>();
            bodyHitbox.Owner = health;
            bodyHitbox.Zone = HitZone.Chest;

            Material teamMaterial = CreateMaterial(team.TeamColor());
            CreateBodyVisual(agent.transform, teamMaterial, human);
            CreateHeadHitbox(agent.transform, health, teamMaterial, human);
            return agent;
        }

        private Camera CreatePlayerCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 74f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 500f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private void CreateBodyVisual(Transform parent, Material material, bool hideRenderer)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body Visual";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.72f, 0.9f, 0.72f);
            Renderer renderer = body.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = !hideRenderer;
            Destroy(body.GetComponent<Collider>());
        }

        private void CreateHeadHitbox(Transform parent, PlayerHealth owner, Material material, bool hideRenderer)
        {
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head Hitbox";
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            head.transform.localScale = Vector3.one * 0.42f;
            Renderer renderer = head.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = !hideRenderer;

            Collider collider = head.GetComponent<Collider>();
            collider.isTrigger = true;
            Hitbox hitbox = head.AddComponent<Hitbox>();
            hitbox.Owner = owner;
            hitbox.Zone = HitZone.Head;
        }

        private void CreateFirstPersonArms(Transform cameraTransform)
        {
            Material material = CreateMaterial(new Color(0.88f, 0.55f, 0.38f));
            CreateArmPart(cameraTransform, "Left Arm", new Vector3(-0.26f, -0.25f, 0.48f), new Vector3(0.08f, 0.08f, 0.46f), material);
            CreateArmPart(cameraTransform, "Right Arm", new Vector3(0.26f, -0.25f, 0.48f), new Vector3(0.08f, 0.08f, 0.46f), material);
        }

        private void CreateArmPart(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = name;
            arm.transform.SetParent(parent, false);
            arm.transform.localPosition = localPosition;
            arm.transform.localScale = scale;
            arm.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(arm.GetComponent<Collider>());
        }

        private void CreateUi()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            GameObject canvasObject = new GameObject("Project Breachpoint UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            HUDController hud = canvasObject.AddComponent<HUDController>();
            hud.Setup(humanPlayer, roundManager, bombManager, economyManager);

            BuyMenu buyMenu = canvasObject.AddComponent<BuyMenu>();
            buyMenu.Setup(humanPlayer, economyManager, weaponDatabase, humanPlayer.GetComponent<FirstPersonController>());
        }

        private void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.52f);

            GameObject sun = new GameObject("Industrial Skylight");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject fill = new GameObject("Factory Fill Light");
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.intensity = 1.5f;
            fillLight.range = 55f;
            fill.transform.position = new Vector3(0f, 12f, 0f);
        }

        private static Material CreateMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }
    }
}
