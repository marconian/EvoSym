using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utilities.Model
{
    public class BlockTemplateSides
    {
        public BlockTemplateSides(Vector3 position, IEnumerable<Vector3> neighboreBlocks)
        {
            Update(position, neighboreBlocks);
        }

        public void Update(Vector3 position, IEnumerable<Vector3> neighboreBlocks)
        {
            Top = neighboreBlocks.FirstOrDefault(r => r == position + Vector3.up);
            Bottom = neighboreBlocks.FirstOrDefault(r => r == position + Vector3.down);
            Right = neighboreBlocks.FirstOrDefault(r => r == position + Vector3.right);
            Left = neighboreBlocks.FirstOrDefault(r => r == position + Vector3.left);
            Front = neighboreBlocks.FirstOrDefault(r => r == position + Vector3.forward);
            Back = neighboreBlocks.FirstOrDefault(r => r == position + Vector3.back);
            FreeSides = new bool[]
            {
                Top == Vector3.zero,
                Bottom == Vector3.zero,
                Right == Vector3.zero,
                Left == Vector3.zero,
                Front == Vector3.zero,
                Back == Vector3.zero
            };
        }

        public Vector3? Top { get; private set; }
        public Vector3? Bottom { get; private set; }
        public Vector3? Right { get; private set; }
        public Vector3? Left { get; private set; }
        public Vector3? Front { get; private set; }
        public Vector3? Back { get; private set; }

        public bool[] FreeSides { get; private set; }

        public bool HasFreeSides
        {
            get => FreeSides.Any(s => s);
        }
    }
}
