using Assets.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utilities.Model
{
    [System.Serializable]
    public class FoliageSetting
    {
        [Range(0f, 1f)]
        public float Size;
        public FoliageType @Type;
    }
}
