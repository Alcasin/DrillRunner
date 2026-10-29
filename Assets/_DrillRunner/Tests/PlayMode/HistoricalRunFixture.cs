using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrillRunner.Tests.PlayMode
{
    /// <summary>Explicit pre-rapid-run tuning for archived M0-M8 regressions with fixed time/Energy/reward expectations.
    /// No assertions are changed. RapidRunPlayModeTests exercise the authored production tuning and short-run economy.</summary>
    internal static class HistoricalRunFixture
    {
        internal static AsyncOperation LoadScene()
        {
            WorkshopController.BalanceFactory = _ =>
            {
                // Awake has already cloned this chain. Restore historical capacities only
                // in that session, before the existing service/motor read their attributes.
                RestoreCapacities(Object.FindFirstObjectByType<WorkshopController>().Configuration);
                return null;
            };
            return SceneManager.LoadSceneAsync("Gameplay");
        }
        internal static void RestoreCapacities(ProgressionConfig root)
        {
            float[][] values={new[]{100f,115,130,145,160,180},new[]{200f,220,240,260,280,300},new[]{320f,345,370,400,430,460},
                new[]{480f,510,540,575,610,650},new[]{680f,720,760,805,850,900},new[]{950f,1000,1060,1120,1180,1250},
                new[]{1320f,1390,1460,1540,1620,1700},new[]{1800f,1890,1980,2080,2190,2300}};
            var form=root;for(int i=0;i<8&&form!=null;i++,form=form.NextForm)form.Energy.Values=values[i];
            root.Validate();
        }
        internal static AsyncOperation UnloadScene()
        {
            WorkshopController.BalanceFactory = null;
            return SceneManager.LoadSceneAsync("SampleScene");
        }
    }
}
