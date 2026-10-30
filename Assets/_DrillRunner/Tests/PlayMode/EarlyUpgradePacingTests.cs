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
        [UnityTest] public IEnumerator EarlyUpgradePacingPairsR2AndR3TablesAndOwnerLikePowerEight()
        {
            var forms=new[]{flow.Configuration,flow.Configuration.NextForm};
            var current=forms.Select(f=>(float[])f.Energy.Values.Clone()).ToArray();
            float[][] previous={new[]{25f,35,45,55,65,80},new[]{90f,105,120,140,160,180}};
            var rows=new List<string>{Sample.Header+",powerLevel,driveLevel,cohort"};
            var median=new List<double>[2,6];for(int f=0;f<2;f++)for(int c=0;c<6;c++)median[f,c]=new List<double>();
            try
            {
                for(int version=0;version<2;version++)
                {
                    for(int i=0;i<2;i++)forms[i].Energy.Values=(float[])(version==0?previous[i]:current[i]).Clone();
                    flow.Configuration.Validate();
                    foreach(int stage in new[]{1,2})foreach(int cap in Enumerable.Range(0,6))foreach(int seed in new[]{1337,904,2026,8101,9201})foreach(Policy policy in Enum.GetValues(typeof(Policy)))
                    {
                        ConfigureLevels(stage,0,cap,0,seed);yield return Drive(policy);
                        rows.Add(last.Csv(version==0?"M8R2":"M8R3")+",0,0,CapacityOnly");
                        File.WriteAllLines("Logs/M9A-M8R4-PairedCapacity.csv",rows);
                        if(version==1&&policy==Policy.Balanced)median[stage-1,cap].Add(last.Seconds);
                        yield return null;
                    }
                    foreach(int drive in new[]{0,3,5})foreach(int seed in new[]{1337,904,2026,8101,9201})foreach(Policy policy in Enum.GetValues(typeof(Policy)))
                    {
                        ConfigureLevels(1,3,3,drive,seed);yield return Drive(policy);
                        rows.Add(last.Csv(version==0?"M8R2":"M8R3")+",3,"+drive+",OwnerLike");
                        File.WriteAllLines("Logs/M9A-M8R4-PairedCapacity.csv",rows);yield return null;
                    }
                }
                double[][] pocket={new[]{7d,9},new[]{8d,10},new[]{10d,12},new[]{12d,14},new[]{13d,16},new[]{15d,19}};
                for(int stage=0;stage<2;stage++)for(int cap=0;cap<6;cap++)
                {
                    double value=median[stage,cap].OrderBy(v=>v).ElementAt(2);
                    Assert.That(value,Is.InRange(stage==0?pocket[cap][0]:11,stage==0?pocket[cap][1]:23));
                    if(cap>0)Assert.That(value,Is.GreaterThan(median[stage,cap-1].OrderBy(v=>v).ElementAt(2)+0.5));
                }
            }
            finally{for(int i=0;i<2;i++)forms[i].Energy.Values=current[i];}
        }
    }
}
