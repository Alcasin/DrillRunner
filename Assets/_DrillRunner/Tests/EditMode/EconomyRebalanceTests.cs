using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;

namespace DrillRunner.Tests.EditMode
{
    public sealed class EconomyRebalanceTests
    {
        private RapidRunBalance Balance => AssetDatabase.LoadAssetAtPath<RapidRunBalance>(RapidRunSetup.Path);
        private ProgressionConfig Root => AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath);
        [Test] public void AuthoredEconomyUsesSixCoinsAndPreservesPricesStatsAndEvolutionGates()
        {
            Balance.Validate();Root.Validate();Assert.That(Balance.Rewards.CoinsPerPayout,Is.EqualTo(6));
            Assert.That(Balance.Rewards.DistancePerPayout,Is.EqualTo(5));Assert.That(Balance.Rewards.ObstacleCoinMultiplier,Is.EqualTo(25));
            Assert.That(Root.Power.Costs,Is.EqualTo(new[]{60,100,160,240,340}));
            Assert.That(Root.Energy.Costs,Is.EqualTo(new[]{50,90,140,210,300}));Assert.That(Root.Drive.Costs,Is.EqualTo(Root.Energy.Costs));
            var next=Root.NextForm;Assert.That(next.Power.Costs,Is.EqualTo(new[]{150,230,340,480,650}));
            Assert.That(next.Energy.Costs,Is.EqualTo(new[]{130,210,310,440,600}));Assert.That(next.Drive.Costs,Is.EqualTo(next.Energy.Costs));
            Assert.That(Root.RequiredResources.Single().Amount,Is.EqualTo(12));Assert.That(next.RequiredResources.Single().Amount,Is.EqualTo(24));
            Assert.That(Root.RequiredResources.Single().Resource,Is.EqualTo(EvolutionResource.Copper));Assert.That(next.RequiredResources.Single().Resource,Is.EqualTo(EvolutionResource.Copper));
            foreach(var f in new[]{Root,next})Assert.That(f.Power.MaximumLevel+f.Energy.MaximumLevel+f.Drive.MaximumLevel,Is.EqualTo(15));
            Assert.That(Root.Energy.Values,Is.EqualTo(new[]{25f,30,35,40,45,55}));Assert.That(next.Energy.Values,Is.EqualTo(new[]{90f,100,110,120,130,140}));
        }
        [TestCase(4.999,0)][TestCase(5,6)][TestCase(9.999,6)][TestCase(10,12)][TestCase(96,114)][TestCase(97,114)]
        public void ConfiguredCompletedIntervalsPayExactlyOnce(double meters,long expected)
        {
            var run=new RunRewards(1,10,Balance.Rewards);run.Start();run.RecordForward(10+meters);run.RecordForward(10+meters);run.RecordForward(10);
            Assert.That(run.DistanceCoins,Is.EqualTo(expected));Assert.That(run.ObstacleCoins,Is.Zero);
            run.Complete();run.RecordForward(1000);Assert.That(run.DistanceCoins,Is.EqualTo(expected));
        }
        [TestCase("Wood",25,0)][TestCase("Brick",50,0)][TestCase("Stone",75,0)][TestCase("CopperOre",0,3)]
        public void ActualMaterialsPayOnlyTheirConfiguredSourceOnFullDestruction(string name,long coins,long copper)
        {
            var material=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(FoundationSetup.Root+"/ScriptableObjects/"+name+".asset");Assert.That(material,Is.Not.Null);material.Validate();
            var run=new RunRewards(2,0,Balance.Rewards);run.Start();
            Assert.That(run.Claim(1,material.RewardKind,material.RewardAmount,false),Is.False);Assert.That(run.Coins+run.Copper,Is.Zero);
            Assert.That(run.Claim(1,material.RewardKind,material.RewardAmount,true),Is.True);Assert.That(run.Claim(1,material.RewardKind,material.RewardAmount,true),Is.False);
            Assert.That(run.ObstacleCoins,Is.EqualTo(coins));Assert.That(run.DistanceCoins,Is.Zero);Assert.That(run.Copper,Is.EqualTo(copper));
        }
        [TestCase(97,1,139)][TestCase(96,3,189)]
        public void RecordedExamplesAreExplicitLedgerCounterfactualsNotNewGameplayMeasurements(double meters,int woodEquivalent,long expected)
        {
            var run=new RunRewards(3,0,Balance.Rewards);run.Start();run.RecordForward(meters);
            run.Claim(1,ObstacleRewardKind.Coins,woodEquivalent,true);run.Complete();Assert.That(run.Coins,Is.EqualTo(expected));
            // Run 2's 75 obstacle Coins are an aggregate; this does not assert which materials were actually destroyed.
        }
        [Test] public void NewIncomeDoesNotRepriceSavedBalancesAndBankReceiptsSurviveReload()
        {
            var initial=new PlayerProgress{Coins=375,Copper=7,Iron=3,Quartz=4,AlloyCore=5,EnergyShard=6,PowerLevel=3,EnergyLevel=3,DriveLevel=2,FormSchema=1,EvolutionSchema=1};
            var store=new MemoryProgressStore(initial);var before=ProgressCodec.Encode(store.Load().Progress);var service=new ProgressionService(store,Root);
            Assert.That(ProgressCodec.Encode(service.Snapshot),Is.EqualTo(before));
            Assert.That(service.TryBeginAttempt(out var token,out _),Is.True);Assert.That(service.TryBank(token,139,3,out _),Is.True);
            Assert.That(service.TryBank(token,139,3,out _),Is.True);var reloaded=new ProgressionService(store,Root);var p=reloaded.Snapshot;
            Assert.That(p.Coins,Is.EqualTo(514));Assert.That(p.Copper,Is.EqualTo(10));Assert.That(p.PowerLevel,Is.EqualTo(3));Assert.That(p.EnergyLevel,Is.EqualTo(3));Assert.That(p.DriveLevel,Is.EqualTo(2));
            Assert.That(p.Iron,Is.EqualTo(3));Assert.That(p.Quartz,Is.EqualTo(4));Assert.That(p.AlloyCore,Is.EqualTo(5));Assert.That(p.EnergyShard,Is.EqualTo(6));Assert.That(p.Version,Is.EqualTo(1));
            Assert.That(reloaded.TryBank(token,139,3,out _),Is.True);Assert.That(reloaded.Snapshot.Coins,Is.EqualTo(514));
        }
    }
}
