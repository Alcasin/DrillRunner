using System;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DrillRunner.Tests.EditMode
{
    public sealed class MaterialEnergyRevisionTests
    {
        private RegionCatalog Regions=>AssetDatabase.LoadAssetAtPath<RegionCatalog>(RegionSetup.CatalogPath);
        [Test] public void RefineryCostsAreExplicitAndOtherRegionsRetainOriginalRates()
        {
            Regions.Validate();var r=Regions.Find(RegionIds.Advanced);
            Assert.That(r.DrillingCosts.Select(c=>c.MaterialId),Is.EquivalentTo(new[]{"crystal_composite","quartz_ore","alloy_core","energy_alloy"}));
            foreach(var c in r.DrillingCosts)Assert.That(c.Multiplier,Is.EqualTo(c.MaterialId=="crystal_composite"||c.MaterialId=="quartz_ore"?8:9));
            foreach(var id in new[]{RegionIds.Yard,RegionIds.Quarry,RegionIds.Energy})Assert.That(Regions.Find(id).DrillingCosts,Is.Empty,id);
            // The old shared material in Refinery remains a cheap Coin alternative, with its original threshold/HP.
            var old=r.Encounters.Patterns.SelectMany(p=>p.Section.Obstacles).First(o=>o.Definition.MaterialId=="reinforced").Definition;
            Assert.That(old.Hardness,Is.EqualTo(40));Assert.That(old.Durability,Is.EqualTo(620));Assert.That(old.DrillingEnergyPerSecond,Is.EqualTo(32));Assert.That(old.RewardAmount,Is.EqualTo(40));
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(0)][TestCase(-1)][TestCase(31)]
        public void InvalidMaterialCoefficientsAreRejected(float multiplier)=>Assert.Throws<ArgumentException>(()=>MaterialDrillingCost.Validate(new[]{new MaterialDrillingCost("quartz_ore",multiplier)}));
        [Test] public void InvalidCostIdentitiesCannotSilentlyAlterMaterialSelection()
        {
            Assert.Throws<ArgumentException>(()=>MaterialDrillingCost.Validate(null));
            Assert.Throws<ArgumentException>(()=>MaterialDrillingCost.Validate(new[]{new MaterialDrillingCost("",8)}));
            Assert.Throws<ArgumentException>(()=>MaterialDrillingCost.Validate(new[]{new MaterialDrillingCost("quartz_ore",8),new MaterialDrillingCost("quartz_ore",9)}));
            var copy=UnityEngine.Object.Instantiate(Regions.Find(RegionIds.Advanced));
            try{copy.DrillingCosts=new[]{new MaterialDrillingCost("unknown",8)};Assert.Throws<ArgumentException>(copy.Validate);}
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        [TestCase("crystal_composite",38)][TestCase("quartz_ore",30)][TestCase("alloy_core",55)][TestCase("energy_alloy",55)][TestCase("energy_shard",48)]
        public void RawSharedMaterialCostsArePreserved(string id,int raw)
        {
            var material=Regions.Regions.SelectMany(r=>r.Materials).First(m=>m.MaterialId==id);Assert.That(material.DrillingEnergyPerSecond,Is.EqualTo(raw));
        }
        [Test] public void RefineryBalanceSetupIsByteIdempotentAcrossAllAssets()
        {
            var files=Directory.GetFiles("Assets","*",SearchOption.AllDirectories);var bytes=files.Select(File.ReadAllBytes).ToArray();
            AdvancedRegionSetup.ApplyRefineryDrillingCosts();AdvancedRegionSetup.ApplyRefineryDrillingCosts();AdvancedRegionSetup.Setup();
            Assert.That(Directory.GetFiles("Assets","*",SearchOption.AllDirectories),Is.EquivalentTo(files));
            for(int i=0;i<files.Length;i++)Assert.That(File.ReadAllBytes(files[i]),Is.EqualTo(bytes[i]),files[i]);
        }
    }
}
