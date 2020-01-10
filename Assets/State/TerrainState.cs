using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.State
{
    public static class TerrainState
    {
        private static GameObject _water;
        public static float WaterLevel
        {
            get
            {
                if (_water == null)
                    _water = GameObject.Find("Water");
                return _water.transform.position.y;
            }
        }

        private static float? _maxDepth;
        public static float MaxDepth
        {
            get
            {
                if (!_maxDepth.HasValue && GameObject.Find("Soil").TryGetComponent(out MeshFilter filter))
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
                Transform terrain = GameObject.Find("Soil").transform;

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
            int layerMask = LayerMask.GetMask("Soil");

            Ray ray = new Ray(new Vector3(position.x, 1000, position.z), Vector3.down);
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
            if (TryGetHeightAtPosition(position, out float heigth) && heigth <= WaterLevel)
                return true;

            return false;
        }
    }
}
