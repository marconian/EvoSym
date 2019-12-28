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
            Sides = new BuildingBlock[]
            {
                neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.up),
                neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.down),
                neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.right),
                neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.left),
                neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.forward),
                neighboreBlocks?.FirstOrDefault(r => r.transform.localPosition == position + Vector3.back)
            };
            FreeSides = Sides
                .Select(s => s == null)
                .ToArray();
        }

        public BuildingBlock Top { get => Sides[0]; }
        public BuildingBlock Bottom { get => Sides[1]; }
        public BuildingBlock Right { get => Sides[2]; }
        public BuildingBlock Left { get => Sides[3]; }
        public BuildingBlock Front { get => Sides[4]; }
        public BuildingBlock Back { get => Sides[5]; }

        public BuildingBlock[] Sides { get; private set; }
        public bool[] FreeSides { get; private set; }

        public bool HasFreeSides
        {
            get => FreeSides.Any(s => s);
        }
    }
}
