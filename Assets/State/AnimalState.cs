using Assets.Utilities;
using Assets.Utilities.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.State
{
    public static class AnimalState
    {
        static AnimalState()
        {
            GenerationCount = 0;

            DefaultTemplate = new BodyTemplate()
            {
                Diet = Diet.Herbivore,
                ChildrenPerLifetime = 3
            };
            DefaultTemplate.Template.Add("Feeling", new Vector3(0f, 0f, 0f), Vector3.zero);
            DefaultTemplate.Template.Add("Motor", new Vector3(0f, 0f, -1f), Vector3.zero);

            BuildingBlocks = Resources.LoadAll("Blocks")
                .OfType<GameObject>()
                .Select(o => o.TryGetComponent(out BuildingBlock b) ? b : null)
                .Where(b => b != null)
                .ToDictionary(b => b.name, b => b);

            BodyTemplates = new Dictionary<Guid, BodyTemplate>();
            BodyCollection = new Dictionary<Guid, ObjectCollection<Body>>();
            SenseCones = new List<GameObject>();
        }
        public static List<GameObject> SenseCones { get; }

        public static int GenerationCount { get; set; }
        public static BodyTemplate DefaultTemplate { get; }
        public static Dictionary<Guid, BodyTemplate> BodyTemplates { get; }
        public static Dictionary<Guid, ObjectCollection<Body>> BodyCollection { get; }
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

        public static bool ReachedHerbivoreLimit { get => Herbivores.Count() >= 150; }
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
    }
}
