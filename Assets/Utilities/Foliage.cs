using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using Assets.Utilities;
using Assets.State;
using Assets.Utilities.Model;

public class Foliage : ObjectBase, IEatable, IAlive
{
    public FoliageType FoliageType;
    public bool SpawnsSeeds;
    public float SpawnDistance;
    public float SpawnTime;

    private IEnumerator SpawnSeeds()
    {
        while(gameObject.activeSelf)
        {
            PlantItem();
            yield return new WaitForSeconds(SpawnTime + Random.Range(SpawnTime * .9f, SpawnTime * 1.1f));
        }
    }

    private void PlantItem()
    {
        if (FoliageState.FoliageCount[FoliageType] <= FoliageState.FoliageLimits[FoliageType])
        {
            ObjectCollection<Foliage> foliageCollection = FoliageState.FoliageCollection[FoliageType];

            if (foliageCollection.Claim(out Foliage foliage, c => c.Habitat == Habitat))
            {
                if (Tools.TryRandomPosition(Habitat, out Vector3 position, transform.position, SpawnDistance))
                {
                    Quaternion rotation = Tools.RandomRotation();

                    if (!Tools.ObjectsInRange(transform.position, SpawnDistance, out ObjectBase[] objects) ||
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

    public float Devour()
    {
        FoliageState.FoliageCollection[FoliageType].Store(this);
        return 1f;
    }

    public void Breathe()
    {
        if (SpawnsSeeds)
            StartCoroutine(SpawnSeeds());
    }
}