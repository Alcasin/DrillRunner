using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;

namespace DrillRunner.Tests.EditMode
{
    public sealed class FullGameBalanceTests
    {
        private ProgressionConfig Root=>AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath);
        private RegionCatalog Regions=>AssetDatabase.LoadAssetAtPath<RegionCatalog>(RegionSetup.CatalogPath);
        private static PlayerProgress Profile(int stage)=>new PlayerProgress{FormId=ProgressionConfig.FormIds[stage-1],FormSchema=1,EvolutionSchema=1,
            PowerLevel=5,EnergyLevel=5,DriveLevel=5,WorkshopPowerLevel=stage>=2?5:0,WorkshopEnergyLevel=stage>=2?5:0,WorkshopDriveLevel=stage>=2?5:0,
            AdvancedForms=Enumerable.Range(3,Math.Max(0,stage-2)).Select(s=>new FormUpgradeRecord{FormId=ProgressionConfig.FormIds[s-1],Power=5,Energy=5,Drive=5}).ToArray()};
        [TestCase(1,"Copper:12")][TestCase(2,"Copper:24")][TestCase(3,"Iron:30")][TestCase(4,"Copper:12|Iron:50")]
        [TestCase(5,"Quartz:40")][TestCase(6,"AlloyCore:24|Quartz:20")][TestCase(7,"AlloyCore:12|EnergyShard:30")]
        public void FullGameResourceGatesKeepExactCostsAccessibleSourcesAndNoCircularDependency(int stage,string expected)
        {
            var form=Root.ForForm(ProgressionConfig.FormIds[stage-1]);
            Assert.That(string.Join("|",form.RequiredResources.OrderBy(c=>c.Resource.ToString()).Select(c=>c.Resource+":"+c.Amount)),Is.EqualTo(expected));
            foreach(var cost in form.RequiredResources)
            {
                var source=Regions.Regions.Single(r=>r.Resources.Contains(cost.Resource));Assert.That(source.CanPlay(stage),Is.True);
                var authored=source.Materials.Concat(source.Encounters.Patterns.SelectMany(p=>p.Section.Obstacles).Select(o=>o.Definition));
                Assert.That(authored.Any(m=>(int)m.RewardKind==(int)cost.Resource+1&&m.Hardness<=form.Power.Values.Last()),Is.True);
                var profile=Profile(stage);profile.Coins=100000;profile.Copper=profile.Iron=profile.Quartz=profile.AlloyCore=profile.EnergyShard=100;profile.Spend(new ResourceCost(cost.Resource,100));
                var service=new ProgressionService(new MemoryProgressStore(profile),Root);Assert.That(service.Evolution(true).Ready,Is.False);Assert.That(service.TryEvolve(true,out _),Is.False);
            }
        }
        [TestCase(4,EvolutionResource.Copper,"Workshop Yard")][TestCase(5,EvolutionResource.Quartz,"Crystal Refinery")]
        [TestCase(6,EvolutionResource.AlloyCore,"Crystal Refinery")][TestCase(7,EvolutionResource.AlloyCore,"Crystal Refinery")]
        public void MaxedWorkshopNamesTheActualMissingIngredientSourceWithoutAlteringEligibility(int stage,EvolutionResource missing,string source)
        {
            var p=Profile(stage);p.Copper=p.Iron=p.Quartz=p.AlloyCore=p.EnergyShard=100;p.Spend(new ResourceCost(missing,100));
            var s=new ProgressionService(new MemoryProgressStore(p),Root);var e=s.Evolution(true);var before=ProgressCodec.Encode(s.Snapshot);
            string text=WorkshopView.EvolutionText(e,s.ActiveForm,s.Snapshot,Regions);
            Assert.That(text,Does.Contain("COLLECT"));Assert.That(text,Does.Contain(ResourceCost.Name(missing)));Assert.That(text,Does.Contain(source));Assert.That(text,Does.Not.Contain("MAX all upgrades"));
            Assert.That(e.Ready,Is.False);Assert.That(ProgressCodec.Encode(s.Snapshot),Is.EqualTo(before));Assert.That(text.Count(c=>c=='\n'),Is.EqualTo(e.Description.Count(c=>c=='\n')));
        }
        [Test] public void QuantumAndUnavailableEvolutionGuidanceRemainAuthoritative()
        {
            var p=Profile(8);var s=new ProgressionService(new MemoryProgressStore(p),Root);var e=s.Evolution(true);
            Assert.That(WorkshopView.EvolutionText(e,s.ActiveForm,s.Snapshot,Regions),Is.EqualTo(e.Description));Assert.That(e.Description,Does.Contain("ALL UPGRADES MAX"));
            s=new ProgressionService(new MemoryProgressStore(Profile(5)),Root);e=s.Evolution(false);Assert.That(WorkshopView.EvolutionText(e,s.ActiveForm,s.Snapshot,Regions),Is.EqualTo(e.Description));
        }
        [TestCase(RegionIds.Yard)][TestCase(RegionIds.Quarry)][TestCase(RegionIds.Advanced)][TestCase(RegionIds.Energy)]
        public void ShortRunEncounterPrefixesRemainVariedDeterministicSafeAndResourceBearing(string id)
        {
            var r=Regions.Find(id);var seen=new HashSet<string>();var first=new HashSet<string>();var rows=new List<string>{"region,seed,ordinal,pattern,category,arrangement,pressure,ore,materials"};
            for(int seed=1;seed<=32;seed++)
            {
                var a=new EncounterSequence(seed,r.Encounters,EncounterEnvelope.Accepted);var b=new EncounterSequence(seed,r.Encounters,EncounterEnvelope.Accepted);string previous=null;int gap=0;
                for(int ordinal=0;ordinal<16;ordinal++)
                {
                    var section=a.Next();Assert.That(section.SectionId,Is.EqualTo(b.Next().SectionId));Assert.That(section.SectionId,Is.Not.EqualTo(previous));previous=section.SectionId;seen.Add(section.SectionId);if(ordinal==0)first.Add(section.SectionId);
                    bool ore=section.Obstacles.Any(o=>o.Definition.RewardKind==r.Encounters.ResourceKind);gap=ore?0:gap+1;Assert.That(gap,Is.LessThan(r.Encounters.MaximumCopperGap));
                    var p=a.Selected;Assert.That(p,Is.Not.Null);rows.Add(string.Join(",",id,seed,ordinal,section.SectionId,p.Category,p.Arrangement,p.Pressure,ore,string.Join("|",section.Obstacles.Select(o=>o.Definition.MaterialId))));
                }
                Assert.That(a.RejectedPatterns+a.FallbackCount,Is.Zero);
            }
            Assert.That(seen.Count,Is.EqualTo(r.Encounters.Patterns.Length));Assert.That(first.Count,Is.GreaterThanOrEqualTo(2));
            File.WriteAllLines("Logs/M10-Variety-"+id+".csv",rows);
        }
        [Test] public void FullGameMaterialEffortLedgerUsesAuthoredCostsWithoutMutatingRewards()
        {
            var balance=AssetDatabase.LoadAssetAtPath<RapidRunBalance>(RapidRunSetup.Path);var rows=new List<string>{"stage,level,region,material,hardness,hp,power,capacity,rate,seconds,energy,coins,resource,amount"};
            for(int stage=1;stage<=8;stage++)foreach(int level in new[]{0,5})foreach(var r in Regions.Regions.Where(r=>r.CanPlay(stage)))
            {
                var form=Root.ForForm(ProgressionConfig.FormIds[stage-1]);var consumption=balance.ForForm(form.FormId);
                foreach(var material in r.Materials.Concat(r.Encounters.Patterns.SelectMany(p=>p.Section.Obstacles).Select(o=>o.Definition)).Distinct())
                {
                    var cost=r.DrillingCosts.FirstOrDefault(c=>c.MaterialId==material.MaterialId);double rate=(consumption.DrillingBase+material.DrillingEnergyPerSecond*consumption.MaterialScale)*(cost.MaterialId==null?1:cost.Multiplier);
                    double time=material.Durability/(form.Power.Values[level]*10.0);bool drillable=material.Hardness<=form.Power.Values[level];long coins=material.RewardKind==ObstacleRewardKind.Coins?material.RewardAmount*balance.Rewards.ObstacleCoinMultiplier:0;
                    rows.Add(string.Join(",",new object[]{stage,level,r.Id,material.MaterialId,material.Hardness,material.Durability,form.Power.Values[level],form.Energy.Values[level],rate,drillable?time:-1,drillable?time*rate:-1,coins,material.RewardKind,material.RewardAmount}.Select(v=>v is IFormattable f?f.ToString(null,System.Globalization.CultureInfo.InvariantCulture):v.ToString())));
                }
            }
            File.WriteAllLines("Logs/M10-MaterialEffort.csv",rows);
        }
    }
}
