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

            BlockPositions = BodyRef.ActiveBlocks.Select(v => v.transform.localPosition).ToArray();
            Width = (BlockPositions.Max(v => v.x) + 1) - BlockPositions.Min(v => v.x);
            Height = (BlockPositions.Max(v => v.y) + 1) - BlockPositions.Min(v => v.y);
            Depth = (BlockPositions.Max(v => v.z) + 1) - BlockPositions.Min(v => v.z);

            Efficiency = BodyRef.ActiveBlocks
                .Sum(b => 0.01f * b.NeighboreBlocks.Count());

            EnergyStorage = BodyRef.ActiveBlocks.Sum(b => b.EnergyStorage) + MinimalEnergyStorage;
            OxygenAbsorbtion = BodyRef.ActiveBlocks.Sum(b => b.ActiveOxygen);
            WaterAbsorbtion = BodyRef.ActiveBlocks.Sum(b => b.ActiveWater);
            FoodAbsorbtion = BodyRef.ActiveBlocks.Sum(b => b.ActiveFood);
            Sense = BodyRef.ActiveBlocks.Sum(b => b.ActiveSense);
            Strength = BodyRef.ActiveBlocks.Sum(b => b.ActiveStrength);
            Speed = BodyRef.ActiveBlocks.Sum(b => b.ActiveSpeed);
            if (Speed < 0)
                Speed = 0;

            Hydrophobic = OxygenAbsorbtion == 0;

            TotalFood = EnergyStorage;
            TotalWater = 1f;
            TotalOxygen = 1f;
            TotalLifeSpan = Mathf.CeilToInt(Random.Range(.01f, .05f) / AgingSpeed + Strength);

            FoodPerHeartBeat = FoodConsumptionSpeed * BodyRef.ActiveBlocks.Count - (FoodConsumptionSpeed * BodyRef.ActiveBlocks.Count) * Efficiency;

            ChildCount = 0;
            Awake = true;

            StartCoroutine(HeartBeat());
        }

        public float UpdateTiming = 0.1f;
        public float AgingSpeed = 0.008f;
        public float FoodConsumptionSpeed = 0.008f;
        public float OxygenConsumptionSpeed = 0.01f;
        public float WaterConsumptionSpeed = 0.001f;
        public float MinimalEnergyStorage = 2f;

        [Range(0f, .9f)]
        public float ChildSpawningCost = .1f;

        private Body BodyRef { get; set; }

        public bool Awake { get; private set; }
        public bool IsAlive { get => gameObject.activeSelf && BodyRef != null && Food > 0 && Water > 0 && Oxygen > 0 && LifeSpan < TotalLifeSpan && transform.position.y > TerrainState.MaxDepth && transform.position.y < 100f; }
        public bool InWater { get => BodyRef.ActiveBlocks.Min(b => b.transform.position.y) < TerrainState.WaterLevel; }

        private Vector3[] BlockPositions { get; set; }
        private float Efficiency { get; set; }

        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Depth { get; private set; }

        public float EnergyStorage { get; private set; }
        public float OxygenAbsorbtion { get; private set; }
        public float WaterAbsorbtion { get; private set; }
        public float FoodAbsorbtion { get; private set; }
        public float Sense { get; private set; }
        public float Strength { get; private set; }
        public float Speed { get; private set; }

        public float TotalFood { get; private set; }
        public float TotalWater { get; private set; }
        public float TotalOxygen { get; private set; }

        public bool Hydrophobic { get; private set; }

        public float TotalLifeSpan { get; private set; }

        public float GestationPeriod { get; private set; }
        public bool Reproduce { get => Awake && GestationPeriod <= 0f && Food + .1f > BodyRef.ActiveBlocks.Count * ChildSpawningCost; }

        private int _childCount = 0;
        public int ChildCount
        {
            get => _childCount;
            set
            {
                _childCount = value;

                float minFood = Mathf.FloorToInt(MinimalEnergyStorage / FoodPerHeartBeat);

                if (_childCount == 0)
                {
                    GestationPeriod = minFood * AgingSpeed;
                }
                else
                {
                    float childrenPerLifetime = AnimalState.BodyTemplates[BodyRef.Template.Value].ChildrenPerLifetime;
                    float stepsPerLifetime = Mathf.FloorToInt(TotalLifeSpan / AgingSpeed);
                    float stepsPerChild = Mathf.FloorToInt((stepsPerLifetime - minFood) / childrenPerLifetime);

                    GestationPeriod = stepsPerChild * AgingSpeed + Random.Range(-.1f, .1f);

                    _food -= BodyRef.ActiveBlocks.Count * ChildSpawningCost;
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

        private float FoodPerHeartBeat { get; set; }

        public IEnumerator HeartBeat()
        {
            while (IsAlive)
            {
                if (_food == Mathf.Infinity)
                    _food = MinimalEnergyStorage;
                if (_water == Mathf.Infinity)
                    _water = TotalWater;
                if (_oxygen == Mathf.Infinity)
                    _oxygen = TotalOxygen;

                _food -= FoodPerHeartBeat;
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

            ObjectCollection<Body> coll = AnimalState.BodyCollection[BodyRef.Template.Value];
            coll.Store(BodyRef);
        }
    }
}
