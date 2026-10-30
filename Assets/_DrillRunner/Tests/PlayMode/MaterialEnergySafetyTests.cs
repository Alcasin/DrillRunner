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
        // Isolated contact fixtures retain real material HP and real motor consumption. Never income/progression samples.
        private DrillObstacle IsolateMaterial(string id,float ahead=12)
        {
            var template=track.Catalog.Variants.SelectMany(s=>s.Obstacles).First(o=>o.Definition.MaterialId==id);
            foreach(var o in motor.RegisteredObstacles.ToArray()){motor.UnregisterObstacle(o);o.gameObject.SetActive(false);}
            var target=Object.Instantiate(template,new Vector3(0,1,motor.Motion.Position.z+ahead),Quaternion.identity);motor.RegisterObstacle(target);return target;
        }
        [UnityTest] public IEnumerator RegionalMaterialSnapshotsFollowStreamedTargetsWithoutLeakingToOtherRegions()
        {
            foreach(string region in RegionIds.All)
            {
                ConfigureAdvanced(region,7,0,1337);
                foreach(float z in new[]{0,120,240})
                {
                    track.UpdateCoverage(z,null);
                    foreach(var target in motor.RegisteredObstacles)
                    {
                        var cost=flow.SelectedRegion.DrillingCosts.FirstOrDefault(c=>c.MaterialId==target.Definition.MaterialId);
                        Assert.That(target.Durability.EnergyMultiplier,Is.EqualTo(cost.MaterialId==null?1:cost.Multiplier));
                        Assert.That(target.Durability.Maximum,Is.EqualTo(target.Definition.Durability));
                    }
                }
                ForceFixtureEnd();flow.OpenWorkshop();yield return null;
            }
        }
        [UnityTest] public IEnumerator IndependentPowerAndCapacityPurchasesImproveRealCompositeMiningAndEndurance()
        {
            var rows=new List<string>{"powerLevel,capacityLevel,power,capacity,drillRate,drillSeconds,drillEnergy,movingEnergy,distance,activeSeconds"};
            var times=new double[3];var costs=new double[3];var distances=new double[3];
            for(int sample=0;sample<3;sample++)
            {
                ConfigureAdvanced(RegionIds.Advanced,5,0,1337);flow.OpenWorkshop();
                if(sample>0)for(int i=0;i<5;i++)Assert.That(flow.Purchase(sample==1?UpgradeCategory.DrillPower:UpgradeCategory.EnergyCapacity),Is.True);
                BeginRegionalAttempt(RegionIds.Advanced,1337);
                var local=new[]{new MaterialDrillingCost("crystal_composite",8)};motor.ApplyMaterialDrillingCosts(local);local[0].Multiplier=1;hud.Restart();
                var target=IsolateMaterial("crystal_composite");Assert.That(Rate(target),Is.EqualTo(40));Launch();int steps=0;
                while(motor.Motion.State!=RunState.Drilling&&steps++<1500)motor.Simulate(1f/120);
                Assert.That(motor.Motion.State,Is.EqualTo(RunState.Drilling));
                var moving=motor.Diagnostics.MovingEnergy;motor.Simulate(0.2f);
                Assert.That(motor.Diagnostics.MovingEnergy,Is.EqualTo(moving));Assert.That(motor.Diagnostics.DrillingEnergy,Is.EqualTo(8).Within(0.001));
                Assert.Throws<InvalidOperationException>(()=>motor.ApplyMaterialDrillingCosts(Array.Empty<MaterialDrillingCost>()));
                var durability=target.Durability;float hp=durability.Remaining;motor.UnregisterObstacle(target);motor.RegisterObstacle(target);
                Assert.That(target.Durability,Is.SameAs(durability));Assert.That(target.Durability.Remaining,Is.EqualTo(hp));Assert.That(Rate(target),Is.EqualTo(40));
                steps=0;while(!target.Durability.IsDestroyed&&steps++<1500)motor.Simulate(1f/120);
                Assert.That(target.Durability.IsDestroyed,Is.True);var c=motor.Diagnostics.DrillingContacts.Single();times[sample]=c.Seconds;costs[sample]=c.Energy;
                Assert.That(c.Seconds,Is.EqualTo(1500.0/(motor.DrillPower*10)).Within(0.001));Assert.That(c.Energy,Is.EqualTo(c.Seconds*40).Within(0.001));
                while(motor.Motion.State!=RunState.RunEnded&&steps++<10000)motor.Simulate(0.05f);
                Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);Assert.That(motor.Diagnostics.BlockedEnergy,Is.Zero);Assert.That(motor.Diagnostics.Consumed,Is.EqualTo(motor.Energy.Capacity).Within(0.001));
                distances[sample]=flow.Run.Rewards.Distance;
                rows.Add(EconomyCsv(sample==1?5:0,sample==2?5:0,motor.DrillPower,motor.Energy.Capacity,40,c.Seconds,c.Energy,motor.Diagnostics.MovingEnergy,distances[sample],motor.Diagnostics.Seconds));
                motor.UnregisterObstacle(target);Object.Destroy(target.gameObject);flow.OpenWorkshop();yield return null;
            }
            Assert.That(times[1],Is.LessThan(times[0]));Assert.That(costs[1],Is.LessThan(costs[0]));Assert.That(times[2],Is.EqualTo(times[0]).Within(0.001));Assert.That(costs[2],Is.EqualTo(costs[0]).Within(0.001));Assert.That(distances[2],Is.GreaterThan(distances[0]));
            File.WriteAllLines("Logs/M9BR-IndependentUpgrades.csv",rows);
        }
        [UnityTest] public IEnumerator RefineryPartialCoreNaturallyDepletesBanksOnceAndRetryCleansUp()
        {
            string original=ProgressCodec.Encode(memory.Load().Progress);ConfigureAdvanced(RegionIds.Advanced,5,0,1337);Launch();int steps=0;
            // Spend real travel Energy, then test a real 1800-HP Core with the remaining budget. No direct Energy writes.
            while(motor.Energy.Remaining>120&&steps++<2000){aim=Choose(Policy.Safe);Steer(aim);Tick(0.05f);}
            var target=IsolateMaterial("alloy_core",9);steps=0;
            // Recenter through ordinary lateral input before reaching this isolated contact.
            while(motor.Motion.State!=RunState.Drilling&&steps++<2000){Steer(0);motor.Simulate(1f/120);}
            Assert.That(motor.Motion.State,Is.EqualTo(RunState.Drilling));motor.Simulate(0.2f);yield return new WaitForSecondsRealtime(0.12f);yield return Capture("M9BR-Base-Core-Attempt");
            var hp=target.Durability.Remaining;Assert.That(hp,Is.LessThan(1800));Assert.That(hp,Is.GreaterThan(0));Assert.That(flow.Run.Rewards.AlloyCore,Is.Zero);
            bool nearEmpty=false;
            while(motor.Motion.State!=RunState.RunEnded&&steps++<4000)
            {
                motor.Simulate(1f/120);
                if(!nearEmpty&&motor.Energy.Remaining<30&&motor.Motion.State==RunState.Drilling)
                {nearEmpty=true;yield return new WaitForSecondsRealtime(0.12f);yield return Capture("M9BR-Drilling-Near-Depletion");}
            }
            Assert.That(nearEmpty,Is.True);Assert.That(motor.ContactTarget,Is.SameAs(target));
            Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);Assert.That(motor.Energy.Remaining,Is.Zero);Assert.That(target.Durability.IsDestroyed,Is.False);Assert.That(target.Durability.Remaining,Is.GreaterThan(0));
            Assert.That(motor.Diagnostics.DrillingContacts.Any(c=>c.MaterialId=="alloy_core"&&!c.Completed),Is.True);Assert.That(flow.Run.Rewards.AlloyCore,Is.Zero);Assert.That(flow.Service.Snapshot.AlloyCore,Is.Zero);
            Assert.That(flow.IsBanked,Is.True);var bank=ProgressCodec.Encode(flow.Service.Snapshot);Assert.That(flow.RetryBank(),Is.True);Assert.That(ProgressCodec.Encode(flow.Service.Snapshot),Is.EqualTo(bank));
            File.WriteAllLines("Logs/M9BR-PartialDepletion.csv",new[]{"stage,material,hp,remainingHp,activeSeconds,movingEnergy,drillingEnergy,blockedEnergy,awardedCore",EconomyCsv(5,"alloy_core",1800,target.Durability.Remaining,motor.Diagnostics.Seconds,motor.Diagnostics.MovingEnergy,motor.Diagnostics.DrillingEnergy,motor.Diagnostics.BlockedEnergy,flow.Run.Rewards.AlloyCore)});
            float remaining=target.Durability.Remaining;motor.Simulate(1);Assert.That(target.Durability.Remaining,Is.EqualTo(remaining));yield return new WaitForSecondsRealtime(0.12f);yield return Capture("M9BR-Partial-Depletion-Results");
            motor.UnregisterObstacle(target);Object.Destroy(target.gameObject);yield return null;hud.RestartButton.onClick.Invoke();motor.GetComponent<DrillPointerInput>().enabled=false;
            Assert.That(motor.Motion.State,Is.EqualTo(RunState.Ready));Assert.That(motor.Energy.Remaining,Is.EqualTo(440));Assert.That(flow.Run.Rewards.AlloyCore,Is.Zero);Assert.That(motor.Diagnostics.DrillingContacts,Is.Empty);
            foreach(var o in motor.RegisteredObstacles)Assert.That(o.Durability.Fraction,Is.EqualTo(1));flow.OpenWorkshop();Assert.That(flow.IsOpen,Is.True);Assert.That(ProgressCodec.Encode(memory.Load().Progress),Is.EqualTo(original));
        }
        [UnityTest,Timeout(180000)] public IEnumerator RefineryNormalUpdateShowsBaseIntermediateAndUpgradedResourcePressure()
        {
            var rows=new List<string>{EnergyPressureHeader};var contacts=new List<string>{"mode,region,stage,level,seed,policy,material,hp,completed,drillingSeconds,drillingEnergy,totalEnergyAtCompletion"};
            foreach(var p in new[]{new[]{5,0},new[]{5,2},new[]{5,5},new[]{6,0}})
            {
                ConfigureAdvanced(RegionIds.Advanced,p[0],p[1],1337);advancedGoal=p[0]==6?ObstacleRewardKind.AlloyCore:ObstacleRewardKind.Quartz;
                string label="M9BR-Evo"+p[0]+"-L"+p[1];bool contact=false,reward=false;motor.enabled=true;
                using(var framing=new LiveFrameTarget(Object.FindFirstObjectByType<RunnerCamera>().GetComponent<Camera>(),hud.GetComponent<Canvas>()))
                try
                {
                    motor.Motion.Press();yield return new WaitForSecondsRealtime(1.15f);motor.Motion.Release();float begin=Time.realtimeSinceStartup;
                    while(motor.Motion.State!=RunState.RunEnded)
                    {
                        aim=Choose(Policy.Balanced);Steer(aim);
                        if(!contact&&motor.Motion.State==RunState.Drilling&&motor.ContactTarget.Durability.Fraction<0.8f)
                        {contact=true;motor.enabled=false;yield return new WaitForSecondsRealtime(0.12f);yield return Capture(label+"-Drilling");motor.enabled=true;}
                        if(!reward&&flow.Run.Rewards.Resource(advancedGoal)>0)
                        {reward=true;motor.enabled=false;yield return Capture(label+"-Resource");motor.enabled=true;}
                        Assert.That(Time.realtimeSinceStartup-begin,Is.LessThan(45));yield return null;
                    }
                    Assert.That(contact&&reward,Is.True);Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);Assert.That(flow.IsBanked,Is.True);yield return Capture(label+"-Results");
                    ExportEnergyPressure(rows,contacts,"Live",p[0],p[1],1337,Policy.Balanced);
                    File.WriteAllLines("Logs/M9BR-LiveProfiles.csv",rows);File.WriteAllLines("Logs/M9BR-LiveContacts.csv",contacts);
                    Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,Is.EqualTo(5));Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
                }
                finally{motor.enabled=false;}
                flow.OpenWorkshop();yield return null;
            }
        }
    }
}
