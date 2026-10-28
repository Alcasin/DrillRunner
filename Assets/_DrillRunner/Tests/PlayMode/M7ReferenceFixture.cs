using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    internal static class M7ReferenceFixture
    {
        [Serializable] internal sealed class Data { public string Source; public Pattern[] Patterns; public Sequence[] Sequences; }
        [Serializable] internal sealed class Pattern
        {
            public string Id; public int Category, Pressure, MinimumOrdinal, EarlyWeight, Weight, LateWeight; public float BypassX; public Placement[] Obstacles;
        }
        [Serializable] internal sealed class Placement { public string Material; public Vector3 Position, Scale; }
        [Serializable] internal sealed class Sequence { public int Seed; public string[] Ids; }
        internal static Data Read() => JsonUtility.FromJson<Data>(File.ReadAllText("Assets/_DrillRunner/Tests/Fixtures/M7-Reference.json"));
        internal static EncounterLibrary Create(EncounterLibrary current, List<Object> owned)
        {
            var data = Read(); var materials = current.Patterns.SelectMany(p => p.Section.Obstacles).GroupBy(o => o.Definition.MaterialId).ToDictionary(g => g.Key, g => g.First().gameObject);
            var records = new List<EncounterDefinition>(); var index = 0;
            foreach (var record in data.Patterns)
            {
                var source = current.Patterns.Single(p => p.Section.SectionId == record.Id);
                var root = Object.Instantiate(source.Section.gameObject); owned.Add(root); root.name = "Frozen M7 template " + record.Id;
                root.transform.position = new Vector3(0, 0, -1000 - index++ * 80);
                var section = root.GetComponent<TrackSection>(); foreach (var obstacle in section.Obstacles) Object.DestroyImmediate(obstacle.gameObject);
                var targets = record.Obstacles.Select(p =>
                {
                    var target = Object.Instantiate(materials[p.Material], root.transform, false); target.transform.localPosition = p.Position;
                    target.transform.localScale = p.Scale; return target.GetComponent<DrillObstacle>();
                }).ToArray();
                typeof(TrackSection).GetField("obstacles", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(section, targets);
                var definition = Object.Instantiate(source); owned.Add(definition); definition.Section = section; definition.Category = (EncounterCategory)record.Category;
                definition.Pressure = record.Pressure; definition.MinimumOrdinal = record.MinimumOrdinal; definition.EarlyWeight = record.EarlyWeight;
                definition.Weight = record.Weight; definition.LateWeight = record.LateWeight; definition.BypassX = record.BypassX; records.Add(definition);
            }
            var library = Object.Instantiate(current); owned.Add(library); library.Patterns = records.ToArray();
            library.IntroductionSections = 4; library.OpeningBufferSections = 1; library.FirstSelectionCopper = true;
            library.CalmFirstEncounter = false; library.ReliefAfterHighPressure = false; library.ArrangementMemory = 0; library.EmptyRecoverySpacing = 0;
            library.EarlySections = 3; library.RicherFromSection = 20; library.MaximumCopperGap = 3; library.MaximumRecoveryGap = 6;
            library.MaximumConsecutivePressure = 1; library.ReactionSeconds = 0.35f; library.SteeringTolerance = 0.04f; library.StartingOreEnergyBudget = 60;
            return library;
        }
    }
}
