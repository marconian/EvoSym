using Assets.Utilities.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.State
{
    public static class FoliageState
    {
        static FoliageState()
        {
            var foliageResources = Resources.LoadAll("Foliage")
                .OfType<GameObject>()
                .Select(f => 
                    (t: f.GetComponent<Foliage>().FoliageType, obj: f));

            FoliageResources = foliageResources
                .Select(r => r.t).Distinct()
                .ToDictionary(t => t, t => foliageResources.Where(r => r.t == t)
                    .Select(r => r.obj).ToList());
            FoliageLimits = Enum.GetValues(typeof(FoliageType))
                .OfType<FoliageType>()
                .ToDictionary(v => v, v => 0);
            FoliageCollection = Enum.GetValues(typeof(FoliageType))
                .OfType<FoliageType>()
                .ToDictionary(v => v, v => new ObjectCollection<Foliage>());
        }

        public static Dictionary<FoliageType, List<GameObject>> FoliageResources { get; }
        public static Dictionary<FoliageType, ObjectCollection<Foliage>> FoliageCollection { get; }

        public static IEnumerable<Foliage> Foliage
        {
            get => AppState.Registry.Values.OfType<Foliage>();
        }
        public static Dictionary<FoliageType, int> FoliageLimits { get; }
    }

    public enum FoliageType
    {
        Tree,
        Bush,
        Plant,
        Flower,
        Grass,
        WaterPlant,
        Log,
        Rock,
        Other
    }
}
