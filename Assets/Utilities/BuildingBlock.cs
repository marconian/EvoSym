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
        private void OnEnable()
        {
            StartCoroutine(DoSense());
        }

        public float EnergyStorage = 0f;

        public float Oxygen = 0f;
        public float Water = 0f;
        public float Food = 0f;
        public float Speed = 0f;
        public float Strength = 0f;
        public FieldOfView Sense = default;

        public float ActiveOxygen { get => Oxygen * _activeOxygen; }
        private float _activeOxygen = 1f;
        public float ActiveWater { get => Water * _activeWater; }
        private float _activeWater = 1f;
        public float ActiveFood { get => Food * _activeFood; }
        private float _activeFood = 1f;
        public float ActiveSight { get => Sense.depth * _activeSight; }
        private float _activeSight = 1f;
        public float ActiveSense { get => Sense.depth * _activeSense; }
        private float _activeSense = 1f;
        public float ActiveStrength { get => Strength * _activeStrength; }
        private float _activeStrength = 1f;
        public float ActiveSpeed { 
            get 
            {
                float minY = BodyRef.ActiveBlocks.Min(b => b.transform.localPosition.y);
                float y = transform.localPosition.y;
                float c = BodyRef.ActiveBlocks.Count(b => b.Speed > 0 && b.transform.localPosition.y == minY);

                float baseSpeed = 8f;
                if (c > 1) baseSpeed /= c;

                if (Speed > 0)
                {
                    if (y == minY)
                        _activeSpeed = Speed * 1f + baseSpeed;
                    else if (NeighboreBlocks.Any(b => b.Speed > 0))
                        _activeSpeed = Speed * .5f;
                }
                else if (Speed == 0 && y == minY)
                    _activeSpeed = baseSpeed * -1;

                return _activeSpeed;
            } 
        }
        private float _activeSpeed = 0f;

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
            while (gameObject.activeSelf && Sense != default)
            {
                if (BodyRef != null && BodyRef.BodyStats != null)
                {
                    IEnumerable<SensoryData> data = new SensoryData[0];
                    if (Sense != default)
                        data = data.Union(ProcessSensoryData(Sense));

                    data = data.ToArray();
                    BodyRef.OnSensedObjects(this, data.ToArray());
                }

                yield return new WaitForSeconds(DoSenseInterval);
            }
        }

        private (GameObject obj, Vector3 pos, float dist)[] GatherSensoryData(FieldOfView view)
        {
            List<(GameObject obj, Vector3 pos, float dist)> results = new List<(GameObject, Vector3, float)>();

            Transform body = BodyRef.transform;
            int layerMask = LayerMask.GetMask("Foliage", "Animals", "Soil", "Edge");

            float horizontalStep = view.HorizontalStep;
            float verticalStep = view.VerticalStep;
            for (float y = -view.height; y <= view.height; y += verticalStep)
            {
                Vector3 slope = new Vector3(0f, y, 0f);
                for (float angle = -view.width; angle <= view.width; angle += horizontalStep)
                {
                    Quaternion rotation = Quaternion.AngleAxis(angle, transform.up);
                    Vector3 direction = rotation * transform.forward * view.depth + slope;

                    if (Physics.Raycast(new Ray(transform.position, direction), out RaycastHit hit, view.depth, layerMask))
                    {
                        GameObject obj = hit.collider.gameObject;

                        if (obj.layer == 8 && !obj.TryGetComponent(out Foliage foliage))
                        {
                            GameObject v = obj;
                            while (obj != null && !obj.TryGetComponent(out foliage))
                                obj = obj.transform.parent != null ? obj.transform.parent.gameObject : null;
                        }

                        if (obj != null)
                            results.Add((obj, body.InverseTransformPoint(hit.point), hit.distance));
                    }

                    Debug.DrawRay(transform.position, direction, Color.green, DoSenseInterval);
                }
            }

            return results.OrderBy(d => d.dist).ToArray();
        }

        private SensoryData[] ProcessSensoryData(FieldOfView view)
        {
            (GameObject obj, Vector3 pos, float dist)[] found = GatherSensoryData(view);

            Transform body = BodyRef.transform;
            bool hydrophobic = BodyRef.BodyStats.Hydrophobic;
            bool underwater = BodyRef.BodyStats.InWater;
            float height = BodyRef.transform.position.y;

            IEnumerable<SensoryData> objects = found
                .Where(c => AppState.Registry.ContainsKey(c.obj.name))
                .Select(c => (obj: AppState.Registry[c.obj.name], c.pos, c.dist))
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj.gameObject,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = c.obj.tag == "Animal" ? SensoryType.Animal : SensoryType.Plant
                });
            IEnumerable<SensoryData> edge = found
                .Where(c => c.obj.tag == "Mountain")
                .Select(c => (obj: c.obj, c.pos, c.dist))
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = SensoryType.Environment
                });
            IEnumerable<SensoryData> water = found
                .Where(c => c.obj.tag == "Water" && hydrophobic && !underwater && TerrainState.WaterAtPosition(c.pos))
                .Select(c => (obj: c.obj.transform.gameObject, c.pos, c.dist))
                .Select(c => new SensoryData(body)
                {
                    Subject = c.obj,
                    Position = c.pos,
                    Distance = c.dist,
                    SensoryType = SensoryType.Environment
                });
            IEnumerable<SensoryData> environment = found
                .Where(c => c.obj.tag == "Soil" &&
                    (hydrophobic && TerrainState.WaterAtPosition(body.TransformPoint(c.pos)) ||
                    !hydrophobic && !TerrainState.WaterAtPosition(body.TransformPoint(c.pos))))
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
            GameObject obj = other.gameObject;
            if (obj.layer == 8)
            {
                while (obj != null && !obj.TryGetComponent(out Foliage foliage))
                    obj = obj.transform.parent != null ? obj.transform.parent.gameObject : null;

            }

            if (BodyRef != null && obj != null && AppState.Registry.ContainsKey(obj.name))
                BodyRef.OnBlockCollision(this, AppState.Registry[obj.name].gameObject);
        }
    }
}
