using Assets.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Assets.Utilities.Model
{

    public class BlockTemplateCollection : Dictionary<Vector3, BlockTemplate>
    {
        public BlockTemplateCollection(BodyTemplate template)
        {
            Template = template;
        }

        private BodyTemplate Template { get; }
        public void Add(string name, Vector3 position, Vector3 rotation, float mutationChance = .9f)
            => Add(position, new BlockTemplate(name, position, rotation, Template, mutationChance));


        public bool TryAddRandom()
        {
            var usable = Values
                .Where(j => j.Sides.HasFreeSides)
                .ToList();

            Vector3 position = Vector3.zero;
            if (usable.Count > 0)
            {
                BlockTemplate jointBlock = Tools.RandomElement(usable);
                BlockTemplateSides jointSides = jointBlock.Sides;

                int[] available = jointSides.FreeSides
                    .Select((s, i) => s ? i : -1)
                    .Where(i => i != -1).ToArray();

                if (available.Any())
                {
                    position = jointBlock.Position;
                    int side = Tools.RandomElement(available);

                    switch (side)
                    {
                        case 0:
                            position += Vector3.up;
                            break;
                        case 1:
                            position += Vector3.down;
                            break;
                        case 2:
                            position += Vector3.right;
                            break;
                        case 3:
                            position += Vector3.left;
                            break;
                        case 4:
                            position += Vector3.forward;
                            break;
                        case 5:
                            position += Vector3.back;
                            break;
                        default:
                            Debug.LogError("Unknown side of block!");
                            break;
                    }
                }
                else
                {
                    Debug.LogWarning("Unavailable sides should not occur here!");
                    return false;
                }
            }

            if (!Values.Any() || !Values.Any(b => b.Position == position))
            {
                Add(Tools.RandomElement(AnimalState.BuildingBlocks.Keys), position, Tools.RandomDirection());
                return true;
            }

            Debug.LogWarning($"Overlaping blocks at position: x{position.x} y{position.y} z{position.z}!");
            return false;
        }
    }
}
