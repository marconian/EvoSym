using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using Assets.Utilities;
using Assets.State;
using Assets.Utilities.Model;

public class SpawnTrees : MonoBehaviour
{
    public int FoliageCount;
    public int SpawnOnStart;

    public FoliageSetting[] Settings;

    private void Awake()
    {
        var foliageResources = Resources.LoadAll("Foliage")
            .OfType<GameObject>();
        foreach (var r in foliageResources)
            r.SetActive(false);
    }

    private void Start()
    {
        float sizeSum = Settings.Sum(s => s.Size);

        foreach (FoliageSetting setting in Settings)
        {
            FoliageState.FoliageLimits[setting.Type] = Mathf.RoundToInt(FoliageCount * ((setting.Size / sizeSum) * 1f));
            IEnumerable<GameObject> foliageResources = FoliageState.FoliageResources[setting.Type];
            ObjectCollection<Foliage> foliageCollection = FoliageState.FoliageCollection[setting.Type];

            for (int i = 0; i < FoliageState.FoliageLimits[setting.Type]; i++)
            {
                GameObject template = Tools.RandomElement(foliageResources);
                GameObject obj = Instantiate(template, Vector3.zero, Quaternion.identity, transform);
                if (obj.TryGetComponent(out Foliage foliage))
                    foliageCollection.Store(foliage);
            }
        }

        StartCoroutine(PlantItems());
    }

    private void OnApplicationQuit()
    {
        var foliageResources = Resources.LoadAll("Foliage")
            .OfType<GameObject>();
        foreach (var r in foliageResources)
            r.SetActive(true);
    }

    private IEnumerator PlantItems()
    {
        while (true)
        {
            foreach (FoliageType foliageType in System.Enum.GetValues(typeof(FoliageType)).OfType<FoliageType>()
                .Where(t => FoliageState.FoliageResources.ContainsKey(t)))
            {
                if (FoliageState.FoliageCount[foliageType] < SpawnOnStart)
                {
                    int noToPlant = SpawnOnStart - FoliageState.FoliageCount[foliageType];
                    for (int i = 0; i < noToPlant; i++)
                        PlantItem(foliageType);
                }
            }

            yield return new WaitForSeconds(60f);
        }
    }

    private void PlantItem(FoliageType category)
    {
        ObjectCollection<Foliage> foliageCollection = FoliageState.FoliageCollection[category];
        if (foliageCollection.Claim(out Foliage foliage))
        {
            if (Tools.TryRandomPosition(foliage.Habitat, out Vector3 position))
            {
                Quaternion rotation = Tools.RandomRotation();

                if (!Tools.ObjectsInRange(position, foliage.SpawnDistance, out ObjectBase[] objects) ||
                    objects.OfType<Foliage>().Count() < 10)
                {
                    foliage.transform.position = position;
                    foliage.transform.rotation = rotation;

                    foliageCollection.Use(foliage);
                    return;
                }
            }

            foliageCollection.Release(foliage);
        }
    }
}