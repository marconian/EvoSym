using System;
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
        private void Start()
        {
            _lifeSpanConstant = UnityEngine.Random.Range(0.5f, 1.2f);
        }
        public void Wake()
        {
            BodyRef = GetComponent<Body>();

            Width = (_blockPositions.Max(v => v.x) + 1) - _blockPositions.Min(v => v.x);
            Height = (_blockPositions.Max(v => v.y) + 1) - _blockPositions.Min(v => v.y);
            Depth = (_blockPositions.Max(v => v.z) + 1) - _blockPositions.Min(v => v.z);
            EnergyStorage = BodyRef.ActiveBlocks
                .Sum(b => b.EnergyStorage * (1 - (0.1f * b.Sides.FreeSides.Count(s => !s))));

            StartCoroutine(HeartBeat());
        }

        public float UpdateTiming = 0.1f;
        public float AgingSpeed = 0.002f;
        public float FoodConsumptionSpeed = 0.001f;
        public float OxygenConsumptionSpeed = 0.002f;
        public float WaterConsumptionSpeed = 0.002f;

        private Body BodyRef { get; set; }

        public bool IsAlive { get => BodyRef != null && Food > 0 && Water > 0 && Oxygen > 0 && LifeSpan < TotalLifeSpan && transform.position.y > AppState.MaxDepth; }
        public bool InWater { get => BodyRef.transform.position.y < AppState.WaterLevel; }
        public int ChildCount { get; set; }


        private Vector3[] _blockPositions { get => BodyRef.ActiveBlocks.Select(v => v.transform.localPosition).ToArray(); }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Depth { get; private set; }

        public float EnergyStorage { get; private set; }
        public float OxygenAbsorbtion { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveOxygen); }
        public float WaterAbsorbtion { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveWater) + 2f; }
        public float FoodAbsorbtion { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveFood) + 2f; }
        public float Speed { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveSpeed); }
        public float Sight { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveSight); }
        public float Sense { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveSense); }
        public float Strength { get => BodyRef.ActiveBlocks.Sum(b => b.ActiveStrength); }

        private float TotalFood { get => EnergyStorage; }
        private float TotalWater { get => EnergyStorage; }
        private float TotalOxygen { get => EnergyStorage; }


        private float _lifeSpanConstant = 1f;
        public float TotalLifeSpan
        {
            get
            {
                return Mathf.CeilToInt(_lifeSpanConstant / AgingSpeed);
            }
        }

        public float ReproductionRate { 
            get 
            {
                // min time based on food reserves
                float minTime = Mathf.CeilToInt(TotalFood / FoodConsumptionSpeed) * UpdateTiming;

                // 10% more time than food reserve allows, to force animal to eat some food
                return minTime * 1.05f + UnityEngine.Random.Range(0f, 2f);
            }
        }

        private float _food = Mathf.Infinity;
        public float Food 
        {
            get => _food;
            set => _food = _food + FoodAbsorbtion * value < TotalFood ? _food + FoodAbsorbtion * value : TotalFood;
        }
        private float _water = Mathf.Infinity;
        public float Water
        {
            get => _water;
            set => _water = _water + WaterAbsorbtion * value < TotalWater ? _water + WaterAbsorbtion * value : TotalWater;
        }
        private float _oxygen = Mathf.Infinity;
        public float Oxygen
        {
            get => _oxygen;
            set => _oxygen = _oxygen + OxygenAbsorbtion * value < TotalOxygen ? _oxygen + OxygenAbsorbtion * value : TotalOxygen;
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

                _food -= FoodConsumptionSpeed;
                //_water -= Energy * WaterConsumptionSpeed * UpdateTiming;

                if (InWater)
                    _oxygen -= OxygenConsumptionSpeed;

                if (!InWater || OxygenAbsorbtion > 0)
                    Oxygen = 0.1f;

                LifeSpan += AgingSpeed;

                yield return new WaitForSeconds(UpdateTiming);
            }

            AppState.Registry.Remove(BodyRef.Template.Value.ToString());
            Destroy(gameObject);
        }
    }
}
