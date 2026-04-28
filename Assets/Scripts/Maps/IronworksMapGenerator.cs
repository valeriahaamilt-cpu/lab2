using System.Collections.Generic;
using UnityEngine;

namespace ProjectBreachpoint
{
    public sealed class IronworksMapGenerator : MonoBehaviour
    {
        private readonly List<Transform> attackerSpawns = new List<Transform>();
        private readonly List<Transform> defenderSpawns = new List<Transform>();
        private readonly List<Transform> attackerTargets = new List<Transform>();
        private readonly List<Transform> defenderTargets = new List<Transform>();
        private readonly List<Transform> coverPoints = new List<Transform>();
        private readonly List<BombSite> bombSites = new List<BombSite>();

        private Material floorMaterial;
        private Material wallMaterial;
        private Material crateMaterial;
        private Material metalMaterial;
        private Material siteAMaterial;
        private Material siteBMaterial;

        public IReadOnlyList<BombSite> BombSites
        {
            get { return bombSites; }
        }

        public void Generate()
        {
            CreateMaterials();
            ClearChildren();

            CreateCube("Ironworks Floor", new Vector3(0f, -0.08f, 0f), new Vector3(92f, 0.16f, 72f), floorMaterial);
            CreatePerimeter();
            CreateRoutesAndCover();
            CreateSites();
            CreateSpawns();
            CreateBotTargets();
        }

        public Transform GetAttackerSpawn(int index)
        {
            return attackerSpawns[Mathf.Abs(index) % attackerSpawns.Count];
        }

        public Transform GetDefenderSpawn(int index)
        {
            return defenderSpawns[Mathf.Abs(index) % defenderSpawns.Count];
        }

        public Transform GetObjectiveTarget(Team team, int index)
        {
            List<Transform> list = team == Team.Attackers ? attackerTargets : defenderTargets;
            return list[Mathf.Abs(index) % list.Count];
        }

        public Transform GetCoverPoint(int index)
        {
            return coverPoints[Mathf.Abs(index) % coverPoints.Count];
        }

        private void CreateMaterials()
        {
            floorMaterial = Material(new Color(0.2f, 0.22f, 0.22f));
            wallMaterial = Material(new Color(0.34f, 0.36f, 0.37f));
            crateMaterial = Material(new Color(0.5f, 0.34f, 0.16f));
            metalMaterial = Material(new Color(0.16f, 0.19f, 0.21f));
            siteAMaterial = Material(new Color(1f, 0.62f, 0.18f, 0.55f));
            siteBMaterial = Material(new Color(0.2f, 0.75f, 1f, 0.55f));
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }

            attackerSpawns.Clear();
            defenderSpawns.Clear();
            attackerTargets.Clear();
            defenderTargets.Clear();
            coverPoints.Clear();
            bombSites.Clear();
        }

        private void CreatePerimeter()
        {
            CreateCube("North Wall", new Vector3(0f, 2f, 36f), new Vector3(92f, 4f, 1.6f), wallMaterial);
            CreateCube("South Wall", new Vector3(0f, 2f, -36f), new Vector3(92f, 4f, 1.6f), wallMaterial);
            CreateCube("West Wall", new Vector3(-46f, 2f, 0f), new Vector3(1.6f, 4f, 72f), wallMaterial);
            CreateCube("East Wall", new Vector3(46f, 2f, 0f), new Vector3(1.6f, 4f, 72f), wallMaterial);
        }

        private void CreateRoutesAndCover()
        {
            CreateCube("A Main Warehouse Wall", new Vector3(-17f, 1.5f, -8f), new Vector3(2f, 3f, 30f), wallMaterial);
            CreateCube("B Tunnel Wall", new Vector3(17f, 1.5f, -8f), new Vector3(2f, 3f, 30f), wallMaterial);
            CreateCube("Mid Control Block", new Vector3(0f, 1.6f, 7f), new Vector3(13f, 3.2f, 2f), wallMaterial);
            CreateCube("Defender Rotation Wall", new Vector3(0f, 1.6f, 23f), new Vector3(40f, 3.2f, 2f), wallMaterial);

            CreateCube("A Truck Body", new Vector3(-29f, 1.1f, 10f), new Vector3(5.5f, 2.2f, 2.8f), metalMaterial);
            CreateCube("A Truck Cab", new Vector3(-24.5f, 1.0f, 10f), new Vector3(2.2f, 2f, 2.6f), metalMaterial);
            CreateCube("B Machine Core", new Vector3(28f, 1.2f, 10f), new Vector3(4.4f, 2.4f, 4.4f), metalMaterial);
            CreateCube("Mid Catwalk", new Vector3(0f, 2.4f, -7f), new Vector3(14f, 0.35f, 3.8f), metalMaterial);
            CreateCube("Catwalk Ramp A", new Vector3(-8f, 1.15f, -9f), new Vector3(7f, 0.35f, 3f), metalMaterial).transform.rotation = Quaternion.Euler(0f, 0f, -16f);
            CreateCube("Catwalk Ramp B", new Vector3(8f, 1.15f, -9f), new Vector3(7f, 0.35f, 3f), metalMaterial).transform.rotation = Quaternion.Euler(0f, 0f, 16f);

            AddCrateStack(-31f, -1f);
            AddCrateStack(-22f, 18f);
            AddCrateStack(-7f, -18f);
            AddCrateStack(7f, -18f);
            AddCrateStack(22f, 19f);
            AddCrateStack(33f, -2f);
            AddCrateStack(0f, 15f);
            AddCrateStack(-35f, 24f);
            AddCrateStack(35f, 24f);
        }

        private void AddCrateStack(float x, float z)
        {
            CreateCube("Crate", new Vector3(x, 0.6f, z), new Vector3(2.6f, 1.2f, 2.6f), crateMaterial);
            CreateCube("Crate Cover", new Vector3(x + 1.5f, 1.45f, z + 1.3f), new Vector3(2.1f, 1.1f, 2.1f), crateMaterial);
            coverPoints.Add(CreatePoint("Cover", new Vector3(x, 0f, z - 2.2f), Quaternion.identity));
        }

        private void CreateSites()
        {
            bombSites.Add(CreateSite("A", new Vector3(-28f, 1.5f, 16f), new Vector3(15f, 3f, 13f), siteAMaterial));
            bombSites.Add(CreateSite("B", new Vector3(28f, 1.5f, 16f), new Vector3(15f, 3f, 13f), siteBMaterial));
        }

        private BombSite CreateSite(string siteName, Vector3 position, Vector3 size, Material markerMaterial)
        {
            GameObject site = new GameObject("Bomb Site " + siteName, typeof(BoxCollider), typeof(BombSite));
            site.transform.SetParent(transform, false);
            site.transform.position = position;
            BoxCollider collider = site.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;

            BombSite bombSite = site.GetComponent<BombSite>();
            bombSite.SiteName = siteName;

            GameObject marker = CreateCube("Site " + siteName + " Marker", new Vector3(position.x, 0.02f, position.z), new Vector3(size.x, 0.04f, size.z), markerMaterial);
            marker.transform.SetParent(site.transform, true);
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            return bombSite;
        }

        private void CreateSpawns()
        {
            for (int i = 0; i < 5; i++)
            {
                attackerSpawns.Add(CreatePoint("Attacker Spawn " + (i + 1), new Vector3(-8f + i * 4f, 0.05f, -30f), Quaternion.Euler(0f, 0f, 0f)));
                defenderSpawns.Add(CreatePoint("Defender Spawn " + (i + 1), new Vector3(-8f + i * 4f, 0.05f, 30f), Quaternion.Euler(0f, 180f, 0f)));
            }
        }

        private void CreateBotTargets()
        {
            attackerTargets.Add(CreatePoint("Attack Route A Main", new Vector3(-29f, 0.05f, 15f), Quaternion.identity));
            attackerTargets.Add(CreatePoint("Attack Route B Tunnel", new Vector3(29f, 0.05f, 15f), Quaternion.identity));
            attackerTargets.Add(CreatePoint("Attack Route Mid", new Vector3(0f, 0.05f, 4f), Quaternion.identity));
            attackerTargets.Add(CreatePoint("Attack Connector", new Vector3(-9f, 0.05f, 12f), Quaternion.identity));
            attackerTargets.Add(CreatePoint("Attack Late B", new Vector3(21f, 0.05f, 19f), Quaternion.identity));

            defenderTargets.Add(CreatePoint("Defend A Dock", new Vector3(-28f, 0.05f, 20f), Quaternion.identity));
            defenderTargets.Add(CreatePoint("Defend B Machine", new Vector3(28f, 0.05f, 20f), Quaternion.identity));
            defenderTargets.Add(CreatePoint("Defend Mid", new Vector3(0f, 0.05f, 14f), Quaternion.identity));
            defenderTargets.Add(CreatePoint("Defend Connector", new Vector3(-9f, 0.05f, 20f), Quaternion.identity));
            defenderTargets.Add(CreatePoint("Defend Rotation", new Vector3(9f, 0.05f, 20f), Quaternion.identity));
        }

        private GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(transform, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return cube;
        }

        private Transform CreatePoint(string name, Vector3 position, Quaternion rotation)
        {
            GameObject point = new GameObject(name);
            point.transform.SetParent(transform, false);
            point.transform.position = position;
            point.transform.rotation = rotation;
            return point.transform;
        }

        private static Material Material(Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            return material;
        }
    }
}
