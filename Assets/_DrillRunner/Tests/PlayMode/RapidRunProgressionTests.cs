using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        [UnityTest] public IEnumerator UnfundedShortRunsSupportBothEarlyEvolutionsWithVisiblePurchases()
        {
            var rows = new List<string> { "family,run,stage,seconds,distance,earnedCoins,earnedCopper,purchases,walletCoins,walletCopper" };
            foreach (var family in new[] {8100,9200,10300})
            {
                memory = new MemoryProgressStore(); yield return Load(); var attempts = 0; var evolutionAt = new List<int>(); var purchaseRuns = 0;
                while (flow.Service.ActiveForm.Stage < 3 && attempts < 50)
                {
                    Assert.That(flow.StartRun(), Is.True); motor.GetComponent<DrillPointerInput>().enabled = false;
                    flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = family + ++attempts; hud.Restart(); observedFront = null; aim = 0; blockedAt = -1;
                    var copperNeeded = flow.Service.ActiveForm.RequiredResources.Where(c => c.Resource == EvolutionResource.Copper).Sum(c => c.Amount);
                    yield return Drive(Policy.Balanced, flow.Service.Snapshot.Copper < copperNeeded);
                    var earned = flow.Run.Rewards; Assert.That(flow.OpenWorkshop(), Is.True); var bought = 0;
                    foreach (var category in new[] {UpgradeCategory.EnergyCapacity,UpgradeCategory.DrivePower,UpgradeCategory.DrillPower}) while (flow.Purchase(category)) bought++;
                    if (bought > 0) purchaseRuns++;
                    var p = flow.Service.Snapshot;
                    rows.Add(string.Join(",", new object[] {family,attempts,flow.Service.ActiveForm.Stage,last.Seconds,last.Distance,earned.Coins,earned.Copper,bought,p.Coins,p.Copper}
                        .Select(v => v is IFormattable f ? f.ToString(null,System.Globalization.CultureInfo.InvariantCulture) : v.ToString())));
                    File.WriteAllLines("Logs/M9A-M8R4-EvolutionJourneys.csv", rows);
                    Assert.That(last.Seconds, Is.LessThan(36));
                    if (flow.Evolution.Ready) { evolutionAt.Add(attempts); Assert.That(flow.Evolve(), Is.True); yield return new UnityEngine.WaitForSecondsRealtime(1); }
                }
                Assert.That(flow.Service.ActiveForm.Stage, Is.EqualTo(3)); Assert.That(evolutionAt.Count, Is.EqualTo(2));
                Assert.That(evolutionAt[0], Is.InRange(10,18)); Assert.That(evolutionAt[1] - evolutionAt[0], Is.InRange(10,22));
                Assert.That(purchaseRuns, Is.GreaterThanOrEqualTo(evolutionAt[1] / 2));
                var final = flow.Service.Snapshot; Assert.That(final.Iron + final.Quartz + final.AlloyCore + final.EnergyShard, Is.Zero);
            }
        }
        [UnityTest] public IEnumerator AdjacentUpgradesHaveMeasuredEnduranceDrillingAndLaunchBenefitsWithoutRateRubberBanding()
        {
            var rows = new List<string> { "stage,category,level,seconds,distance,woodDrillSeconds,woodEnergy,movingRate,capacity" };
            for (var stage = 1; stage <= 8; stage++) foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
            {
                double previousTime = 0, previousDistance = 0, previousDrill = double.PositiveInfinity, previousCost = double.PositiveInfinity;
                var expectedRate = flow.Balance.ForForm(ProgressionConfig.FormIds[stage - 1]).Moving;
                for (var level = 0; level <= 5; level++)
                {
                    flow.UseTemporaryStage(stage,1000000,0,0,0,0,0,false);
                    for (var i = 0; i < level; i++) Assert.That(flow.Purchase(category), Is.True);
                    Assert.That(flow.StartRun(), Is.True); motor.GetComponent<DrillPointerInput>().enabled = false;
                    flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = 904; hud.Restart(); observedFront = null; aim = 0;
                    var wood = motor.RegisteredObstacles.First(o => o.Definition.MaterialId == "wood");
                    var time = wood.Definition.Durability / (motor.DrillPower * motor.Drilling.DamagePerPower); var cost = time * Rate(wood);
                    yield return Drive(Policy.Safe);
                    Assert.That(motor.Drilling.MovingEnergyPerSecond, Is.EqualTo(expectedRate));
                    if (level > 0)
                    {
                        if (category == UpgradeCategory.EnergyCapacity) { Assert.That(last.Seconds, Is.GreaterThan(previousTime + 0.1)); Assert.That(last.Distance, Is.GreaterThan(previousDistance + 1)); }
                        if (category == UpgradeCategory.DrivePower) { Assert.That(last.Seconds, Is.EqualTo(previousTime).Within(0.001)); Assert.That(last.Distance, Is.GreaterThan(previousDistance + 1)); }
                        if (category == UpgradeCategory.DrillPower) { Assert.That(time, Is.LessThan(previousDrill)); Assert.That(cost, Is.LessThan(previousCost)); }
                    }
                    rows.Add(string.Join(",",new object[]{stage,category,level,last.Seconds,last.Distance,time,cost,expectedRate,motor.Energy.Capacity}
                        .Select(v=>v is IFormattable f?f.ToString(null,System.Globalization.CultureInfo.InvariantCulture):v.ToString())));
                    previousTime = last.Seconds; previousDistance = last.Distance; previousDrill = time; previousCost = cost;
                    File.WriteAllLines("Logs/M9A-M8R4-Upgrades.csv", rows); yield return null;
                }
            }
        }
    }
}
