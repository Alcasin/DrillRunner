using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DrillRunner
{
    // Presentation only: selection is a transaction on the existing WorkshopController/service.
    public sealed class RegionWorkshopView : MonoBehaviour
    {
        [SerializeField] private WorkshopController flow;
        [SerializeField] private Button selector;
        [SerializeField] private Text selected,marker;
        [SerializeField] private GameObject drawer;
        [SerializeField] private Button[] choices;
        [SerializeField] private Text[] labels;
        private UnityAction[] actions;
        private long shownRevision=-1;
        private bool shownOpen;
        private RunRewards shownRun;
        public Button Selector => selector;
        public Button[] Choices => choices;
        public Text[] Labels => labels;
        public Text Selected => selected;
        public Text Marker => marker;
        public bool IsExpanded => drawer.activeSelf;
        private void OnEnable()
        {
            selector.onClick.AddListener(Toggle);actions=new UnityAction[choices.Length];
            for(int i=0;i<choices.Length;i++){int index=i;actions[i]=()=>Choose(index);choices[i].onClick.AddListener(actions[i]);}
            Refresh();
        }
        private void OnDisable()
        {
            if(selector!=null)selector.onClick.RemoveListener(Toggle);if(actions!=null)for(int i=0;i<choices.Length;i++)if(choices[i]!=null)choices[i].onClick.RemoveListener(actions[i]);if(drawer!=null)drawer.SetActive(false);
        }
        private void Toggle(){if(flow.CanPurchase){drawer.SetActive(!drawer.activeSelf);Refresh();}}
        private void Choose(int index){if(flow.SelectRegion(flow.Regions.Regions[index].Id)){drawer.SetActive(false);selector.Select();}Refresh();}
        private void LateUpdate()
        {
            if(flow.Service==null)return;
            if(shownRevision!=flow.Service.Revision||shownOpen!=flow.IsOpen||shownRun!=flow.Run.Rewards)Refresh();
        }
        public void Refresh()
        {
            if(flow.Service==null||flow.Regions==null)return;
            var p=flow.Service.Snapshot;shownRevision=p.Revision;shownOpen=flow.IsOpen;shownRun=flow.Run.Rewards;
            var glow=flow.View.Panel.transform.Find("Pocket Drill Presentation/M6 Evolution Glow") as RectTransform;if(glow!=null)glow.sizeDelta=new Vector2(180,60);
            var region=flow.SelectedRegion;selected.fontSize=region.Id==RegionIds.Advanced?15:17;
            string regionalResources=region.Id==RegionIds.Yard?"COPPER":region.Id==RegionIds.Quarry?"IRON":region.Id==RegionIds.Advanced?"QUARTZ / ALLOY":"ENERGY SHARD";
            selected.text=region.DisplayName.ToUpperInvariant()+"  /  "+regionalResources+"   ▾";
            selector.interactable=flow.CanPurchase;if(!flow.IsOpen)drawer.SetActive(false);
            for(int i=0;i<choices.Length;i++)
            {
                var r=flow.Regions.Regions[i];bool available=r.CanPlay(flow.Service.ActiveForm.Stage);
                string state=available?(r.Id==region.Id?"SELECTED":"AVAILABLE"):"LOCKED / EVO "+r.UnlockStage;
                string resource=r.Id==RegionIds.Yard?"Copper":r.Id==RegionIds.Quarry?"Iron":r.Id==RegionIds.Advanced?"Quartz / Alloy Core":"Energy Shard";
                labels[i].text=r.DisplayName+"  /  "+state+"\n"+resource+"  /  EVO "+r.UnlockStage+"  /  Best "+System.Math.Floor(p.BestDistance(r.Id)).ToString("0")+" m";
                labels[i].color=available?Color.white:new Color(0.12f,0.22f,0.28f);
                choices[i].interactable=flow.CanPurchase&&available;
            }
            var run=flow.Run.Rewards;string id=run!=null?run.RegionId:region.Id;var active=flow.Regions.Find(id);
            marker.text=active.DisplayName.ToUpperInvariant()+"   /   BEST "+System.Math.Floor(p.BestDistance(id)).ToString("0")+" m";marker.gameObject.SetActive(!flow.IsOpen);
        }
    }
}
