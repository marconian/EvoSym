using System;
using UnityEngine;

namespace Assets.Utilities.Model
{
    [Serializable]
    public struct FieldOfView
    {
        public FieldOfView(float height = 0f, float width = 0f, float depth = 0f, float verticalResolution = 1f, float horizontalResolution = 1f)
        {
            this.height = height;
            this.width = width;
            this.depth = depth;
            this.verticalResolution = verticalResolution;
            this.horizontalResolution = horizontalResolution;
        }

        [Range(0f, 90f)]
        public float height;
        [Range(0f, 180f)]
        public float width;
        [Range(0f, 100f)]
        public float depth;

        [Range(1f, 100f)]
        public float horizontalResolution;
        [Range(0.1f, 10f)]
        public float verticalResolution;

        public float HorizontalStep
        {
            get
            {
                float res = horizontalResolution;
                float steps = width / res;
                float floor = Mathf.FloorToInt(steps);
                return res + (steps - floor) / floor;
            }
        }

        public float VerticalStep
        {
            get
            {
                float res = verticalResolution;
                float steps = height / res;
                float floor = Mathf.FloorToInt(steps);
                return res + (steps - floor) / floor;
            }
        }

        public static bool operator ==(FieldOfView a, FieldOfView b)
        {
            return a.height == b.height && a.width == b.width && a.depth == b.depth;
        }
        public static bool operator !=(FieldOfView a, FieldOfView b)
        {
            return !(a.height == b.height && a.width == b.width && a.depth == b.depth);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
