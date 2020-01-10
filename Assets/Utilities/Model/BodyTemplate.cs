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
            FoodCount = new Dictionary<string, int>();

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
        public Dictionary<string, int> FoodCount { get; set; }
        public int ChildrenPerLifetime { get; set; } = 10;

        private float _mutationChance, _mutationNewTraitChance, _mutationLoseTraitChance, _mutationChangeDietChance, _mutationChangeChildrenChance;
        public float MutationChance { get => _mutationChance; set => _mutationChance = value > .05f ? value : .05f; }
        public float MutationNewTraitChance { get => _mutationNewTraitChance; set => _mutationNewTraitChance = value > .1f ? value : .1f; }
        public float MutationLoseTraitChance { get => _mutationLoseTraitChance; set => _mutationLoseTraitChance = value > .1f ? value : .1f; }
        public float MutationChangeDietChance { get => _mutationChangeDietChance; set => _mutationChangeDietChance = value > .05f ? value : .05f; }
        public float MutationChangeChildrenChance { get => _mutationChangeChildrenChance; set => _mutationChangeChildrenChance = value > .05f ? value : .05f; }

        public void ResetMutationRates()
        {
            MutationChance = .3f;
            MutationChangeChildrenChance = .5f;
            MutationChangeDietChance = .05f;
            MutationLoseTraitChance = .5f;
            MutationNewTraitChance = .9f;

            foreach (var block in Template)
                block.Value.MutationChance = .9f;
        }

        public bool TryMutate(out System.Guid key)
        {
            // Mutate
            if (Random.value < MutationChance)
            {
                key = System.Guid.NewGuid();

                var mutatedTemplate = new BodyTemplate()
                {
                    Diet = Diet,
                    ChildrenPerLifetime = ChildrenPerLifetime,
                    FoodCount = FoodCount
                };

                bool changedDiet = false;
                if (Random.value < MutationChangeDietChance)
                {
                    mutatedTemplate.Diet = Tools.RandomElement(new Diet[] {
                            Diet.Herbivore,
                            Diet.Carnivore
                            //Diet.Omnivore
                        }.Where(d => d != Diet));
                    mutatedTemplate.FoodCount.Clear();
                }

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

                if (hasMutation || changedDiet || changedChildren)
                {
                    AnimalState.BodyTemplates.Add(key, mutatedTemplate);
                    AnimalState.BodyCollection.Add(key, new ObjectCollection<Body>());

                    return true;
                }
            }

            MutationChance *= .99f;

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
