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

    protected override void OnAlive()
    {
        base.OnAlive();
        Height = transform.GetComponentsInChildren<MeshFilter>()
            .Max(f => f.transform.localPosition.y + f.sharedMesh.vertices.Max(v => v.y));
    }

    public float Height { get; private set; }

    private bool isNew = true;
    private IEnumerator SpawnSeeds()
    {
        while(gameObject.activeSelf)
        {
            int limit = FoliageState.FoliageLimits[FoliageType];
            var coll = FoliageState.FoliageCollection[FoliageType];

            if (isNew) isNew = !isNew;
            else if (coll.UseCount >= limit)
                yield return new WaitUntil(() => coll.UseCount < limit);
            else PlantItem();

            yield return new WaitForSeconds(Random.Range(SpawnTime * .8f, SpawnTime * 1.2f));
        }
    }

    private void PlantItem()
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