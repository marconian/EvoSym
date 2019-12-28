using Assets.Utilities.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Assets.Utilities
{
    public static class AppState
    {
        static AppState() 
        {
            GenerationCount = 1;

            DefaultTemplate = new BodyTemplate() { Diet = Diet.Herbivore };
            DefaultTemplate.Template.Add("Feeling", new Vector3(0f, 0f, 0f), Vector3.zero);
            DefaultTemplate.Template.Add("Motor", new Vector3(0f, 0f, -1f), Vector3.zero);

            BuildingBlocks = Resources.LoadAll("Blocks")
                .OfType<GameObject>()
                .Select(o => o.TryGetComponent(out BuildingBlock b) ? b : null)
                .Where(b => b != null)
                .ToDictionary(b => b.name, b => b);

            BodyTemplates = new Dictionary<Guid, BodyTemplate>() 
                {{ Guid.NewGuid(), DefaultTemplate }};

            Registry = new Dictionary<string, GameObject>();
        }

        public static Body Selected { get; set; }

        public static int GenerationCount { get; set; }
        public static BodyTemplate DefaultTemplate { get; }
        public static Dictionary<Guid, BodyTemplate> BodyTemplates { get; }

        public static Dictionary<string, GameObject> Registry { get; }
        public static Dictionary<string, BuildingBlock> BuildingBlocks { get; }

        public static IEnumerable<(Body body, BodyStats stats)> Animals
        {
            get => GameObject.FindGameObjectsWithTag("Animal")
                .Where(o => AppState.Registry.ContainsKey(o.name))
                .Select(o => (
                    body: o.TryGetComponent(out Body c) ? c : null,
                    stats: o.TryGetComponent(out BodyStats s) ? s : null
                ))
                .Where(b => b.body != null && b.stats != null);
        }

        public static bool ReachedHerbivoreLimit { get => Herbivores.Count() <= 150; }
        public static IEnumerable<(Body body, BodyStats stats)> Herbivores
        {
            get => Animals
                .Where(b => b.body.Template.HasValue && BodyTemplates[b.body.Template.Value].Diet == Diet.Herbivore);
        }
        public static IEnumerable<(Body body, BodyStats stats)> Carnivores
        {
            get => Animals
                .Where(b => b.body.Template.HasValue && BodyTemplates[b.body.Template.Value].Diet == Diet.Carnivore);
        }

        private static float? _waterLevel;
        public static float WaterLevel
        {
            get
            {
                if (!_waterLevel.HasValue)
                    _waterLevel = GameObject.Find("Water")?.transform.position.y;
                return _waterLevel.HasValue ? _waterLevel.Value : 0f;
            }
        }

        private static float? _maxDepth;
        public static float MaxDepth
        {
            get
            {
                if (!_maxDepth.HasValue && GameObject.Find("Ground").TryGetComponent(out MeshFilter filter))
                {
                    _maxDepth = filter.mesh.vertices.Min(v => v.y) + filter.gameObject.transform.position.y;
                }
                return _maxDepth.HasValue ? _maxDepth.Value : -100f;
            }
        }

        public static float[] Borders
        {
            get
            {
                Transform terrain = GameObject.Find("Ground").transform;

                float scaleX = terrain.lossyScale.x * 10;
                float scaleZ = terrain.lossyScale.z * 10;

                float xmin = terrain.position.x - scaleX / 2;
                float zmin = terrain.position.z - scaleZ / 2;
                float xmax = xmin + scaleX;
                float zmax = zmin + scaleZ;

                return new float[]
                {
                    xmin, zmin, xmax, zmax
                };
            }
        }

        public static bool TryGetHeightAtPosition(float x, float z, out float height)
        {
            if (TryGetHeightAtPosition(new Vector3(x, 0f, z), out float y))
            {
                height = y;
                return true;
            }

            height = 0f;
            return false;
        }

        public static bool TryGetHeightAtPosition(Vector3 position, out float height)
        {
            int layerMask = LayerMask.GetMask("Terrain");

            Ray ray;
            if (WaterAtPosition(position, out RaycastHit waterHit))
                ray = new Ray(new Vector3(position.x, waterHit.point.y, position.z), Vector3.down);
            else
                ray = new Ray(new Vector3(position.x, 1000, position.z), Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                height = hit.point.y;
                return true;
            }

            height = 0f;
            return false;
        }

        public static bool WaterAtPosition(float x, float z) =>
            WaterAtPosition(new Vector3(x, 0f, z));
        public static bool WaterAtPosition(Vector3 position)
        {
            if (WaterAtPosition(position, out RaycastHit hit))
                return true;

            return false;
        }
        public static bool WaterAtPosition(Vector3 position, out RaycastHit hit)
        {
            int layerMask = LayerMask.GetMask("Terrain");
            Ray ray = new Ray(new Vector3(position.x, 1000, position.z), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit h, Mathf.Infinity, layerMask) && h.transform.name == "Water")
            {
                hit = h;
                return true;
            }

            hit = new RaycastHit();
            return false;
        }
    }
}
