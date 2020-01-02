using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using Assets.Utilities;
using Assets.State;

public class SpawnTrees : MonoBehaviour
{

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
        FoliageState.FoliageLimits[FoliageType.Tree] = Trees;
        FoliageState.FoliageLimits[FoliageType.Bush] = Bushes;
        FoliageState.FoliageLimits[FoliageType.Plant] = Plants;
        FoliageState.FoliageLimits[FoliageType.Flower] = Flowers;
        FoliageState.FoliageLimits[FoliageType.Grass] = Grasses;
        FoliageState.FoliageLimits[FoliageType.Log] = Logs;
        FoliageState.FoliageLimits[FoliageType.Rock] = Rocks;

        StartCoroutine(PlantItems());
    }

    private IEnumerator PlantItems()
    {
        while (true)
        {
            foreach (FoliageType foliageType in System.Enum.GetValues(typeof(FoliageType)).OfType<FoliageType>()
                .Where(t => FoliageState.FoliageCollection.ContainsKey(t)))
            {
                for (int i = 0; i < FoliageState.FoliageLimits[foliageType] - FoliageState.FoliageCount[foliageType]; i++)
                    PlantItem(foliageType);
            }

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

        if (Physics.OverlapSphere(position, 50f, LayerMask.GetMask("Foliage")).Length < 10)
        {
            IEnumerable<GameObject> foliageCollection = FoliageState.FoliageCollection[category];

            GameObject obj = Instantiate(Tools.RandomElement(foliageCollection), position, rotation, transform);

            obj.layer = 8;
            obj.tag = obj.name.Split('_')[0];
            obj.name = System.Guid.NewGuid().ToString();
            obj.isStatic = true;

            var children = obj.transform.OfType<Transform>()
                .Select(t => t.gameObject).ToArray();
            foreach (GameObject child in children)
            {
                child.layer = 18;
                child.tag = obj.tag;
                child.name = obj.name;
            }

            Foliage foliage = obj.AddComponent<Foliage>();
            foliage.FoliageType = category;

            if (!AppState.Registry.ContainsKey(obj.name))
                AppState.Registry.Add(obj.name, obj);
        }
    }
}

public enum FoliageType
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