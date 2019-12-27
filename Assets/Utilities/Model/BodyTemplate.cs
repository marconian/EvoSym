using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utilities;

namespace Assets.Utilities.Model
{
    public class BodyTemplate
    {
        public BodyTemplate()
        {
            Generation = AppState.GenerationCount++;
            Template = new BlockTemplateCollection(this);
        }

        public int Generation { get; }
        public BlockTemplateCollection Template { get; }
        public Diet Diet { get; set; }

        public float MutationChance { get; set; } = .05f;
        public float MutationNewTraitChance { get; set; } = .5f;
        public float MutationLoseTraitChance { get; set; } = .5f;
        public float MutationChangeDietChance { get; set; } = .5f;

        public bool TryMutate(out System.Guid key)
        {
            // Mutate
            if (Random.value < MutationChance)
            {
                key = System.Guid.NewGuid();

                bool changedDiet = Random.value < MutationChangeDietChance;
                var mutatedTemplate = new BodyTemplate()
                {
                    Diet = changedDiet ?
                        Tools.RandomElement(new Diet[] {
                            Diet.Herbivore,
                            Diet.Carnivore
                            //Diet.Omnivore
                        }.Where(d => d != Diet)) : Diet
                };

                foreach (BlockTemplate block in Template.Values)
                    mutatedTemplate.Template.Add(block.Name, block.Position, block.Rotation, block.MutationChance * .95f);

                // Add block, else change
                bool hasMutation = false;
                if (Random.value < MutationNewTraitChance)
                {
                    hasMutation = mutatedTemplate.Template.TryAddRandom();
                }
                else
                {
                    BlockTemplate oldBlock = Tools.RandomElement(mutatedTemplate.Template.Values);
                    if (Random.value < oldBlock.MutationChance)
                    {
                        Vector3 position = oldBlock.Position;
                        bool isEssential = oldBlock.Sides.FreeSides.Count(f => !f) > 1;

                        mutatedTemplate.Template.Remove(position);

                        if (isEssential || Random.value > MutationLoseTraitChance)
                        {
                            mutatedTemplate.Template.Add(Tools.RandomElement(AppState.BuildingBlocks.Keys), position, Tools.RandomDirection());
                            hasMutation = true;
                        }
                    }
                }

                if (hasMutation || changedDiet)
                {
                    AppState.BodyTemplates.Add(key, mutatedTemplate);
                    Debug.Log("Mutation occured!");

                    return true;
                }
            }

            return false;
        }
    }

    public enum Diet : int
    {
        Herbivore = 0,
        Carnivore = 1,
        Omnivore = 2
    }
}
