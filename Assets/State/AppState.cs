using Assets.Utilities;
using Assets.Utilities.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Assets.State
{
    public static class AppState
    {
        static AppState() 
        {
            Registry = new Dictionary<string, GameObject>();
        }

        public static bool Paused { get; set; }
        public static Dictionary<string, GameObject> Registry { get; }
        public static bool SenseConesVisible { get; set; }
        public static Body Selected { get; set; }
    }
}
