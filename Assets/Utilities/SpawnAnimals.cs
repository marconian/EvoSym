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
    public int IntialAnimalCount = 10;

    private int AnimalCount { get => transform.childCount; }
    private GameObject Template { get; set; }

    public void Awake()
    {
        Template = Resources.Load("AnimalBase") as GameObject;
        Template.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(CreateAnimals());
        //StartCoroutine(ClearBodyTemplates());
    }

    private void OnApplicationQuit()
    {
        Template.SetActive(true);
    }

    private IEnumerator ClearBodyTemplates()
    {
        while(true)
        {
            System.Guid[] templates = AnimalState.BodyTemplates.Keys
                .Skip(1)
                .Where(t => AnimalState.BodyTemplates[t] != null)
                .ToArray();
            System.Guid[] activeTemplates = AnimalState.Animals
                .Where(a => a.body.Template.HasValue)
                .Select(a => a.body.Template.Value)
                .Distinct()
                .ToArray();

            foreach(System.Guid template in templates)
            {
                Transform container = AnimalState.BodyTemplates[template].Container;
                AnimalState.BodyTemplates.Remove(template);

                ObjectCollection<Body> bodies = AnimalState.BodyCollection[template];
                bodies.DestroyAll();
                Destroy(container.gameObject);
            }

            yield return new WaitForSeconds(60f);
        }
    }

    private IEnumerator CreateAnimals()
    {
        while(true)
        {
            if (AnimalCount == 0)
            {
                for (int i = 0; i < IntialAnimalCount; i++)
                    CreateAnimal();
            }

            yield return new WaitForSeconds(30f);
        }
    }

    private void CreateAnimal()
    {
        System.Guid template = AnimalState.BodyTemplates.Keys.First();
        BodyTemplate bodyTemplate = AnimalState.BodyTemplates[template];
        bool hydrophobic = !bodyTemplate.Template.Any(b => b.Value.Name == "Membrane");

        if (Tools.TryRandomPosition(hydrophobic ? Habitat.Land : Habitat.Water, out Vector3 position))
        {
            Quaternion rotation = Tools.RandomRotation();

            ObjectCollection<Body> bodies = AnimalState.BodyCollection[template];

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
                animal = Instantiate(Template, position, rotation, bodyTemplate.Container);
                animalBody = animal.GetComponent<Body>();
                animalBody.Template = template;

                bodies.Store(animalBody, true);
            }

            bodies.Use(animalBody);
        }
    }
}
