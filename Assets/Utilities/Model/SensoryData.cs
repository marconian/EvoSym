using UnityEngine;

namespace Assets.Utilities.Model
{
    public class SensoryData
    {
        public SensoryData(Transform body)
        {
            Body = body;
        }

        private Transform Body { get; }

        public GameObject Subject { get; set; }
        public SensoryType SensoryType { get; set; }

        private Vector3? _position;
        public Vector3 Position {
            get
            {
                if (_position.HasValue)
                    return _position.Value;
                else if (Subject != null)
                    return Body.InverseTransformPoint(Subject.transform.position);

                return Vector3.zero;
            }
            set => _position = value;
        }

        private float? _distance;
        public float Distance { 
            get
            {
                if (_distance.HasValue)
                    return _distance.Value;

                return Vector3.Distance(Position, Vector3.zero);
            }
            set => _distance = value;
        }
    }

    public enum SensoryType
    {
        Animal,
        Plant,
        Environment,
        Unknown
    }
}
