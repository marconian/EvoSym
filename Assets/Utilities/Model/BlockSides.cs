using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utilities.Model
{
    public class BlockSides
    {
        public BlockSides(Vector3 position, IEnumerable<BuildingBlock> neighboreBlocks)
        {
            Update(position, neighboreBlocks);
        }

        public void Update(Vector3 position, IEnumerable<BuildingBlock> neighboreBlocks)
        {
            Top = neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.up);
            Bottom = neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.down);
            Right = neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.right);
            Left = neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.left);
            Front = neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.forward);
            Back = neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.back);
            FreeSides = new bool[]
            {
                Top == null,
                Bottom == null,
                Right == null,
                Left == null,
                Front == null,
                Back == null
            };
        }

        public BuildingBlock Top { get; private set; }
        public BuildingBlock Bottom { get; private set; }
        public BuildingBlock Right { get; private set; }
        public BuildingBlock Left { get; private set; }
        public BuildingBlock Front { get; private set; }
        public BuildingBlock Back { get; private set; }

        public bool[] FreeSides { get; private set; }

        public bool HasFreeSides
        {
            get => FreeSides.Any(s => s);
        }
    }
}
