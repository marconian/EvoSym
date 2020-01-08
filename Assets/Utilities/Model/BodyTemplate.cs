using Assets.State;
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
            Generation = AnimalState.GenerationCount++;
            Template = new BlockTemplateCollection(this);

            ResetMutationRates();
        }

        public int Generation { get; }

        private Transform _container;
        public Transform Container 
        { 
            get
            {
                if(_container == null)
                {
                    GameObject parent = GameObject.Find("Animals");
                    GameObject obj = new GameObject($"gen_{Generation}");
                    _container = obj.transform;
                    _container.parent = parent.transform;
                }

                return _container;
            }
        
        }
        public BlockTemplateCollection Template { get; }
        public Diet Diet { get; set; }
        public int ChildrenPerLifetime { get; set; } = 10;

        public float MutationChance { get; set; }
        public float MutationNewTraitChance { get; set; }
        public float MutationLoseTraitChance { get; set; }
        public float MutationChangeDietChance { get; set; }
        public float MutationChangeChildrenChance { get; set; }

        public void ResetMutationRates()
        {
            MutationChance = .5f;
            MutationChangeChildrenChance = .5f;
            MutationChangeDietChance = .2f;
            MutationLoseTraitChance = .5f;
            MutationNewTraitChance = .5f;

            foreach (var block in Template)
                block.Value.MutationChance = .9f;
        }

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
                        }.Where(d => d != Diet)) : Diet,
                    ChildrenPerLifetime = ChildrenPerLifetime
                };

                bool changedChildren = false;
                if (Random.value < MutationChangeChildrenChance)
                {
                    int children = ChildrenPerLifetime + Mathf.RoundToInt(Random.Range(-1.49f, 1.49f));
                    if (children != ChildrenPerLifetime && children >= 1 && children <= 20)
                    {
                        mutatedTemplate.ChildrenPerLifetime = children;
                        changedChildren = true;
                    }
                }

                foreach (BlockTemplate block in Template.Values)
                    mutatedTemplate.Template.Add(block.Name, block.Position, block.Rotation, block.MutationChance * .95f);

                // Add block, else change
                bool hasMutation = false;
                if (Random.value < MutationNewTraitChance)
                {
                    hasMutation = mutatedTemplate.Template.TryAddRandom();
                }
                else if (mutatedTemplate.Template.Count > 1)
                {
                    BlockTemplate oldBlock = Tools.RandomElement(mutatedTemplate.Template.Values);
                    if (Random.value < oldBlock.MutationChance)
                    {
                        Vector3 position = oldBlock.Position;
                        bool isEssential = oldBlock.Sides.FreeSides.Count(f => !f) > 1;

                        mutatedTemplate.Template.Remove(position);

                        if (isEssential || Random.value > MutationLoseTraitChance)
                        {
                            mutatedTemplate.Template.Add(Tools.RandomElement(AnimalState.BuildingBlocks.Keys), position, Tools.RandomDirection());
                            hasMutation = true;
                        }
                    }
                }

                if (!hasMutation)
                {
                    mutatedTemplate.MutationLoseTraitChance *= .95f;
                    mutatedTemplate.MutationNewTraitChance *= .95f;
                }
                if (!changedDiet)
                {
                    mutatedTemplate.MutationChangeDietChance *= .95f;
                }
                if (!changedChildren)
                {
                    mutatedTemplate.MutationChangeChildrenChance *= .95f;
                }

                if (hasMutation || changedDiet || changedChildren)
                {
                    AnimalState.BodyTemplates.Add(key, mutatedTemplate);
                    AnimalState.BodyCollection.Add(key, new ObjectCollection<Body>());

                    return true;
                }
            }

            MutationChance *= .95f;
            MutationLoseTraitChance *= .95f;
            MutationNewTraitChance *= .95f;
            MutationChangeDietChance *= .95f;
            MutationChangeChildrenChance *= .95f;

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
