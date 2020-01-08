using Assets.State;
using Assets.Utilities.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Assets.Utilities
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SenseCone : MonoBehaviour
    {
        private Body BodyRef { get => transform.parent.parent.TryGetComponent(out Body body) ? body : null; }
        private void Awake()
        {
            if (transform.parent.TryGetComponent(out BuildingBlock block) && TryGetComponent(out MeshFilter filter))
            {
                Mesh mesh = null;
                if (block.Sense > 0)
                    mesh = GetSensoryfieldCone(block.Sense);
                else if (block.Sight > 0)
                    mesh = GetSensoryfieldCone(block.Sight, 20f);

                if (mesh != null)
                    filter.sharedMesh = mesh;
            }

            gameObject.SetActive(AppState.SenseConesVisible);
            AnimalState.SenseCones.Add(gameObject);
        }

        private void OnEnable()
        {
            if (BodyRef != null && TryGetComponent(out MeshRenderer renderer))
            {
                Diet diet = AnimalState.BodyTemplates[BodyRef.Template.Value].Diet;

                UnityEngine.Object resource;
                if (diet == Diet.Carnivore)
                    resource = Resources.Load("Blocks/Materials/SenseConeCarnivore");
                else resource = Resources.Load("Blocks/Materials/SenseCone");

                if (resource != null && resource is Material material)
                    renderer.sharedMaterial = material;
            }
        }

        private void OnDestroy()
        {
            AnimalState.SenseCones.Remove(gameObject);
        }

        public Mesh GetSensoryfieldCone(float distance, float view = 180f)
        {
            Mesh mesh = new Mesh();

            List<Vector3> verticesTop = new List<Vector3>();
            List<Vector3> verticesBottom = new List<Vector3>();

            int angles = 0;
            for (float angle = -view; angle <= view; angle += 6)
            {
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 direction = rotation * Vector3.forward * distance;

                verticesTop.Add(direction + new Vector3(0f, 1, 0f));
                verticesBottom.Add(direction + new Vector3(0f, -1, 0f));

                angles++;
            }

            verticesBottom.Reverse();

            Vector3[] vertices = (new List<Vector3>() { Vector3.zero })
                .Union(verticesTop)
                .Union(verticesBottom)
                .ToArray();

            List<int> triangles = new List<int>();
            List<int> idx = vertices.Select((v, i) => i)
                .Skip(1)
                .ToList();

            int l = vertices.Length - 1;

            foreach ((int a, int b) in idx.Select((v, i) => (v, idx.ElementAt(i < idx.Count() - 1 ? i + 1 : 0))))
            {
                if (!(view == 180f && (a == 1 && b == l || a == l && b == 1)))
                {
                    triangles.Add(0);
                    triangles.Add(a);
                    triangles.Add(b);
                }
            }

            for (int i = 0; i < verticesTop.Count; i++)
            {
                int a1 = i + 1;
                int a2 = a1 + 1;
                int b1 = l - i;
                int b2 = b1 - 1;

                triangles.Add(a2);
                triangles.Add(a1);
                triangles.Add(b1);

                triangles.Add(a2);
                triangles.Add(b1);
                triangles.Add(b2);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            //mesh.uv = vertices.Select(v => new Vector2(v.x, v.y)).ToArray();

            return mesh;
        }
    }
}
