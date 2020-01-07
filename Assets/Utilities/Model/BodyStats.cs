using Assets.State;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utilities.Model
{
    [RequireComponent(typeof(Body))]
    public class BodyStats : MonoBehaviour
    {
        public void Wake()
        {
            BodyRef = GetComponent<Body>();

            Width = (_blockPositions.Max(v => v.x) + 1) - _blockPositions.Min(v => v.x);
            Height = (_blockPositions.Max(v => v.y) + 1) - _blockPositions.Min(v => v.y);
            Depth = (_blockPositions.Max(v => v.z) + 1) - _blockPositions.Min(v => v.z);

            Efficiency = BodyRef.ActiveBlocks
                .Sum(b => 0.01f * b.NeighboreBlocks.Count());

            _lifeSpanConstant = Random.Range(.05f, .2f);
            ChildCount = 0;
            Awake = true;

            StartCoroutine(HeartBeat());
        }

        public float UpdateTiming = 0.1f;
        public float AgingSpeed = 0.008f;
        public float FoodConsumptionSpeed = 0.008f;
        public float OxygenConsumptionSpeed = 0.01f;
        public float WaterConsumptionSpeed = 0.001f;
        public float ChildSpawningCost = .1f;

        private Body BodyRef { get; set; }

        public bool Awake { get; private set; }
        public bool IsAlive { get => gameObject.activeSelf && BodyRef != null && Food > 0 && Water > 0 && Oxygen > 0 && LifeSpan < TotalLifeSpan && transform.position.y > TerrainState.MaxDepth && transform.position.y < 100f; }
        public bool InWater { get => BodyRef.transform.position.y < TerrainState.WaterLevel; }

        private Vector3[] _blockPositions { get => BodyRef.ActiveBlocks.Select(v => v.transform.localPosition).ToArray(); }
        private float Efficiency { get; set; }

        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Depth { get; private set; }

        public float EnergyStorage { get => BodyRef.ActiveBlocks.Sum(b => b.EnergyStorage); }
        public float OxygenAbsorbtion { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveOxygen); }
        public float WaterAbsorbtion { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveWater); }
        public float FoodAbsorbtion { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveFood); }
        public float Sight { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveSight); }
        public float Sense { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveSense); }
        public float Strength { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveStrength); }
        public float Speed
        {
            get
            {
                float speed = BodyRef.ActiveBlocks.Sum(b => b.ActiveSpeed);
                return speed > 0 ? speed + 5f : 0f;
            }
        }

        public float TotalFood { get => EnergyStorage + 1f; }
        public float TotalWater { get => EnergyStorage; }
        public float TotalOxygen { get => EnergyStorage; }

        public bool Hydrophobic { get => OxygenAbsorbtion == 0; }


        private float _lifeSpanConstant = 1f;
        public float TotalLifeSpan { get => Mathf.CeilToInt(_lifeSpanConstant / AgingSpeed + Strength); }

        public float GestationPeriod { get; private set; }
        public bool Reproduce { get => Awake && GestationPeriod < .5f && Food / TotalFood > .7f; }

        private int _childCount = 0;
        public int ChildCount
        {
            get => _childCount;
            set
            {
                _childCount = value;

                int childrenPerLifetime = AnimalState.BodyTemplates[BodyRef.Template.Value].ChildrenPerLifetime;
                GestationPeriod = (TotalLifeSpan - 1) / childrenPerLifetime + Random.Range(-.2f, .2f);

                if (_childCount == 0)
                {
                    float minFood = (TotalFood / FoodConsumptionSpeed) * AgingSpeed;
                    if (minFood > GestationPeriod)
                        GestationPeriod = (TotalFood / FoodConsumptionSpeed) * AgingSpeed + 1f;
                }
                else
                {
                    _food -= TotalFood * ChildSpawningCost;
                }
            }
        }

        private float _food = Mathf.Infinity;
        public float Food 
        {
            get => _food;
            set => _food = _food + FoodAbsorbtion * value + .2f < TotalFood ? _food + FoodAbsorbtion * value + .2f : TotalFood;
        }
        private float _water = Mathf.Infinity;
        public float Water
        {
            get => _water;
            set => _water = _water + WaterAbsorbtion * value + .1f < TotalWater ? _water + WaterAbsorbtion * value + .1f : TotalWater;
        }
        private float _oxygen = Mathf.Infinity;
        public float Oxygen
        {
            get => _oxygen;
            set => _oxygen = _oxygen + OxygenAbsorbtion * value + .1f < TotalOxygen ? _oxygen + OxygenAbsorbtion * value + .1f : TotalOxygen;
        }

        public float LifeSpan { get; set; } = 0f;

        public IEnumerator HeartBeat()
        {
            while (IsAlive)
            {
                if (_food == Mathf.Infinity)
                    _food = TotalFood;
                if (_water == Mathf.Infinity)
                    _water = TotalWater;
                if (_oxygen == Mathf.Infinity)
                    _oxygen = TotalOxygen;

                _food -= FoodConsumptionSpeed - FoodConsumptionSpeed * Efficiency;
                //_water -= WaterConsumptionSpeed - WaterConsumptionSpeed * Efficiency;

                if (Hydrophobic && InWater || !Hydrophobic && !InWater)
                    _oxygen -= OxygenConsumptionSpeed - OxygenConsumptionSpeed * Efficiency;
                else Oxygen = 1f;

                LifeSpan += AgingSpeed;
                GestationPeriod -= AgingSpeed;

                yield return new WaitForSeconds(UpdateTiming);
            }

            Awake = false;
            _food = Mathf.Infinity;
            _water = Mathf.Infinity;
            _oxygen = Mathf.Infinity;

            AnimalState.BodyCollection[BodyRef.Template.Value].Store(BodyRef);
        }
    }
}
