using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using Assets.Utilities;
using Assets.Utilities.Model;
using Assets.State;

public class SpawnAnimals : MonoBehaviour
{
    public int InitialAnimalCount = 10;

    [Range(0f, 1f)]
    public float InitialMutationChance;

    private int AnimalCount { get => AppState.Registry.Values.OfType<Body>().Count(); }
    private GameObject Template { get; set; }

    public void Awake()
    {
        Template = Resources.Load("AnimalBase") as GameObject;
        Template.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(CreateAnimals());
    }

    private void OnApplicationQuit()
    {
        Template.SetActive(true);
    }

    private IEnumerator ClearBodyTemplates()
    {
        bool doWhile = true;
        while(doWhile)
        {
            System.Guid[] templates = AnimalState.BodyTemplates.Keys
                .ToArray();
            System.Guid[] activeTemplates = AnimalState.BodyCollection
                .Where(a => !a.Value.IsViable())
                .Select(a => a.Key)
                .ToArray();

            foreach(System.Guid templateId in activeTemplates)
            {
                Transform container = AnimalState.BodyTemplates[templateId].Container;
                ObjectCollection<Body> bodies = AnimalState.BodyCollection[templateId];

                AnimalState.BodyTemplates.Remove(templateId);
                AnimalState.BodyCollection.Remove(templateId);

                bodies.DestroyAll();
                Destroy(container.gameObject);

            }

            if (AnimalCount == 0)
                doWhile = false;
            else yield return new WaitForSeconds(10f);
        }
    }

    private IEnumerator CreateAnimals()
    {
        while(true)
        {
            if (AnimalCount == 0)
            {
                AnimalState.BodyTemplates.Clear();
                AnimalState.BodyCollection.Clear();

                System.Guid templateId = System.Guid.NewGuid();
                BodyTemplate template = AnimalState.DefaultTemplate;

                AnimalState.BodyTemplates.Add(templateId, template);
                AnimalState.BodyCollection.Add(templateId, new ObjectCollection<Body>());

                AnimalState.GenerationCount = template.Generation;

                for (int i = 0; i < InitialAnimalCount; i++)
                {
                    template.ResetMutationRates();
                    template.MutationChance = InitialMutationChance;
                    if (template.TryMutate(out System.Guid key))
                        CreateAnimal(key);
                }

                template.ResetMutationRates();

                StartCoroutine(ClearBodyTemplates());
            }

            yield return new WaitUntil(() => AnimalCount == 0);
        }
    }

    private void CreateAnimal(System.Guid templateId)
    { 
        BodyTemplate template = AnimalState.BodyTemplates[templateId];
        bool hydrophobic = !template.Template.Any(b => b.Value.Name == "Membrane");

        if (Tools.TryRandomPosition(hydrophobic ? Habitat.Land : Habitat.Water, out Vector3 position))
        {
            Quaternion rotation = Tools.RandomRotation();

            ObjectCollection<Body> bodies = AnimalState.BodyCollection[templateId];

            GameObject animal;
            Body animalBody;
            if (bodies.Claim(out animalBody))
            {
                animal = animalBody.gameObject;
                animal.transform.position = position;
                animal.transform.rotation = rotation;
            }
            else
            {
                animal = Instantiate(Template, position, rotation, template.Container);
                animalBody = animal.GetComponent<Body>();
                animalBody.Template = templateId;

                bodies.Store(animalBody, true);
            }

            bodies.Use(animalBody);
        }
    }
}
