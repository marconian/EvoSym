using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using Assets.Utilities;
using Assets.State;

public class ObjectBase : MonoBehaviour
{

    public Habitat Habitat;
    public bool Edible;

    protected virtual void OnAlive() { }
    protected virtual void OnDeath() { }

    private void Awake()
    {
        name = System.Guid.NewGuid().ToString();
    }

    private void OnEnable()
    {
        if (!AppState.Registry.ContainsKey(name))
            AppState.Registry.Add(name, this);

        OnAlive();
    }

    private void OnDisable()
    {
        if (AppState.Registry.ContainsKey(name))
            AppState.Registry.Remove(name);

        if (AppState.Selected?.name == name)
            AppState.Selected = null;

        OnDeath();
    }
}