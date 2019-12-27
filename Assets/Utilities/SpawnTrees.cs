using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using Assets.Utilities;

public class SpawnTrees : MonoBehaviour
{
    private IEnumerable<(FoliageType tag, GameObject obj)> FoliageCollection
    {
        get => Resources.LoadAll("Foliage")
            .OfType<GameObject>()
            .Select(f => (
                System.Enum.TryParse(f.name.Split('_')[0], out FoliageType t) ? t : FoliageType.Other, 
                f
            ));
    }

    private GameObject Ground { get => GameObject.Find("Ground"); }

    private int FoliageTotal { get => Trees + Bushes + Plants + Flowers + Grasses + Logs + Rocks; }
    private int FoliageCount { get => transform.childCount; }

    public int Trees;
    public int Bushes;
    public int Plants;
    public int Flowers;
    public int Grasses;
    public int Logs;
    public int Rocks;

    private void Start()
    {
        StartCoroutine(PlantItems());
    }

    private IEnumerator PlantItems()
    {
        while(true)
        {
            var children = transform.OfType<Transform>()
                .Select(c => System.Enum.TryParse(c.tag, out FoliageType t) ? t : FoliageType.Other)
                .ToArray();

            var count = System.Enum.GetValues(typeof(FoliageType))
                .OfType<FoliageType>()
                .ToDictionary(v => v, v => children.Count(c => c == v));

            for (int i = 0; i < Trees - count[FoliageType.Tree]; i++)
                PlantItem(FoliageType.Tree);
            for (int i = 0; i < Bushes - count[FoliageType.Bush]; i++)
                PlantItem(FoliageType.Bush);
            for (int i = 0; i < Plants - count[FoliageType.Plant]; i++)
                PlantItem(FoliageType.Plant);
            for (int i = 0; i < Flowers - count[FoliageType.Flower]; i++)
                PlantItem(FoliageType.Flower);
            for (int i = 0; i < Grasses - count[FoliageType.Grass]; i++)
                PlantItem(FoliageType.Grass);
            for (int i = 0; i < Logs - count[FoliageType.Log]; i++)
                PlantItem(FoliageType.Log);
            for (int i = 0; i < Rocks - count[FoliageType.Rock]; i++)
                PlantItem(FoliageType.Rock);

            yield return new WaitForSeconds(10f);
        }
    }

    private void PlantItem(FoliageType category)
    {
        FoliageType[] allowedUnderWater = new FoliageType[] { 
            FoliageType.Plant,
            FoliageType.Grass,
            FoliageType.Log,
            FoliageType.Rock
        };

        bool underWater = allowedUnderWater.Contains(category);
        Vector3 position = Tools.RandomPosition(underWater);
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 359), 0);

        IEnumerable<GameObject> foliageCollection = FoliageCollection
                .Where(f => f.tag == category)
                .Select(f => f.obj);

        GameObject foliage = Instantiate(Tools.RandomElement(foliageCollection), position, rotation, transform);

        foliage.layer = 8;
        foliage.tag = foliage.name.Split('_')[0];
        foliage.name = System.Guid.NewGuid().ToString();
        foliage.isStatic = true;

        var children = foliage.transform.OfType<Transform>()
            .Select(t => t.gameObject).ToArray();
        foreach (GameObject child in children)
        {
            child.layer = 18;
            child.tag = foliage.tag;
            child.name = foliage.name;
        }

        if (!AppState.Registry.ContainsKey(foliage.name))
            AppState.Registry.Add(foliage.name, foliage);
    }
}

enum FoliageType
{
    Tree,
    Bush,
    Plant,
    Flower,
    Grass,
    Log,
    Rock,
    Other
}