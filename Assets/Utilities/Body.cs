using Assets.State;
using Assets.Utilities;
using Assets.Utilities.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(BodyStats))]
public class Body : ObjectBase, IEatable, IAlive
{
    public Body()
    {
        ActiveBlocks = new List<BuildingBlock>();
        SensoryData = new Dictionary<BuildingBlock, SensoryData[]>();
        Dangers = new SensoryData[0];
        Obstacles = new SensoryData[0];
        Food = new SensoryData[0];
    }

    public float AccelerationSpeed = 18f;
    public float RotationSpeed = .5f;
    public float AvoidObstacleSpeed = 15f;
    public float ChildSpawningDistance = 10f;
    public int HuntingTime = 10;
    public int PopulationLimit = 50;
    public float HorizontalRotationLimit = 10f;

    public System.Guid? Template { get; set; }


    public Dictionary<BuildingBlock, SensoryData[]> SensoryData { get; private set; }
    public SensoryData[] Dangers { get; private set; }
    public SensoryData[] Obstacles { get; private set; }
    public SensoryData[] Food { get; private set; }

    private SensoryData _focus = null;
    public SensoryData Focus
    {
        get
        {
            if (Dangers.Any())
                _focus = null;

            return _focus;
        }
        set
        {
            if (value == null || !Dangers.Any())
                _focus = value;
        }
    }

    public Rigidbody Rigidbody { get; private set; }
    public BoxCollider Collider { get; private set; }

    public List<BuildingBlock> ActiveBlocks { get; }
    public BodyStats BodyStats { get; set; }

    override protected void OnAlive()
    {
        base.OnAlive();

        Rigidbody = GetComponent<Rigidbody>();
        Collider = GetComponent<BoxCollider>();
    }

    public void Breathe()
    {
        if (Template.HasValue)
        {
            ObjectCollection<Body> coll = AnimalState.BodyCollection[Template.Value];
            BodyTemplate bodyTemplate = AnimalState.BodyTemplates[Template.Value];
            if (coll.Count > 1 && bodyTemplate.TryMutate(out System.Guid template))
            {
                coll.Extract(this);

                Template = template;

                bodyTemplate = AnimalState.BodyTemplates[template];
                coll = AnimalState.BodyCollection[template];
                coll.Store(this, true, false);

                transform.parent = bodyTemplate.Container;
                coll.Use(this);
            }
            else
            {
                BuildTemplate(bodyTemplate);

                BodyStats = GetComponent<BodyStats>();
                BodyStats.Wake();

                UpdatePhysics();
                UpdatePosition();

                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(CheckDangers());
                    StartCoroutine(CheckObstacles());
                    StartCoroutine(CheckFood());
                    StartCoroutine(SpawnChild());
                }
            }
        }
        else
        {
            Debug.LogWarning("No template found!");
            Destroy(gameObject);
        }
    }

    private IEnumerator CheckDangers()
    {
        while(BodyStats.IsAlive)
        {
            var preditors = SensoryData.Values.SelectMany(v => v)
                .Where(d => d.SensoryType == SensoryType.Animal && d.Subject != null)
                .Where(d => d.Subject.GetComponent<Body>().ActiveBlocks.Count > ActiveBlocks.Count)
                .Where(d => d.Distance < 20f);
            var environment = SensoryData.Values.SelectMany(v => v)
                .Where(d => d.SensoryType == SensoryType.Environment && d.Subject != null)
                .Where(d => d.Distance < 15f)
                .OrderBy(d => BodyStats.Hydrophobic ? d.Position.y : -d.Position.y);

            Dangers = preditors.Union(environment)
                .ToArray();

            yield return new WaitForSeconds(.2f);
        }
    }

    private IEnumerator CheckFood()
    {
        while (BodyStats.IsAlive)
        {
            Dictionary<string, int> foodCount = AnimalState.BodyTemplates[Template.Value].FoodCount;
            Food = SensoryData.Values.SelectMany(v => v)
                .Where(v => v.Subject != null && AppState.Registry.ContainsKey(v.Subject.name) && CanEat(v.Subject, out IEatable _))
                .OrderByDescending(v =>
                {
                    ObjectBase obj = AppState.Registry[v.Subject.name];
                    string tag = "unknown";
                    if (obj is Foliage foliage)
                        tag = foliage.FoliageType.ToString();
                    else if (obj is Body body)
                        tag = body.Template.Value.ToString();

                    return foodCount.ContainsKey(tag) ? foodCount[tag] : 0;
                })
                .ThenBy(v => v.Distance)
                .ToArray();

            if (Focus?.Subject == null && Food.Any())
            {
                Focus = Food.First();
            }

            yield return new WaitForSeconds(.2f);
        }
    }

    private IEnumerator CheckObstacles()
    {
        while (BodyStats.IsAlive)
        {
            Obstacles = SensoryData.Values.SelectMany(v => v)
                .Where(d => d.Subject != null && AppState.Registry.ContainsKey(d.Subject.name))
                .Where(d => d.Distance < 20f && !CanEat(d.Subject, out IEatable _))
                .Where(d => Mathf.Abs(Vector3.SignedAngle(Vector3.forward, d.Position, Vector3.up)) < 90f)
                .OrderBy(d => d.Distance)
                .ToArray();

            yield return new WaitForSeconds(.2f);
        }
    }

    private bool CanEat(GameObject obj, out IEatable eatable)
    {
        if (AppState.Registry.ContainsKey(obj.name))
        {
            bool hydrophobic = BodyStats.Hydrophobic;
            if (hydrophobic && obj.transform.position.y > TerrainState.WaterLevel ||
                    !hydrophobic && obj.transform.position.y < TerrainState.WaterLevel)
            {
                ObjectBase objectBase = AppState.Registry[obj.name];
                if (objectBase.Edible && objectBase is IEatable e)
                {
                    eatable = e;

                    Diet diet = AnimalState.BodyTemplates[Template.Value].Diet;
                    if (diet != Diet.Carnivore && objectBase is Foliage foliage)
                    {
                        return true; // BodyStats.Height >= foliage.Height * .5f;
                    }
                    else if (diet != Diet.Herbivore && objectBase is Body body)
                    {
                        if (body.Template != Template && body.ActiveBlocks.Count <= ActiveBlocks.Count)
                            return true;
                    }
                }
            }
        }

        eatable = null;
        return false;
    }

    private void FixedUpdate()
    {
        SensoryData[] dangers = Dangers;
        SensoryData[] obstacles = Obstacles;
        SensoryData focus = Focus;

        if (dangers.Any())
            MoveFrom(dangers);
        else if (obstacles.Any())
            MoveFrom(obstacles);
        else if (focus?.Subject != null)
        {
            if (focus.Distance < 2f && focus is IEatable eatable)
                Eat(eatable);
            else
            {
                MoveTo(focus);
                StartCoroutine(Hunt());
            }
        }

        float velocity = Rigidbody.velocity.magnitude;
        float maxVelocity = BodyStats.Speed;
        if (maxVelocity > 0 && velocity < maxVelocity)
        {
            float acceleration = AccelerationSpeed * (1 - (Mathf.Sqrt(velocity / maxVelocity) + Mathf.Epsilon));
            Vector3 direction = transform.forward * acceleration;
            Rigidbody.AddForce(direction, ForceMode.Acceleration);

            Vector3 dragForce = -BodyStats.Width * Rigidbody.velocity.normalized * Rigidbody.velocity.sqrMagnitude;
            Rigidbody.AddForce(dragForce, ForceMode.Force);

            Vector3 downwardForce = -transform.up.normalized * Rigidbody.velocity.sqrMagnitude;
            Rigidbody.AddForce(downwardForce, ForceMode.Force);
        }

        LockRotation(HorizontalRotationLimit);
    }

    private void LockRotation(float angle)
    {
        float lower = angle % 360;
        float upper = (360 - angle) % 360;

        Vector3 angles = transform.rotation.eulerAngles;

        if (angles.x > 180f && angles.x < upper)
            angles.x = upper;
        else if (angles.x < 180f && angles.x > lower)
            angles.x = lower;

        if (angles.z > 180f && angles.z < upper)
            angles.z = upper;
        else if (angles.z < 180f && angles.z > lower)
            angles.z = lower;

        transform.rotation = Quaternion.Euler(angles);
    }

    private void MoveTo(SensoryData data) => Move(data, to: data.Subject.transform.position);
    private void MoveFrom(SensoryData[] data) => Move(data.OrderBy(d => d.Distance).First(), .1f, -1f);
    private void Move(SensoryData data, float speed = 1f, float offset = 1f, Vector3 to = default)
    {
        float distance = data.Distance;
        Vector3 position = to != default ? to : transform.TransformPoint(data.Position);
        Vector3 direction = (position - transform.position).normalized;
        direction.y = 0f;

        Quaternion rotation = transform.rotation;
        rotation.SetLookRotation(direction * offset, transform.up);

        float rotationSpeed = RotationSpeed * speed;
        if (distance < 10f)
            rotationSpeed = rotationSpeed * (10f - distance);

        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.fixedDeltaTime);

        Color rayColor = offset > 0f ? Color.blue : Color.red;
        Debug.DrawRay(transform.position, direction * distance, rayColor, Time.fixedDeltaTime);
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AppState.Selected = this;
        }
    }

    public void OnBlockCollision(BuildingBlock block, GameObject obj)
    {
        if (CanEat(obj, out IEatable eatable))
            Eat(eatable);
    }

    public void OnSensedObjects(BuildingBlock block, SensoryData[] data)
    {
        if (!SensoryData.ContainsKey(block))
            SensoryData.Add(block, data);
        else SensoryData[block] = data;
    }

    private IEnumerator Hunt()
    {
        int hunting = HuntingTime + 1;
        while(Focus != null && hunting > 0)
        {
            hunting--;
            yield return new WaitForSeconds(1f);
        }

        Focus = null;
    }

    private void Eat(IEatable eatable)
    {
        try
        {
            Focus = null;

            if (eatable != null)
            {
                BodyStats.Food = eatable.Devour();

                string tag = "unknown";
                if (eatable is Foliage foliage)
                    tag = foliage.FoliageType.ToString();
                else if (eatable is Body body)
                    tag = body.Template.Value.ToString();

                BodyTemplate template = AnimalState.BodyTemplates[Template.Value];
                if (!template.FoodCount.ContainsKey(tag))
                    template.FoodCount.Add(tag, 1);
                else template.FoodCount[tag]++;
            }
        }
        catch (MissingReferenceException) { }
    }

    private IEnumerator SpawnChild()
    {
        while (BodyStats.IsAlive)
        {
            if (BodyStats.Reproduce &&
                AnimalState.Animals.Where(a => a.body.Template == Template).Count() <= PopulationLimit &&
                (AnimalState.BodyTemplates[Template.Value].Diet == Diet.Carnivore || !AnimalState.ReachedHerbivoreLimit) &&
                Tools.TryRandomPosition(Habitat, out Vector3 position, transform.position, ChildSpawningDistance))
            {
                ObjectCollection<Body> bodies = AnimalState.BodyCollection[Template.Value];

                Body childBody;
                if (bodies.Claim(out childBody))
                {
                    childBody.transform.position = position;
                    childBody.transform.rotation = Quaternion.identity;

                }
                else
                {
                    GameObject objectTemplate = Resources.Load("AnimalBase") as GameObject;
                    GameObject child = Instantiate(objectTemplate, position, Quaternion.identity, transform.parent);
                    childBody = child.GetComponent<Body>();
                    childBody.Template = Template;

                    bodies.Store(childBody, true);
                }

                bodies.Use(childBody);

                Debug.Log("A child is born");
                BodyStats.ChildCount++;
            }

            yield return new WaitUntil(() => BodyStats.Reproduce);
        }
    }

    private void BuildTemplate(BodyTemplate template)
    {
        Habitat = !template.Template.Any(b => b.Value.Name == "Membrane") ? Habitat.Land : Habitat.Water;

        foreach(BuildingBlock block in ActiveBlocks.ToArray())
            block.gameObject.SetActive(false);

        List<BuildingBlock> available = ActiveBlocks.ToList();
        ActiveBlocks.Clear();

        foreach (KeyValuePair<Vector3, BlockTemplate> blockDescription in template.Template)
            CreateBlock(blockDescription.Value.Name, blockDescription.Key, blockDescription.Value.Rotation, ref available);

        foreach (BuildingBlock block in available)
            Destroy(block);
    }

    private void UpdatePosition()
    {
        Vector3 position = transform.position;
        position.y += 1f;
        transform.position = position;

        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.x = 0f;
        rotation.z = 0f;
        transform.rotation = Quaternion.Euler(rotation);
    }

    private void UpdatePhysics()
    {
        Vector3[] positions = ActiveBlocks.Select(b => b.transform.localPosition).ToArray();

        float xmin = positions.Min(p => p.x);
        float ymin = positions.Min(p => p.y);
        float zmin = positions.Min(p => p.z);
        float xmax = positions.Max(p => p.x);
        float ymax = positions.Max(p => p.y);
        float zmax = positions.Max(p => p.z);

        Vector3 center = new Vector3(
            x: xmin + (xmax - xmin) / 2f,
            y: ymin + (ymax - ymin) / 2f,
            z: zmin + (zmax - zmin) / 2f
        );

        Rigidbody.mass = 3; // BodyStats.Mass;
        Rigidbody.centerOfMass = center + Vector3.back * 1f;

        Collider.size = new Vector3(BodyStats.Width, BodyStats.Height, BodyStats.Depth);
        Collider.center = center;
    }

    private BuildingBlock CreateBlock(string name, Vector3 position, Vector3 direction, ref List<BuildingBlock> available)
    {
        BuildingBlock block = null;
        GameObject obj = null;
        if (available != null && available.Any(v => v.name == name))
        {
            block = available.First(v => v.name == name);
            obj = block.gameObject;

            available.Remove(block);
        }
        else if (AnimalState.BuildingBlocks.ContainsKey(name))
        {
            obj = Instantiate(AnimalState.BuildingBlocks[name].gameObject, transform.TransformPoint(position), transform.rotation, transform);
            obj.name = name;
            block = obj.GetComponent<BuildingBlock>();
        }

        if (obj != null && block != null)
        {
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(direction);

            ActiveBlocks.Add(block);
            obj.SetActive(true);

            return block;
        }

        return null;
    }

    public float Devour()
    {
        ObjectCollection<Body> coll = AnimalState.BodyCollection[Template.Value];
        coll.Store(this);

        if (!coll.IsViable())
            coll.DestroyAll();
        return BodyStats.EnergyStorage;
    }
}
