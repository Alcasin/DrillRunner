using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        private static string RegionalProbe(int stage)=>new[]{"wood","brick","granite","plating","crystal_composite","alloy_core","energy_shard","energy_alloy"}[stage-1];
        private void ConfigureOneUpgrade(int stage,UpgradeCategory category,int level)
        {
            ConfigureAdvanced(PrimaryRegion(stage),stage,0,904);flow.OpenWorkshop();
            for(int i=0;i<level;i++)Assert.That(flow.Purchase(category),Is.True);BeginRegionalAttempt(PrimaryRegion(stage),904);
        }
        private IEnumerator FinishIsolatedContact(DrillObstacle target)
        {
            Launch();int steps=0;while(!target.Durability.IsDestroyed&&motor.Motion.State!=RunState.RunEnded&&steps++<5000)motor.Simulate(1f/120);
            Assert.That(target.Durability.IsDestroyed,Is.True);Assert.That(motor.Diagnostics.DrillingContacts.Single().Completed,Is.True);
            while(motor.Motion.State!=RunState.RunEnded&&steps++<15000)motor.Simulate(0.05f);
            Assert.That(motor.Diagnostics.EndedByEnergy&&flow.IsBanked,Is.True);Assert.That(motor.Diagnostics.BlockedEnergy,Is.Zero);
            Assert.That(motor.Diagnostics.Consumed,Is.EqualTo(motor.Energy.Capacity).Within(0.001));
            motor.UnregisterObstacle(target);Object.Destroy(target.gameObject);flow.OpenWorkshop();yield return null;
        }
        [UnityTest,Timeout(300000)] public IEnumerator FullGameAllAdjacentUpgradesHaveMeasuredRegionalContactOrCoastBenefits()
        {
            var rows=new List<string>{"stage,category,level,material,power,capacity,drillSeconds,drillEnergy,drillRate,activeSeconds,distance"};
            for(int stage=1;stage<=8;stage++)foreach(UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
            {
                double previousTime=double.PositiveInfinity,previousCost=double.PositiveInfinity,previousDistance=0,previousDuration=0;float fixedRate=0;
                for(int level=0;level<=5;level++)
                {
                    ConfigureOneUpgrade(stage,category,level);
                    if(category==UpgradeCategory.DrivePower)
                    {
                        yield return Drive(Policy.Safe);var d=motor.Diagnostics;
                        if(level>0){Assert.That(d.Seconds,Is.EqualTo(previousDuration).Within(0.001));Assert.That(flow.Run.Rewards.Distance,Is.GreaterThan(previousDistance+1));}
                        rows.Add(EconomyCsv(stage,category,level,"bypass",motor.DrillPower,motor.Energy.Capacity,0,0,0,d.Seconds,flow.Run.Rewards.Distance));
                        previousDistance=flow.Run.Rewards.Distance;previousDuration=d.Seconds;flow.OpenWorkshop();
                    }
                    else
                    {
                        var target=IsolateMaterial(RegionalProbe(stage));float rate=Rate(target);yield return FinishIsolatedContact(target);var c=motor.Diagnostics.DrillingContacts.Single();
                        if(level==0)fixedRate=rate;Assert.That(rate,Is.EqualTo(fixedRate),"Purchased Power/Capacity must not raise consumption rates.");
                        if(level>0&&category==UpgradeCategory.DrillPower){Assert.That(c.Seconds,Is.LessThan(previousTime));Assert.That(c.Energy,Is.LessThan(previousCost));}
                        if(level>0&&category==UpgradeCategory.EnergyCapacity)
                        {
                            Assert.That(c.Seconds,Is.EqualTo(previousTime).Within(0.001));Assert.That(c.Energy,Is.EqualTo(previousCost).Within(0.001));
                            Assert.That(motor.Diagnostics.Seconds,Is.GreaterThan(previousDuration));Assert.That(flow.Run.Rewards.Distance,Is.GreaterThan(previousDistance));
                        }
                        rows.Add(EconomyCsv(stage,category,level,c.MaterialId,motor.DrillPower,motor.Energy.Capacity,c.Seconds,c.Energy,rate,motor.Diagnostics.Seconds,flow.Run.Rewards.Distance));
                        previousTime=c.Seconds;previousCost=c.Energy;previousDistance=flow.Run.Rewards.Distance;previousDuration=motor.Diagnostics.Seconds;
                    }
                    File.WriteAllLines("Logs/M10-AdjacentUpgrades.csv",rows);yield return null;
                }
            }
        }
        [UnityTest] public IEnumerator FullGameEvolutionImprovesActualFamiliarMaterialEfficiencyAcrossEveryTransition()
        {
            var rows=new List<string>{"transition,profile,region,material,power,capacity,drillSeconds,drillEnergy"};
            for(int previous=1;previous<8;previous++)
            {
                double seconds=0,energy=0;string region=PrimaryRegion(previous),material=RegionalProbe(previous);
                foreach(int next in new[]{0,1})
                {
                    ConfigureAdvanced(region,previous+next,next==0?5:0,904);var target=IsolateMaterial(material);yield return FinishIsolatedContact(target);var c=motor.Diagnostics.DrillingContacts.Single();
                    rows.Add(EconomyCsv(previous,next==0?"previousMAX":"nextBase",region,material,motor.DrillPower,motor.Energy.Capacity,c.Seconds,c.Energy));
                    if(next==0){seconds=c.Seconds;energy=c.Energy;}else{Assert.That(c.Seconds,Is.LessThan(seconds));Assert.That(c.Energy,Is.LessThan(energy));}
                }
            }
            File.WriteAllLines("Logs/M10-EvolutionEfficiency.csv",rows);
        }
        [UnityTest,Timeout(300000)] public IEnumerator FullGameStrategicRegionReturnsEarnCoinsWithoutBypassingIngredientGates()
        {
            var rows=new List<string>{"family,attempt,stage,region,seek,seconds,coins,copper,iron,quartz,alloyCore,energyShard,purchases,wallet,ready"};
            foreach(int family in new[]{8100,9200,10300})
            {
                memory=new MemoryProgressStore();yield return Load();int attempt=0,purchases=0;
                while(flow.Service.ActiveForm.Stage<8&&attempt<250)
                {
                    int stage=flow.Service.ActiveForm.Stage;string needed=NeededRegion();bool seek=SetResourceGoal(needed);
                    // A disclosed specialization strategy: mine needed ingredients, then choose the measured Coin region.
                    string region=seek?needed:stage>=7?RegionIds.Energy:stage>=3?RegionIds.Quarry:RegionIds.Yard;
                    BeginRegionalAttempt(region,family+ ++attempt);seek=SetResourceGoal(region);
                    if(seek)yield return Drive(Policy.Resource,true);
                    else
                    {
                        Launch();int ticks=0;while(motor.Motion.State!=RunState.RunEnded&&ticks++<12000){aim=ChooseCoinRoute();Steer(aim);Tick(0.05f);if(ticks%250==0)yield return null;}
                        Assert.That(motor.Diagnostics.EndedByEnergy&&flow.IsBanked,Is.True);
                    }
                    var earned=flow.Run.Rewards;flow.OpenWorkshop();int bought=BuyEconomyUpgrades(Policy.Balanced);purchases+=bought;bool ready=flow.Evolution.Ready;
                    rows.Add(EconomyCsv(family,attempt,stage,region,seek,motor.Diagnostics.Seconds,earned.Coins,earned.Copper,earned.Iron,earned.Quartz,earned.AlloyCore,earned.EnergyShard,bought,flow.Service.Snapshot.Coins,ready));File.WriteAllLines("Logs/M10-RegionTrader.csv",rows);
                    if(ready){Assert.That(AllCurrentUpgradesMaximum(),Is.True);Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(0.95f);}yield return null;
                }
                Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(8));Assert.That(purchases,Is.EqualTo(105));Assert.That(flow.IsSandbox,Is.False);
            }
        }
    }
}
