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
        public float ActiveSight { get => Sight * _activeSight; }
        private float _activeSight = 1f;
        public float ActiveSense { get => Sense * _activeSense; }
        private float _activeSense = 1f;
        public float ActiveStrength { get => Strength * _activeStrength; }
        private float _activeStrength = 1f;
        public float ActiveSpeed { 
            get 
            {
                float active = _activeSpeed;
                if (Speed > 0 && active == 0 && NeighboreBlocks.Any())
                    active = NeighboreBlocks.Max(b => b._activeSpeed);

                return Speed * active;
            } 
        }
        public float _activeSpeed = 0f;

        public BoxCollider Collider { get => GetComponent<BoxCollider>(); }
        public Body BodyRef { get => GetComponentInParent<Body>(); }

        private BlockSides _blockSides;
        public BlockSides Sides
        {
            get
            {
                Vector3 position = transform.localPosition;
                BuildingBlock[] neighbores = BodyRef?.ActiveBlocks
                    .Where(b => b.transform.localPosition != transform.localPosition)
                    .ToArray();
                if (_blockSides == null)
                    _blockSides = new BlockSides(position, neighbores);
                else _blockSides.Update(position, neighbores);

                return _blockSides;
            }
        }
        public IEnumerable<BuildingBlock> NeighboreBlocks { get => Sides.Sides.Where(s => s != null); }

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
            bool hydrophobic = BodyRef.BodyStats.Hydrophobic;
            bool underwater = BodyRef.BodyStats.InWater;
            float height = BodyRef.transform.position.y;

            IEnumerable<SensoryData> objects = found
                .Where(c => AppState.Registry.ContainsKey(c.obj.name))
                .Select(c => (obj: AppState.Registry[c.obj.name], c.pos, c.dist))
                .Where(c => !hydrophobic || c.obj.transform.position.y > TerrainState.WaterLevel)
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = c.obj.tag == "Animal" ? SensoryType.Animal : SensoryType.Plant
                });
            IEnumerable<SensoryData> edge = found
                .Where(c => c.obj.tag == "Mountain")
                .Select(c => (obj: c.obj.transform.gameObject, c.pos, c.dist))
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = SensoryType.Environment
                });
            IEnumerable<SensoryData> water = found
                .Where(c => hydrophobic && !underwater && c.obj.tag == "Water")
                .Select(c => (obj: c.obj.transform.gameObject, c.pos, c.dist))
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = SensoryType.Environment
                });
            IEnumerable<SensoryData> environment = found
                .Where(c => c.obj.tag == "Ground" &&
                    (hydrophobic && body.TransformPoint(c.pos).y < TerrainState.WaterLevel ||
                    !hydrophobic && body.TransformPoint(c.pos).y > TerrainState.WaterLevel))
                .Select(c => (obj: c.obj.transform.gameObject, c.pos, c.dist))
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos.y > 0 ?
                        hydrophobic ? c.pos : -c.pos :
                        hydrophobic ? -c.pos : c.pos,
                    Distance = c.dist,
                    SensoryType = SensoryType.Environment
                });

            SensoryData[] data = objects
                .Union(edge)
                .Union(water)
                .Union(environment)
                .ToArray();

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
                if (Speed > 0 && _activeSpeed < 1f)
                    _activeSpeed += 0.1f;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            //if (other.tag == "Ground")
            //    _activeSpeed = _activeSpeed > 0 ? _activeSpeed - 0.01f : 0;
        }
    }
}
