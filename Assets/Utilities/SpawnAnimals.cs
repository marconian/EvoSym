﻿using UnityEngine;
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
    private GameObject Ground { get => GameObject.Find("Ground"); }

    private void Start()
    {
        StartCoroutine(CreateAnimals());
        StartCoroutine(ClearBodyTemplates());
    }

    private IEnumerator ClearBodyTemplates()
    {
        while(true)
        {
            System.Guid[] templates = AnimalState.BodyTemplates.Keys
                .Skip(1)
                .Where(t => AnimalState.BodyTemplates[t] != null)
                .Take(AnimalState.BodyTemplates.Count - 10)
                .ToArray();
            System.Guid[] activeTemplates = AnimalState.Animals
                .Where(a => a.body.Template.HasValue)
                .Select(a => a.body.Template.Value)
                .Distinct()
                .ToArray();

            foreach(System.Guid template in templates)
            {
                if (!activeTemplates.Contains(template))
                    AnimalState.BodyTemplates.Remove(template);
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
        bool hydrophobic = !AnimalState.BodyTemplates[template].Template.Any(b => b.Value.Name == "Membrane");

        Vector3 position = Tools.RandomPosition(!hydrophobic);
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 359), 0);

        GameObject objectTemplate = Resources.Load("AnimalBase") as GameObject;
        GameObject animal = Instantiate(objectTemplate, position, rotation, transform);
        animal.name = System.Guid.NewGuid().ToString();

        if (animal.TryGetComponent(out Body animalBody))
            animalBody.Template = template;

        if (!AppState.Registry.ContainsKey(animal.name))
            AppState.Registry.Add(animal.name, animal);
    }
}
