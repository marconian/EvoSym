
using Assets.State;
using Assets.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utilities
{
    public static class Tools
    {
        public static int RandomInt(int min, int max) =>
            Mathf.RoundToInt(Random.Range(min, max));
        public static T RandomElement<T>(IEnumerable<T> enumerable)
        {
            int index = Mathf.RoundToInt(Random.Range(-.4999f, (enumerable.Count() - 1) + .4999f));
            if (index < enumerable.Count())
                return enumerable.ElementAt(index);

            return default;
        }

        private static GameObject Ground { get => GameObject.Find("Soil"); }

        public static bool TryRandomPosition(Habitat habitat, out Vector3 position, Vector3 center = default, float radius = 100f)
        {
            position = default;

            if (Ground)
            {
                Transform terrain = Ground.transform;

                float scaleX = terrain.lossyScale.x * 10f;
                float scaleZ = terrain.lossyScale.z * 10f;
                float deadZone = 10f;

                float xmin = (terrain.position.x - scaleX / 2f) + deadZone;
                float zmin = (terrain.position.z - scaleZ / 2f) + deadZone;
                float xmax = (terrain.position.x + scaleX / 2f) - deadZone;
                float zmax = (terrain.position.z + scaleZ / 2f) - deadZone;

                if (center != default && radius != 0)
                {
                    xmin = Mathf.Max(xmin, center.x - radius);
                    zmin = Mathf.Max(zmin, center.z - radius);
                    xmax = Mathf.Min(xmax, center.x + radius);
                    zmax = Mathf.Min(zmax, center.z + radius);

                    if (xmin >= xmax || zmin >= zmax)
                        return false;
                }

                float x = Random.Range(xmin, xmax);
                float z = Random.Range(zmin, zmax);

                bool inrange = ObjectsInRange(x, z, .5f, out ObjectBase[] _);
                if (habitat != Habitat.All || inrange)
                {
                    bool inwater = TerrainState.WaterAtPosition(x, z);

                    int i = 0;
                    while (habitat == Habitat.Land ? inwater : !inwater || inrange)
                    {
                        x = Random.Range(xmin, xmax);
                        z = Random.Range(zmin, zmax);

                        i++;
                        if (i > 100) return false;

                        inwater = TerrainState.WaterAtPosition(x, z);
                        inrange = ObjectsInRange(x, z, .5f, out ObjectBase[] _);
                    }
                }

                float y = 0f;
                if (TerrainState.TryGetHeightAtPosition(x, z, out float height))
                    y = height;
                else return false;

                position = new Vector3(x, y, z);
                return true;
            }

            return false;
        }

        public static Vector3 RandomDirection()
        {
            Vector3 direction = RandomElement(new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back });
            return Quaternion.FromToRotation(Vector3.up, direction).eulerAngles;
        }

        public static Quaternion RandomRotation() => Quaternion.Euler(0, Random.Range(0, 359), 0);

        public static bool ObjectsInRange(float x, float z, float radius, out ObjectBase[] objects) =>
            ObjectsInRange(new Vector3(x, 0f, z), radius, out objects);
        public static bool ObjectsInRange(Vector3 position, float radius, out ObjectBase[] objects)
        {
            objects = AppState.Registry.Values
                .Select(v => (p: v.transform.position - position, o: v))
                .Where(v => Mathf.Max(Mathf.Abs(v.p.x), Mathf.Abs(v.p.z)) < radius)
                .Select(v => v.o)
                .ToArray();

            return objects.Any();
        }
    }
}