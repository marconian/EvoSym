
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
        public static T RandomElement<T>(IEnumerable<T> enumerable) =>
            enumerable.ElementAt(Mathf.RoundToInt(Random.Range(0, enumerable.Count() - 1)));

        private static GameObject Ground { get => GameObject.Find("Ground"); }

        public static Vector3 RandomPosition(bool allowUnderWater = true)
        {
            if (Ground)
            {
                Transform terrain = Ground.transform;

                float scaleX = terrain.lossyScale.x * 10;
                float scaleZ = terrain.lossyScale.z * 10;

                float xmin = terrain.position.x - scaleX / 2;
                float zmin = terrain.position.z - scaleZ / 2;
                float xmax = xmin + scaleX;
                float zmax = zmin + scaleZ;

                float deadZone = 10f;

                float x = Random.Range(xmin + deadZone, xmax - deadZone);
                float z = Random.Range(zmin + deadZone, zmax - deadZone);

                if (!allowUnderWater)
                {
                    int i = 0;
                    while (AppState.WaterAtPosition(x, z))
                    {
                        x = Random.Range(xmin + deadZone, xmax - deadZone);
                        z = Random.Range(zmin + deadZone, zmax - deadZone);

                        i++;
                        if (i > 1000) break;
                    }
                }

                float y = 0f;
                if (AppState.TryGetHeightAtPosition(x, z, out float height))
                    y = height;

                Vector3 position = new Vector3(x, y, z);

                return position;
            }

            return Vector3.zero;
        }

        public static Vector3 RandomDirection()
        {
            Vector3 direction = RandomElement(new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back });
            return Quaternion.FromToRotation(Vector3.up, direction).eulerAngles;
        }
    }
}