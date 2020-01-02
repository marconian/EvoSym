﻿using Assets.State;
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

            if (AnimalState.BodyTemplates[Template.Value].TryMutate(out System.Guid template))
                Template = template;

            BuildTemplate(AnimalState.BodyTemplates[Template.Value]);

            StartCoroutine(Breathe());
        }
        else
        {
            Debug.LogWarning("No template found!");
            Destroy(gameObject);
        }
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
                .Where(d => d.Distance < 20f && !CanEat(d.Subject))
                .Where(d => Mathf.Abs(Vector3.SignedAngle(Vector3.forward, d.Position, Vector3.up)) < 90f)
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
            switch (AnimalState.BodyTemplates[Template.Value].Diet)
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
            MoveFrom(dangers);
        else if (obstacles.Any())
            MoveFrom(obstacles);
        else if (focus?.Subject != null)
        {
            if (focus.Distance < 3.5f)
                Eat(focus.Subject);
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
            if (BodyStats.Reproduce &&
                AnimalState.Animals.Where(a => a.body.Template == Template).Count() <= PopulationLimit &&
                (AnimalState.BodyTemplates[Template.Value].Diet == Diet.Carnivore || !AnimalState.ReachedHerbivoreLimit))
            {
                float r = ChildSpawningDistance;
                float x = Random.Range(-r, r);
                float z = Random.Range(-r, r);

                bool inwater = BodyStats.InWater;

                int i = 0;
                while (BodyStats.Hydrophobic ? inwater : !inwater)
                {
                    x = Random.Range(-r, r);
                    z = Random.Range(-r, r);
                    inwater = TerrainState.WaterAtPosition(x, z);

                    i++;
                    if (i == 100) break;
                }

                Vector3 position = transform.TransformPoint(new Vector3(x, 0f, z));
                if (TerrainState.TryGetHeightAtPosition(position, out float y))
                    position.y = y;

                GameObject objectTemplate = Resources.Load("AnimalBase") as GameObject;
                GameObject child = Instantiate(objectTemplate, position, Quaternion.identity, transform.parent);
                child.name = System.Guid.NewGuid().ToString();

                if (child.TryGetComponent(out Body childBody))
                    childBody.Template = Template;

                if (!AppState.Registry.ContainsKey(child.name))
                    AppState.Registry.Add(child.name, child);

                Debug.Log("A child is born");
                BodyStats.ChildCount += 1;
            }

            yield return new WaitUntil(() => BodyStats.Reproduce);
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
        if (TerrainState.TryGetHeightAtPosition(position, out float y))
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
        Rigidbody.centerOfMass = center + Vector3.back * 1f;

        Collider.size = new Vector3(BodyStats.Width, BodyStats.Height, BodyStats.Depth);
        Collider.center = center;
    }

    private BuildingBlock CreateBlock(string name, Vector3 position, Vector3 direction)
    {
        if (AnimalState.BuildingBlocks.ContainsKey(name))
        {
            GameObject gameObject = Instantiate(AnimalState.BuildingBlocks[name].gameObject, transform.TransformPoint(position), transform.rotation, transform);
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
