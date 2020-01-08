using Assets.State;
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
    public Text SelectedInfo;
    public GameObject SelectedInfoPanel;

    public Color defaultColor = Color.black;
    public Color unimportantColor = Color.gray;
    public Color titleColor = Color.black;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    private string Hex(Color color) => $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    private string State(float current, float total) =>
        $"<color={Hex(current / total < .1f ? dangerColor : current / total < .3f ? warningColor : defaultColor)}>{current:N1}</color>";

    // Update is called once per frame
    void FixedUpdate()
    {
        var generations = AnimalState.BodyTemplates
            .Select((v, i) => (
                template: v.Value,
                population: AnimalState.Animals.Where(a => a.body.Template == v.Key &&
                    a.stats.IsAlive).ToArray()
            ))
            .Where(v => v.population.Length > 0)
            .OrderByDescending(v => v.population.Length);

        IEnumerable<string> generationTexts = generations.Where(v => v.population.Length >= 5)
            .Select(v =>
                $"<color={Hex(defaultColor)}>" +
                $"<b><color={Hex(titleColor)}> Generation {v.template.Generation} ({v.template.Diet.ToString()})</color></b>\n" +
                $"  - Population = {v.population.Length}; " +
                $"Size = {v.template.Template.Count}\n" +
                $"  - e={v.population[0].stats.EnergyStorage:F1}; " +
                $"sp={v.population[0].stats.Speed:F1}; " +
                $"s0={v.population[0].stats.Sense:F1}; " +
                $"s1={v.population[0].stats.Sight:F1}\n" +
                $"  - f={(State(v.population.Sum(p => p.stats.Food != Mathf.Infinity ? p.stats.Food : 0f) / v.population.Length, v.population[0].stats.TotalFood)):F2}; " +
                $"o={(State(v.population.Sum(p => p.stats.Oxygen != Mathf.Infinity ? p.stats.Oxygen : 0f) / v.population.Length, v.population[0].stats.TotalOxygen)):F2}; " +
                $"a={(v.population.Sum(p => p.stats.LifeSpan) / v.population.Length):F0}; " +
                $"c={(v.template.ChildrenPerLifetime):F0}" +
                $"m={(v.template.MutationChance):F3}" +
                $"</color>")
            .Union(generations.Where(v => v.population.Length < 5)
            .Select(v =>
                $"<color={Hex(unimportantColor)}>" +
                $"<b>Generation {v.template.Generation} ({v.template.Diet.ToString()})</b>\n" +
                $"  - Population = {v.population.Length}; " +
                $"Size = {v.template.Template.Count}" +
                $"</color>"));

        TemplateInfo.text = string.Join("\n", generationTexts);

        IEnumerable<string> animalTexts = AnimalState.Animals
            .Where(v => v.stats.IsAlive)
            .OrderByDescending(v => v.stats.LifeSpan)
            .Take(10)
            .Select(v =>
                $"<color={Hex(defaultColor)}>" +
                $"- " +
                $"<b><color={Hex(titleColor)}>Food</color></b> {State(v.stats.Food, v.stats.TotalFood)}; " +
                $"<b><color={Hex(titleColor)}>O²</color></b> {State(v.stats.Oxygen, v.stats.TotalOxygen)}; " +
                $"<b><color={Hex(titleColor)}>Age</color></b> {v.stats.LifeSpan:F0} of {v.stats.TotalLifeSpan:F0}" +
                $"</color>");

        AnimalInfo.text = string.Join("\n", animalTexts);

        if (!AppState.Selected)
        {
            SelectedInfoPanel.SetActive(false);
        }
        else
        {
            SelectedInfoPanel.SetActive(true);

            Body selected = AppState.Selected;
            BodyTemplate template = AnimalState.BodyTemplates[selected.Template.Value];
            Vector3 velocity = selected.Rigidbody.velocity;

            SelectedInfo.text =
                $"<color={Hex(defaultColor)}>" +
                $"<b><color={Hex(titleColor)}>Generation</color></b> = {template.Generation:F0}\n" +
                $"<b><color={Hex(titleColor)}>Diet</color></b> = {template.Diet.ToString()}\n" +
                $"<b><color={Hex(titleColor)}>Template:</color></b>\n" +
                string.Join("\n", template.Template.Select(t => $" - {t.Value.Name} [{t.Value.MutationChance}] {t.Value.Position}")) +
                $"\n\n" +
                $"<b><color={Hex(titleColor)}>Food</color></b> = {State(selected.BodyStats.Food, selected.BodyStats.TotalFood)} / {selected.BodyStats.TotalFood:N1}\n" +
                $"<b><color={Hex(titleColor)}>Water</color></b> = {State(selected.BodyStats.Water, selected.BodyStats.TotalWater)} / {selected.BodyStats.TotalWater:N1}\n" +
                $"<b><color={Hex(titleColor)}>Oxygen</color></b> = {State(selected.BodyStats.Oxygen, selected.BodyStats.TotalOxygen)} / {selected.BodyStats.TotalOxygen:N1}\n" +
                $"<b><color={Hex(titleColor)}>Life Span</color></b> = {selected.BodyStats.LifeSpan:N1} / {selected.BodyStats.TotalLifeSpan:N1}\n" +
                $"\n" +
                $"<b><color={Hex(titleColor)}>Food Absorbtion</color></b> = {selected.BodyStats.FoodAbsorbtion:N1}\n" +
                $"<b><color={Hex(titleColor)}>Water Absorbtion</color></b> = {selected.BodyStats.WaterAbsorbtion:N1}\n" +
                $"<b><color={Hex(titleColor)}>Oxygen Absorbtion</color></b> = {selected.BodyStats.OxygenAbsorbtion:N1}\n" +
                $"\n" +
                $"<b><color={Hex(titleColor)}>Position</color></b> = {selected.transform.position:N1}\n" +
                $"<b><color={Hex(titleColor)}>Rotation</color></b> = {selected.transform.rotation.eulerAngles:N1}\n" +
                $"<b><color={Hex(titleColor)}>Velocity</color></b> = {selected.Rigidbody.velocity.magnitude:N1} [{selected.BodyStats.Speed}]\n" +
                $"<b><color={Hex(titleColor)}>Drag</color></b> = {selected.BodyStats.Width:N1}\n" +
                $"<b><color={Hex(titleColor)}>Angular Velocity</color></b> = {selected.Rigidbody.angularVelocity.magnitude:N1}\n" +
                $"<b><color={Hex(titleColor)}>Angular Drag</color></b> = {selected.Rigidbody.angularDrag:N1}\n" +
                $"<b><color={Hex(titleColor)}>Center Of Mass</color></b> = {selected.Rigidbody.centerOfMass:N1}\n" +
                $"\n" +
                $"<b><color={Hex(titleColor)}>In Water</color></b> = {selected.BodyStats.InWater:N1}\n" +
                $"\n" +
                $"<b><color={Hex(titleColor)}>Child Count</color></b> = {selected.BodyStats.ChildCount:F0}\n" +
                $"<b><color={Hex(titleColor)}>Gestation Period</color></b> = {selected.BodyStats.GestationPeriod:N1}\n" +
                $"\n" +
                $"<b><color={Hex(titleColor)}>Energy Storage</color></b> = {selected.BodyStats.EnergyStorage:N1}\n" +
                $"<b><color={Hex(titleColor)}>Sense</color></b> = {selected.BodyStats.Sense:N1}\n" +
                $"<b><color={Hex(titleColor)}>Sight</color></b> = {selected.BodyStats.Sight:N1}\n" +
                $"<b><color={Hex(titleColor)}>Speed</color></b> = {selected.BodyStats.Speed:N1}\n" +
                $"<b><color={Hex(titleColor)}>Strength</color></b> = {selected.BodyStats.Strength:N1}" +
                $"</color>";
        }

    }
}
