using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        private static string EconomyCsv(params object[] values) => string.Join(",", values.Select(v => v is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : v.ToString()));
        private bool AllCurrentUpgradesMaximum() => Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>().All(c => flow.Service.Snapshot.Level(c) == flow.Service.ActiveForm.Table(c).MaximumLevel);
        private int AffordableUpgradeCount() => Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>().Count(c => {
            var level = flow.Service.Snapshot.Level(c); var table = flow.Service.ActiveForm.Table(c);
            return level < table.MaximumLevel && table.Costs[level] <= flow.Service.Snapshot.Coins;
        });
        private int BuyEconomyUpgrades(Policy policy)
        {
            var order = policy == Policy.Power ? new[]{UpgradeCategory.DrillPower,UpgradeCategory.EnergyCapacity,UpgradeCategory.DrivePower} :
                policy == Policy.Resource ? new[]{UpgradeCategory.EnergyCapacity,UpgradeCategory.DrillPower,UpgradeCategory.DrivePower} :
                new[]{UpgradeCategory.EnergyCapacity,UpgradeCategory.DrivePower,UpgradeCategory.DrillPower};
            int count = 0;
            while (AffordableUpgradeCount() > 0)
            {
                var choices = order.Where(c => flow.Service.Snapshot.Level(c) < flow.Service.ActiveForm.Table(c).MaximumLevel &&
                    flow.Service.ActiveForm.Table(c).Costs[flow.Service.Snapshot.Level(c)] <= flow.Service.Snapshot.Coins);
                var category = policy == Policy.Balanced ? choices.OrderBy(c => flow.Service.ActiveForm.Table(c).Costs[flow.Service.Snapshot.Level(c)]).First() : choices.First();
                Assert.That(flow.Purchase(category), Is.True); count++;
            }
            return count;
        }
        [UnityTest] public IEnumerator CoinCandidatesUseIdenticalGameplayAndSeparateEveryRewardSource()
        {
            var rows = new List<string>{Sample.Header + ",distanceRate,powerLevel,driveLevel"};
            var profiles = new[]{new[]{1,0,0,0},new[]{1,3,3,0},new[]{1,5,5,5},new[]{2,0,0,0},new[]{2,2,2,2},new[]{2,5,5,5},
                new[]{3,0,0,0},new[]{3,5,5,5},new[]{4,2,2,2},new[]{5,2,2,2},new[]{6,2,2,2},new[]{7,2,2,2},new[]{8,2,2,2}};
            var paired = new Dictionary<string,Sample>();
            foreach (int rate in new[]{12,6,7,8}) foreach (var p in profiles) foreach (int seed in new[]{1337,904,2026}) foreach (Policy policy in Enum.GetValues(typeof(Policy)))
            {
                flow.Balance.Rewards.CoinsPerPayout = rate;
                ConfigureLevels(p[0],p[1],p[2],p[3],seed); yield return Drive(policy);
                Assert.That(last.Coins, Is.EqualTo(last.DistanceCoins + last.ObstacleCoins));
                Assert.That(last.DistanceCoins, Is.EqualTo((long)Math.Floor(last.Distance / 5 + 1e-9) * rate));
                var key = EconomyCsv(p[0],p[1],p[2],p[3],seed,policy);
                if (rate == 12) paired[key] = last;
                else
                {
                    var previous = paired[key]; Assert.That(last.Seconds, Is.EqualTo(previous.Seconds).Within(0.0001));
                    Assert.That(last.Distance, Is.EqualTo(previous.Distance).Within(0.0001));
                    Assert.That(last.DrillEnergy, Is.EqualTo(previous.DrillEnergy).Within(0.0001));
                    Assert.That(last.ObstacleCoins, Is.EqualTo(previous.ObstacleCoins)); Assert.That(last.Copper, Is.EqualTo(previous.Copper));
                }
                rows.Add(last.Csv("M8R4-Candidate") + EconomyCsv("",rate,p[1],p[3]));
                File.WriteAllLines("Logs/M9A-M8R4-EconomyCandidates.csv",rows); yield return null;
            }
        }
        [UnityTest, Timeout(600000)] public IEnumerator FreshEconomyCandidateJourneysMeasureAffordabilitySavingsAndBothEvolutions()
        {
            var rows = new List<string>{"rate,family,strategy,actualPolicy,attempt,stage,powerLevel,capacityLevel,driveLevel,seconds,distance,distanceCoins,obstacleCoins,earnedCoins,earnedCopper,drillingEnergy,balanceBefore,balanceAfterBank,affordableCategories,purchases,spentCoins,balanceAfterPurchases,copperAfterPurchases,allMax,copperReady,evolved,walletCoins,walletCopper"};
            foreach (int rate in new[]{12,6,7,8}) foreach (int family in new[]{8100,9200,10300}) foreach (Policy strategy in Enum.GetValues(typeof(Policy)))
            {
                memory = new MemoryProgressStore(); yield return Load(); flow.Balance.Rewards.CoinsPerPayout = rate;
                Assert.That(flow.Service.Snapshot.Coins,Is.Zero); Assert.That(flow.Service.Snapshot.Copper,Is.Zero);
                int attempt = 0; int purchaseAttempts = 0; int dryStreak = 0;
                while (flow.Service.ActiveForm.Stage < 3 && attempt < 70)
                {
                    var form = flow.Service.ActiveForm; var before = flow.Service.Snapshot;
                    long needed = form.RequiredResources.Where(c => c.Resource == EvolutionResource.Copper).Sum(c => c.Amount);
                    // Pure avoidance cannot earn Copper. After MAX, this conservative player explicitly switches to Ore to meet the existing gate.
                    var actual = strategy == Policy.Safe && AllCurrentUpgradesMaximum() ? Policy.Resource : strategy;
                    Assert.That(flow.StartRun(), Is.True); motor.GetComponent<DrillPointerInput>().enabled = false;
                    flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = family + ++attempt; hud.Restart(); observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
                    yield return Drive(actual,before.Copper < needed);
                    Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(before.Coins + last.Coins));
                    Assert.That(flow.RetryBank(),Is.True); Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(before.Coins + last.Coins));
                    Assert.That(flow.OpenWorkshop(),Is.True); int affordable = AffordableUpgradeCount(); var banked=flow.Service.Snapshot.Coins;
                    int bought=BuyEconomyUpgrades(strategy); if(bought>0)purchaseAttempts++;
                    var after=flow.Service.Snapshot; bool max=AllCurrentUpgradesMaximum(),copper=after.Copper>=needed;
                    dryStreak = affordable == 0 && !max ? dryStreak + 1 : 0;
                    Assert.That(dryStreak,Is.LessThanOrEqualTo(2),"No long series of attempts without an affordable improvement.");
                    if (attempt == 1) { Assert.That(bought,Is.GreaterThanOrEqualTo(1)); if(rate==6) Assert.That(bought,Is.EqualTo(1)); }
                    Assert.That(AffordableUpgradeCount(),Is.Zero); bool evolved=flow.Evolution.Ready;
                    if(evolved){Assert.That(max&&copper,Is.True);Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(1);}
                    rows.Add(EconomyCsv(rate,family,strategy,actual,attempt,form.Stage,before.Level(UpgradeCategory.DrillPower),before.Level(UpgradeCategory.EnergyCapacity),before.Level(UpgradeCategory.DrivePower),
                        last.Seconds,last.Distance,last.DistanceCoins,last.ObstacleCoins,last.Coins,last.Copper,last.DrillEnergy,before.Coins,banked,affordable,bought,banked-after.Coins,after.Coins,after.Copper,max,copper,evolved,flow.Service.Snapshot.Coins,flow.Service.Snapshot.Copper));
                    File.WriteAllLines("Logs/M9A-M8R4-EconomyJourneys.csv",rows);
                    yield return null;
                }
                Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(3),EconomyCsv(rate,family,strategy));
                Assert.That(purchaseAttempts,Is.GreaterThan(0)); Assert.That(flow.Service.Snapshot.Iron+flow.Service.Snapshot.Quartz+flow.Service.Snapshot.AlloyCore+flow.Service.Snapshot.EnergyShard,Is.Zero);
            }
        }
        [UnityTest] public IEnumerator RevisedResultsWorkshopPurchasesAndRetryKeepEarningsDistinctFromSavings()
        {
            Assert.That(flow.Service.Snapshot.Coins,Is.Zero); Assert.That(flow.StartRun(),Is.True);
            flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=1337;hud.Restart();yield return Drive(Policy.Balanced);yield return null;
            var first=flow.Run.Rewards; long earned=first.Coins;
            Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(earned)); Assert.That(hud.SummaryText.text,Does.Contain(earned+" Coins"));
            yield return Capture("Economy-Fresh-Results"); Assert.That(flow.OpenWorkshop(),Is.True);
            Assert.That(flow.View.Balance.text,Does.Contain("COINS  "+earned)); yield return Capture("Economy-Fresh-Workshop");
            flow.View.PurchaseButtons[1].onClick.Invoke(); Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(earned-50));
            var saved=flow.Service.Snapshot.Coins; Assert.That(flow.Purchase(UpgradeCategory.DrillPower),Is.False); Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(saved));
            Assert.That(flow.View.PurchaseButtons[0].interactable,Is.False);yield return Capture("Economy-Purchased-Unaffordable");
            flow.View.StartButton.onClick.Invoke(); motor.GetComponent<DrillPointerInput>().enabled=false;
            Assert.That(motor.Energy.Capacity,Is.EqualTo(30));Assert.That(flow.Run.Rewards.Coins,Is.Zero);
            observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
            yield return Drive(Policy.Balanced);yield return null;
            Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(saved+last.Coins));Assert.That(first.Coins,Is.EqualTo(earned));
            var total=flow.Service.Snapshot.Coins;Assert.That(flow.RetryBank(),Is.True);Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(total));
            yield return Capture("Economy-Next-Results");flow.OpenWorkshop();yield return Capture("Economy-Next-Workshop");
            yield return Load();Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(total));Assert.That(flow.Service.Snapshot.EnergyLevel,Is.EqualTo(1));
        }
    }
}
