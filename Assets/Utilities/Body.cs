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
public class Body : MonoBehaviour
{
    public Body()
    {
        ActiveBlocks = new List<BuildingBlock>();
        SensoryData = new Dictionary<BuildingBlock, SensoryData[]>();
        Dangers = new SensoryData[0];
        Obstacles = new SensoryData[0];
        Food = new SensoryData[0];
    }

    private void Start()
    {
        if (Template.HasValue)
        {
            Rigidbody = GetComponent<Rigidbody>();
            Collider = GetComponent<BoxCollider>();

            if (AppState.BodyTemplates[Template.Value].TryMutate(out System.Guid template))
                Template = template;

            BuildTemplate(AppState.BodyTemplates[Template.Value]);

            StartCoroutine(Breathe());
        }
        else
        {
            Debug.LogWarning("No template found!");
            Destroy(gameObject);
        }
    }

    public float AccelerationSpeed = 20f;
    public float Drag = 1f;
    public float RotationSpeed = 2f;
    public float MaxDistanceToEdge = 5f;
    public int HuntingTime = 10;
    public int PopulationLimit = 50;

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

    private IEnumerator Breathe()
    {
        bool status = TryGetComponent(out BodyStats bodyStats);
        while (!status)
            yield return new WaitWhile(() => !TryGetComponent(out BodyStats _));

        BodyStats = bodyStats;
        BodyStats.Wake();

        UpdatePhysics();
        UpdatePosition();

        StartCoroutine(CheckDangers());
        StartCoroutine(CheckObstacles());
        StartCoroutine(CheckFood());
        StartCoroutine(SpawnChild());
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
                .Where(d => d.Distance < 10f);

            Dangers = preditors.Union(environment)
                .ToArray();

            yield return new WaitForSeconds(.2f);
        }
    }

    private IEnumerator CheckFood()
    {
        while (BodyStats.IsAlive)
        {
            Food = SensoryData.Values.SelectMany(v => v)
                .Where(v => v.Subject != null && AppState.Registry.ContainsKey(v.Subject.name))
                .Where(v => CanEat(v.Subject))
                .OrderBy(v => v.Distance)
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
                .Where(d => d.Distance < 10f && !CanEat(d.Subject))
                .Where(d => Mathf.Abs(Vector3.SignedAngle(Vector3.forward, d.Position, Vector3.up)) < 5f)
                .OrderBy(d => d.Distance)
                .ToArray();

            yield return new WaitForSeconds(.2f);
        }
    }

    private bool CanEat(GameObject obj)
    {
        if (AppState.Registry.ContainsKey(obj.name))
        {
            bool isAnimal = obj.TryGetComponent(out Body body);
            switch (AppState.BodyTemplates[Template.Value].Diet)
            {
                case Diet.Herbivore:
                    return !isAnimal;
                case Diet.Carnivore:
                    return isAnimal;
                case Diet.Omnivore:
                    return !isAnimal || body.ActiveBlocks.Count < ActiveBlocks.Count;
                default:
                    throw new System.NotImplementedException();
            }
        }

        return false;
    }

    private void FixedUpdate()
    {
        SensoryData[] dangers = Dangers;
        SensoryData[] obstacles = Obstacles;
        SensoryData focus = Focus;

        if (dangers.Any())
        {
            SensoryData danger = dangers.OrderBy(d => d.Distance).First();
            float distance = danger.Distance;

            Vector3 position = transform.TransformPoint(danger.Position);
            Vector3 direction = (position - transform.position).normalized;
            direction.y = 0f;

            Quaternion rotation = transform.rotation;
            rotation.SetLookRotation(-direction, transform.up);

            float rotationSpeed = RotationSpeed * .1f;
            if (distance < 10f)
                rotationSpeed = rotationSpeed * (10f - distance);

            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.fixedDeltaTime);

            Debug.DrawRay(transform.position, direction * distance, Color.red, Time.fixedDeltaTime);
        }
        else if (focus?.Subject != null)
        {
            float distance = focus.Distance;
            if (distance < 3.5f)
                Eat(focus.Subject);
            else
            {
                Vector3 position = focus.Subject.transform.position;
                Vector3 direction = (position - transform.position).normalized;
                direction.y = 0f;

                Quaternion rotation = transform.rotation;
                rotation.SetLookRotation(direction, transform.up);

                float rotationSpeed = RotationSpeed;
                if (distance < 10f)
                    rotationSpeed = rotationSpeed * (10f - distance);

                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.fixedDeltaTime);

                StartCoroutine(Hunt());

                Debug.DrawRay(transform.position, direction * distance, Color.blue, Time.fixedDeltaTime);
            }
        }

        //if (Obstacles.Any())
        //{
        //    SensoryData obstacle = obstacles.OrderBy(d => d.Distance).First();

        //    Vector3 center = obstacle.Subject.transform.position;
        //    transform.RotateAround(center, transform.forward, RotationSpeed * Time.deltaTime);
        //}

        float maxVelocity = BodyStats?.Speed ?? 0f;
        if (maxVelocity > 0)
        {
            Vector3 velocity = Rigidbody.velocity;
            if (Mathf.Max(velocity.x, velocity.y, velocity.z) < maxVelocity)
            {
                Vector3 direction = transform.forward * AccelerationSpeed;
                Rigidbody.AddForce(direction, ForceMode.Acceleration);
            }
        }

        Vector3 force = -Drag * Rigidbody.velocity.normalized * Rigidbody.velocity.sqrMagnitude;
        Rigidbody.AddForce(force, ForceMode.Force);
    }

    public void OnBlockCollision(BuildingBlock block, GameObject collider)
    {
        if (CanEat(collider))
            Eat(collider);

        //if (Focus != null && collider.transform == Focus)
        //    Eat(Focus.gameObject);
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

    private void Eat(GameObject obj)
    {
        try
        {
            Focus = null;

            if (obj != null)
            {
                if (obj.tag == "Animal")
                {
                    Body prey = obj.GetComponent<Body>();
                    if (prey != null)
                        BodyStats.Food = prey.BodyStats.EnergyStorage / 10;
                }
                else BodyStats.Food = 1;

                AppState.Registry.Remove(obj.name);
                Destroy(obj);
            }
        }
        catch (MissingReferenceException) { }
    }

    private IEnumerator SpawnChild()
    {
        while (BodyStats.IsAlive)
        {
            if (BodyStats.ChildCount > 0 && 
                AppState.Animals.Where(a => a.body.Template == Template).Count() <= PopulationLimit &&
                (AppState.BodyTemplates[Template.Value].Diet == Diet.Carnivore || AppState.ReachedHerbivoreLimit))
            {
                float r = 30;
                float x = Random.Range(-r, r);
                float z = Random.Range(-r, r);

                if (BodyStats.OxygenAbsorbtion == 0)
                {
                    int i = 0;
                    while (AppState.WaterAtPosition(x, z))
                    {
                        x = Random.Range(-r, r);
                        z = Random.Range(-r, r);

                        i++;
                        if (i == 100) break;
                    }
                }

                Vector3 position = transform.TransformPoint(new Vector3(x, 0f, z));
                if (AppState.TryGetHeightAtPosition(position, out float y))
                    position.y = y;

                GameObject objectTemplate = Resources.Load("AnimalBase") as GameObject;
                GameObject child = Instantiate(objectTemplate, position, Quaternion.identity, transform.parent);
                child.name = System.Guid.NewGuid().ToString();

                if (child.TryGetComponent(out Body childBody))
                    childBody.Template = Template;

                if (!AppState.Registry.ContainsKey(child.name))
                    AppState.Registry.Add(child.name, child);

                Debug.Log("A child is born");
            }
            BodyStats.ChildCount += 1;

            yield return new WaitForSeconds(BodyStats.ReproductionRate);
        }
    }

    private void BuildTemplate(BodyTemplate template)
    {
        foreach(KeyValuePair<Vector3, BlockTemplate> blockDescription in template.Template)
            CreateBlock(blockDescription.Value.Name, blockDescription.Key, blockDescription.Value.Rotation);
    }

    private void UpdatePosition()
    {
        Vector3 position = transform.position;
        if (AppState.TryGetHeightAtPosition(position, out float y))
            position.y = y + 1f;

        transform.position = position;
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
        Rigidbody.centerOfMass = center + Vector3.back * .1f;

        Rigidbody.drag = 0f; // Mathf.Sqrt(BodyStats.Width * BodyStats.Height);
        //Rigidbody.angularDrag = 0f;

        Collider.size = new Vector3(BodyStats.Width, BodyStats.Height, BodyStats.Depth);
        Collider.center = center;
    }

    private BuildingBlock CreateBlock(string name, Vector3 position, Vector3 direction)
    {
        if (AppState.BuildingBlocks.ContainsKey(name))
        {
            GameObject gameObject = Instantiate(AppState.BuildingBlocks[name].gameObject, transform.TransformPoint(position), transform.rotation, transform);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.Euler(direction);
            gameObject.name = name;

            if (gameObject.TryGetComponent(out BuildingBlock block))
            {
                ActiveBlocks.Add(block);
                return block;
            }
        }

        return null;
    }
}
