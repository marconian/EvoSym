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
                    (t: Enum.TryParse(f.name.Split('_')[0], out FoliageType t) ? t : FoliageType.Other, obj: f));

            FoliageCollection = foliageResources
                .Select(r => r.t).Distinct()
                .ToDictionary(t => t, t => foliageResources.Where(r => r.t == t)
                    .Select(r => r.obj).ToList());
            FoliageLimits = Enum.GetValues(typeof(FoliageType))
                .OfType<FoliageType>()
                .ToDictionary(v => v, v => 100);
        }

        public static Dictionary<FoliageType, List<GameObject>> FoliageCollection { get; }
        public static IEnumerable<Foliage> Foliage
        {
            get => AppState.Registry.Values.Where(r => r != null && r.layer == 8)
                .Select(o => o.TryGetComponent(out Foliage f) ? f : null)
                .Where(f => f != null);
        }
        public static Dictionary<FoliageType, int> FoliageCount
        {
            get => Enum.GetValues(typeof(FoliageType))
                .OfType<FoliageType>()
                .ToDictionary(v => v, v => Foliage
                    .Where(f => f.FoliageType == v).Count());
        }
        public static Dictionary<FoliageType, int> FoliageLimits { get; }
    }
}
