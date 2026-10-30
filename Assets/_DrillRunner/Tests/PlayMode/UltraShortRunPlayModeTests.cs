using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        private void ConfigureLevels(int stage,int power,int capacity,int drive,int seed)
        {
            flow.UseTemporaryStage(stage,1000000,0,0,0,0,0,false);
            int[] levels={power,capacity,drive};
            for(int category=0;category<3;category++) for(int level=0;level<levels[category];level++)Assert.That(flow.Purchase((UpgradeCategory)category),Is.True);
            Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;
            flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=seed;hud.Restart();observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
        }
        private void RestoreM8Capacities()
        {
            HistoricalRunFixture.RestoreCapacities(flow.Configuration);
        }
        [UnityTest] public IEnumerator UltraShortCapacityAndMixedUpgradesMeasureRealDecisionsAndCopper()
        {
            var rows=new List<string>{Sample.Header+",powerLevel,driveLevel"};var balanced=new List<double>[6];for(int i=0;i<6;i++)balanced[i]=new List<double>();
            int baseOre=0,upgradedOre=0;
            var profiles=Enumerable.Range(0,6).Select(c=>new[]{0,c,0}).Concat(new[]{new[]{1,0,0},new[]{0,0,1},new[]{2,0,2},new[]{5,0,5},new[]{5,5,0},new[]{0,5,5},new[]{2,2,2}}).ToArray();
            foreach(var p in profiles)foreach(int seed in new[]{1337,904,2026,8101,9201,0,1,7,42,10301})foreach(Policy policy in Enum.GetValues(typeof(Policy)))
            {
                ConfigureLevels(1,p[0],p[1],p[2],seed);yield return Drive(policy);
                rows.Add(last.Csv("M8R4")+","+p[0]+","+p[2]);File.WriteAllLines("Logs/M9A-M8R4-CapacityAndCombinations.csv",rows);
                Assert.That(last.Coins,Is.GreaterThanOrEqualTo(flow.Service.ActiveForm.Energy.Costs[0]));
                if(p[0]==0&&p[2]==0&&policy==Policy.Balanced)
                {
                    balanced[p[1]].Add(last.Seconds);Assert.That(last.Contacts,Is.GreaterThanOrEqualTo(1));Assert.That(last.Destroyed,Is.GreaterThanOrEqualTo(1));
                    Assert.That(last.FirstContact,Is.InRange(0.5,3.5),"The short loop must include an early real drilling/avoidance decision.");
                }
                if(p[0]==0&&p[2]==0&&policy==Policy.Resource&&last.Copper>0){if(p[1]==0)baseOre++;else upgradedOre++;}yield return null;
            }
            double[][] ranges={new[]{7d,9},new[]{8d,10},new[]{10d,12},new[]{12d,14},new[]{13d,16},new[]{15d,19}};
            for(int i=0;i<6;i++){var a=balanced[i].OrderBy(t=>t).ToArray();Assert.That((a[4]+a[5])/2,Is.InRange(ranges[i][0],ranges[i][1]));}
            Assert.That(baseOre,Is.GreaterThan(0),"Some initial Ore routes must be affordable at 25 Energy.");Assert.That(upgradedOre,Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator FreshPocketInspectorShortcutLeavesRealStoreAndReceiptsUnchanged()
        {
            var before=ProgressCodec.Encode(memory.Load().Progress);flow.UseTemporaryStage(1,0,0,0,0,0,0,false);Assert.That(flow.IsSandbox,Is.True);
            Assert.That(flow.Service.Snapshot.Coins,Is.Zero);Assert.That(flow.Service.Snapshot.Copper,Is.Zero);
            foreach(UpgradeCategory c in Enum.GetValues(typeof(UpgradeCategory)))Assert.That(flow.Service.Snapshot.Level(c),Is.Zero);
            Assert.That(flow.StartRun(),Is.True);Assert.That(motor.Energy.Capacity,Is.EqualTo(25));yield return Drive(Policy.Balanced);
            Assert.That(flow.IsBanked,Is.True);Assert.That(ProgressCodec.Encode(memory.Load().Progress),Is.EqualTo(before));
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("SampleScene");
            Assert.That(ProgressCodec.Encode(memory.Load().Progress),Is.EqualTo(before),"Leaving the sandbox cannot write the normal store.");
        }
    }
}
