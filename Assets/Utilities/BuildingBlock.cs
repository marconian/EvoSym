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
    public class BuildingBlock : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(DoSense());
        }

        public float EnergyStorage = 1f;

        public float Oxygen = 0f;
        public float Water = 0f;
        public float Food = 0f;
        public float Speed = 0f;
        public float Sight = 0f;
        public float Sense = 0f;
        public float Strength = 0f;

        public float ActiveOxygen { get => Oxygen * _activeOxygen; }
        private float _activeOxygen = 1f;
        public float ActiveWater { get => Water * _activeWater; }
        private float _activeWater = 1f;
        public float ActiveFood { get => Food * _activeFood; }
        private float _activeFood = 1f;
        public float ActiveSpeed { get => Speed * _activeSpeed; }
        private float _activeSpeed = 0f;
        public float ActiveSight { get => Sight * _activeSight; }
        private float _activeSight = 1f;
        public float ActiveSense { get => Sense * _activeSense; }
        private float _activeSense = 1f;
        public float ActiveStrength { get => Strength * _activeStrength; }
        private float _activeStrength = 1f;

        public BoxCollider Collider { get => GetComponent<BoxCollider>(); }
        public Body BodyRef { get => GetComponentInParent<Body>(); }

        private BuildingBlock[] NeighboreBlocks
        {
            get => BodyRef?.ActiveBlocks
                .Where(b => b.transform.localPosition != transform.localPosition)
                .ToArray();
        }

        private BlockSides _blockSides;
        public BlockSides Sides
        {
            get
            {
                Vector3 position = transform.localPosition;
                if (_blockSides == null)
                    _blockSides = new BlockSides(position, NeighboreBlocks);
                else _blockSides.Update(position, NeighboreBlocks);

                return _blockSides;
            }
        }

        private float DoSenseInterval = .5f;
        private IEnumerator DoSense()
        {
            while (Sense > 0 || Sight > 0)
            {
                if (BodyRef != null)
                {
                    IEnumerable<SensoryData> data = new SensoryData[0];
                    if (Sense > 0)
                        data = data.Union(ProcessSensoryData(Sense));
                    if (Sight > 0)
                        data = data.Union(ProcessSensoryData(Sight, 20f));

                    data = data.ToArray();
                    BodyRef.OnSensedObjects(this, data.ToArray());
                }

                yield return new WaitForSeconds(DoSenseInterval);
            }
        }

        private (Collider obj, Vector3 pos, float dist)[] GatherSensoryData(float distance, float view = 180f)
        {
            List<(Collider obj, Vector3 pos, float dist)> results = new List<(Collider, Vector3, float)>();

            Transform body = BodyRef.transform;
            int layerMask = LayerMask.GetMask("Foliage", "FoliageParts", "Animals", "Terrain");

            for (float y = -1; y <= 1; y++)
            {
                Vector3 slope = new Vector3(0f, y, 0f);
                for (float angle = -view; angle <= view; angle += 6)
                {
                    Quaternion rotation = Quaternion.AngleAxis(angle, transform.up);
                    Vector3 direction = rotation * transform.forward * distance + slope;

                    if (Physics.Raycast(new Ray(transform.position, direction), out RaycastHit hit, distance, layerMask))
                        results.Add((hit.collider, body.InverseTransformPoint(hit.point), hit.distance));

                    Debug.DrawRay(transform.position, direction, Color.green, DoSenseInterval);
                }
            }

            return results.OrderBy(d => d.dist).ToArray();
        }

        private SensoryData[] ProcessSensoryData(float distance, float view = 180f)
        {
            (Collider obj, Vector3 pos, float dist)[] found = GatherSensoryData(distance, view);

            Transform body = BodyRef.transform;
            bool hydrophobic = BodyRef.BodyStats.OxygenAbsorbtion == 0;

            IEnumerable<SensoryData> objects = found
                .Where(c => AppState.Registry.ContainsKey(c.obj.name))
                .Select(c => (obj: AppState.Registry[c.obj.name], c.pos, c.dist))
                .Where(c => !hydrophobic || c.obj.transform.position.y > AppState.WaterLevel)
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = c.obj.tag == "Animal" ? SensoryType.Animal : SensoryType.Plant
                });
            IEnumerable<SensoryData> environment = found
                .Where(c => c.obj.tag == "Mountain" || (hydrophobic && c.obj.tag == "Water"))
                .Select(c => (obj: c.obj.transform.gameObject, c.pos, c.dist))
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = SensoryType.Environment
                });

            SensoryData[] data = objects.Union(environment).ToArray();
            return data;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (BodyRef != null && AppState.Registry.ContainsKey(other.name))
                BodyRef.OnBlockCollision(this, AppState.Registry[other.name]);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.tag == "Ground")
            {
                if (Speed > 0)
                    _activeSpeed = _activeSpeed < 1 ? _activeSpeed + 0.1f : 1;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            //if (other.tag == "Ground")
            //    _activeSpeed = _activeSpeed > 0 ? _activeSpeed - 0.01f : 0;
        }
    }
}
