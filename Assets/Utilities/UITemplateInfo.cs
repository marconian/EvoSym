using Assets.Utilities;
using Assets.Utilities.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UITemplateInfo : MonoBehaviour
{

    public Text TemplateInfo;
    public Text AnimalInfo;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
        IEnumerable<string> texts = AppState.BodyTemplates
            .Select((v, i) => (
                template: v.Value,
                population: AppState.Animals.Where(a => a.body.Template == v.Key && 
                    a.stats.IsAlive).ToArray()
            ))
            .Where(v => v.population.Length > 0)
            .OrderByDescending(v => v.population.Length)
            .Select(v => $"gen {v.template.Generation} [{(int)v.template.Diet}]: " +
                $"p={v.population.Length}; " +
                $"c={v.template.Template.Count};\n" +
                $"  - e={v.population[0].stats.EnergyStorage:F1}; " +
                $"sp={v.population[0].stats.Speed:F1}; " +
                $"s0={v.population[0].stats.Sense:F1}; " +
                $"s1={v.population[0].stats.Sight:F1}\n" +
                $"  - f={(v.population.Sum(p => p.stats.Food != Mathf.Infinity ? p.stats.Food : 0f) / v.population.Length):F2}; " +
                $"w={(v.population.Sum(p => p.stats.Water != Mathf.Infinity ? p.stats.Water : 0f) / v.population.Length):F2}; " +
                $"o={(v.population.Sum(p => p.stats.Oxygen != Mathf.Infinity ? p.stats.Oxygen : 0f) / v.population.Length):F2}; " +
                $"a={(v.population.Sum(p => p.stats.LifeSpan) / v.population.Length):F0}");

        TemplateInfo.text = string.Join("\n", texts);


        IEnumerable<string> animalTexts = AppState.Animals
            .Where(v => v.stats.IsAlive).Take(10)
            .Select(v => $"- f:{v.stats.Food:N1}; " +
            $"w:{v.stats.Water:N1}; " +
            $"o:{v.stats.Oxygen:N1}; " +
            $"a:{v.stats.LifeSpan:F0}/{v.stats.TotalLifeSpan:F0}; " +
            (v.body.Focus != null ? $"d:{v.body.Focus.Distance:F0}" : ""));

        AnimalInfo.text = string.Join("\n", animalTexts);
    }
}
