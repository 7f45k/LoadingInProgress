﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;

namespace DropPodsInProgress
{
    public class Dialog_ReloadTransporters : Window
    {
        public Dialog_ReloadTransporters(Map map, List<CompTransporter> transporters)
        {
            this.map = map;
            this.transporters = new List<CompTransporter>();
            this.transporters.AddRange(transporters);
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)UI.screenHeight);
            }
        }

        protected override float Margin
        {
            get
            {
                return 0f;
            }
        }

        private float MassCapacity
        {
            get
            {
                float num = 0f;
                for (int i = 0; i < this.transporters.Count; i++)
                {
                    num += this.transporters[i].Props.massCapacity;
                }
                return num;
            }
        }
        private float CaravanMassCapacity
        {
            get
            {
                if (this.caravanMassCapacityDirty)
                {
                    this.caravanMassCapacityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedCaravanMassCapacity = CollectionsMassCalculator.CapacityTransferables(this.transferables, stringBuilder);
                    this.cachedCaravanMassCapacityExplanation = stringBuilder.ToString();
                }
                return this.cachedCaravanMassCapacity;
            }
        }

        private string TransportersLabel
        {
            get
            {
                return Find.ActiveLanguageWorker.Pluralize(this.transporters[0].parent.Label, -1);
            }
        }

        private string TransportersLabelCap
        {
            get
            {
                return this.TransportersLabel.CapitalizeFirst();
            }
        }

        private BiomeDef Biome
        {
            get
            {
                return this.map.Biome;
            }
        }

        private float MassUsage
        {
            get
            {
                if (this.massUsageDirty)
                {
                    this.massUsageDirty = false;
                    this.cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, false);
                    this.cachedMassUsage += InventoryMassUsage();
                }
                return this.cachedMassUsage;
            }
        }

        public float CaravanMassUsage
        {
            get
            {
                if (this.caravanMassUsageDirty)
                {
                    this.caravanMassUsageDirty = false;
                    this.cachedCaravanMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
                    this.cachedCaravanMassUsage += InventoryMassUsage();
                }
                return this.cachedCaravanMassUsage;
            }
        }

        private float TilesPerDay
        {
            get
            {
                if (this.tilesPerDayDirty)
                {
                    this.tilesPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(this.transferables, this.MassUsage, this.MassCapacity, this.map.Tile, -1, stringBuilder);
                    this.cachedTilesPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedTilesPerDay;
            }
        }

        private Pair<float, float> DaysWorthOfFood
        {
            get
            {
                if (this.daysWorthOfFoodDirty)
                {
                    this.daysWorthOfFoodDirty = false;
                    float first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.transferables, this.map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer, null, 0f, 3300);
                    this.cachedDaysWorthOfFood = new Pair<float, float>(first, DaysUntilRotCalculator.ApproxDaysUntilRot(this.transferables, this.map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, null, 0f, 3300));
                }
                return this.cachedDaysWorthOfFood;
            }
        }

        private Pair<ThingDef, float> ForagedFoodPerDay
        {
            get
            {
                if (this.foragedFoodPerDayDirty)
                {
                    this.foragedFoodPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(this.transferables, this.Biome, Faction.OfPlayer, stringBuilder);
                    this.cachedForagedFoodPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedForagedFoodPerDay;
            }
        }

        private float Visibility
        {
            get
            {
                if (this.visibilityDirty)
                {
                    this.visibilityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedVisibility = CaravanVisibilityCalculator.Visibility(this.transferables, stringBuilder);
                    this.cachedVisibilityExplanation = stringBuilder.ToString();
                }
                return this.cachedVisibility;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            this.CalculateAndRecacheTransferables();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, TitleRectHeight);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "LoadTransporters".Translate(this.TransportersLabel));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(this.MassUsage, this.MassCapacity, string.Empty, this.TilesPerDay, this.cachedTilesPerDayExplanation, this.DaysWorthOfFood, this.ForagedFoodPerDay, this.cachedForagedFoodPerDayExplanation, this.Visibility, this.cachedVisibilityExplanation, this.CaravanMassUsage, this.CaravanMassCapacity, this.cachedCaravanMassCapacityExplanation), null, this.map.Tile, null, this.lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), false, null, false);
            tabsList.Clear();
            tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate ()
            {
                this.tab = Tab.Pawns;
            }, this.tab == Tab.Pawns));
            tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate ()
            {
                this.tab = Tab.Items;
            }, this.tab == Tab.Items));
            inRect.yMin += 119f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, tabsList, 200f);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            this.DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 59f;
            bool flag = false;
            Tab tab = this.tab;
            if (tab != Tab.Pawns)
            {
                if (tab == Tab.Items)
                {
                    this.itemsTransfer.OnGUI(inRect2, out flag);
                }
            }
            else
            {
                this.pawnsTransfer.OnGUI(inRect2, out flag);
            }
            if (flag)
            {
                this.CountToTransferChanged();
            }
            GUI.EndGroup();
        }

        public override bool CausesMessageBackground()
        {
            return true;
        }

        private void AddToTransferables(Thing t)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                this.transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
        }

        private void AddToTransferablesSelected(Thing t)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if(transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                this.transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
            transferableOneWay.AdjustTo(t.stackCount);
        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - this.BottomButtonSize.x / 2f, rect.height - BottomAreaHeight, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true))
            {
                if (this.CaravanMassUsage > this.CaravanMassCapacity && this.CaravanMassCapacity != 0f)
                {
                    if (this.CheckForErrors(TransferableUtility.GetPawnsFromTransferables(this.transferables)))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("TransportersCaravanWillBeImmobile".Translate(), delegate
                        {
                            if (this.TryAccept())
                            {
                                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                                this.Close(false);
                            }
                        }, false, null));
                    }
                }
                else if (this.TryAccept())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    this.Close(false);
                }
            }
            Rect rect3 = new Rect(rect2.x - 10f - this.BottomButtonSize.x, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                this.CalculateAndRecacheTransferables();
            }
            Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                this.Close(true);
            }
            if (Prefs.DevMode)
            {
                float width = 200f;
                float num = this.BottomButtonSize.y / 2f;
                Rect rect5 = new Rect(0f, rect.height - BottomAreaHeight, width, num);
                if (Widgets.ButtonText(rect5, "Dev: Load instantly", true, false, true) && this.DebugTryLoadInstantly())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    this.Close(false);
                }
                Rect rect6 = new Rect(0f, rect.height - BottomAreaHeight + num, width, num);
                if (Widgets.ButtonText(rect6, "Dev: Select everything", true, false, true))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    this.SetToLoadEverything();
                }
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            this.transferables = new List<TransferableOneWay>();
            this.AddPawnsToTransferables();
            this.AddItemsToTransferables();
            this.AddContentsToTransferables();
            IEnumerable<TransferableOneWay> enumerable = null;
            string text = null;
            string destinationLabel = null;
            string text2 = "FormCaravanColonyThingCountTip".Translate();
            bool flag = true;
            IgnorePawnsInventoryMode ignorePawnInventoryMass = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
            bool flag2 = true;
            Func<float> availableMassGetter = () => this.MassCapacity - this.MassUsage;
            int tile = this.map.Tile;
            this.pawnsTransfer = new TransferableOneWayWidget(enumerable, text, destinationLabel, text2, flag, ignorePawnInventoryMass, flag2, availableMassGetter, 0f, false, tile, true, true, true, false, true, false, false);
            CaravanUIUtility.AddPawnsSections(this.pawnsTransfer, this.transferables);
            enumerable = from x in this.transferables
                            where x.ThingDef.category != ThingCategory.Pawn
                            select x;
            text2 = null;
            destinationLabel = null;
            text = "FormCaravanColonyThingCountTip".Translate();
            flag2 = true;
            ignorePawnInventoryMass = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
            flag = true;
            availableMassGetter = (() => this.MassCapacity - this.MassUsage);
            tile = this.map.Tile;
            this.itemsTransfer = new TransferableOneWayWidget(enumerable, text2, destinationLabel, text, flag2, ignorePawnInventoryMass, flag, availableMassGetter, 0f, false, tile, true, false, false, true, false, true, false);
            this.CountToTransferChanged();
        }

        private bool DebugTryLoadInstantly()
        {
            this.CreateAndAssignNewTransportersGroup();
            int i;
            for (i = 0; i < this.transferables.Count; i++)
            {
                TransferableUtility.Transfer(this.transferables[i].things, this.transferables[i].CountToTransfer, delegate (Thing splitPiece, IThingHolder originalThing)
                {
                    this.transporters[i % this.transporters.Count].GetDirectlyHeldThings().TryAdd(splitPiece, true);
                });
            }
            return true;
        }

        private bool TryAccept()
        {
            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(this.transferables);
            if (!this.CheckForErrors(pawnsFromTransferables))
            {
                return false;
            }
            int transportersGroup = this.CreateAndAssignNewTransportersGroup();
            this.KickOutFreeloadingPawns();
            this.RemoveUnwantedItems();
            this.ClearContentsNotLoaded();
            this.AssignTransferablesToRandomTransporters();
            IEnumerable<Pawn> enumerable = from x in pawnsFromTransferables
                                            where x.IsColonist && !x.Downed
                                            select x;
            if (enumerable.Any<Pawn>())
            {
                foreach (Pawn pawn in enumerable)
                {
                    Lord lord = pawn.GetLord();
                    if (lord != null)
                    {
                        lord.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord, null);
                    }
                }
                LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadAndEnterTransporters(transportersGroup), this.map, enumerable);
                foreach (Pawn pawn2 in enumerable)
                {
                    if (pawn2.Spawned)
                    {
                        pawn2.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
            }
            Messages.Message("MessageTransportersLoadingProcessStarted".Translate(), this.transporters[0].parent, MessageTypeDefOf.TaskCompletion, false);
            return true;
        }

        public void KickOutFreeloadingPawns()
        {
            int num = 0;
            foreach(CompTransporter transporter in this.transporters)
            {
                for(int i = transporter.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing t = transporter.innerContainer[i];
                    if(t is Pawn)
                    {
                        bool flag = transporter.innerContainer.TryDrop(t, ThingPlaceMode.Near, out Thing thing);

                        if( (t as Pawn).GetLord() != null)
                            (t as Pawn).GetLord().lordManager.RemoveLord((t as Pawn).GetLord());

                        /*For Debugging*/ 
                        //Log.Message("Dropping " + t.LabelShort + " : " + flag);
                    }
                    num++;
                }
            }
        }

        public void RemoveUnwantedItems()
        {
            foreach(CompTransporter transporter in this.transporters)
            {
                foreach(Thing t in transporter.innerContainer)
                {
                    /*TransferableOneWay transferable = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.transferables, TransferAsOneMode.PodsOrCaravanPacking);
                    if(transferable != null && transferable.CountToTransfer < transferable.MaxCount)
                    {
                        int dropCount = (transferable.MaxCount - transferable.CountToTransfer);
                        transporter.innerContainer.TryDrop(t, ThingPlaceMode.Near, dropCount, out Thing droppedThing);
                    }*/
                    if(t is Pawn && TransferableUtility.GetPawnsFromTransferables(this.transferables).Contains(t as Pawn))
                    {
                        Log.Warning(string.Concat(new object[]{
                            "Pawn ",
                            t.LabelShort,
                            " was still inside transporter ",
                            transporter.parent.ThingID,
                            " after pawns were dumped.",
                            "Removing ", t.LabelShort,
                            " from transporter and spawning manually. - Smash Phil"
                        }));
                        if (!t.Spawned)
                            GenSpawn.Spawn(t, transporter.parent.Position, transporter.Map, WipeMode.Vanish);
                        transporter.innerContainer.Remove(t);
                    }
                }
            }
        }

        public void ClearContentsNotLoaded()
        {
            foreach(CompTransporter transporter in this.transporters)
            {
                transporter.leftToLoad?.Clear();
            }

            List<Pawn> allPawnsSpawned = this.map.mapPawns.AllPawnsSpawned;
            for(int i = 0; i < allPawnsSpawned.Count; i++)
            {
                foreach(CompTransporter transporter in this.transporters)
                {
                    if(allPawnsSpawned[i].CurJobDef == JobDefOf.HaulToTransporter)
                    {
                        JobDriver_HaulToTransporter jobDriver_HaulToTransporter = (JobDriver_HaulToTransporter)allPawnsSpawned[i].jobs.curDriver;
                        if(jobDriver_HaulToTransporter.Transporter == transporter)
                        {
                            if(jobDriver_HaulToTransporter.ThingToCarry != null)
                            {
                                allPawnsSpawned[i].jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                            }
                        }
                    }
                }
            }
        }

        private void AssignTransferablesToRandomTransporters()
        {
            TransferableOneWay transferableOneWay = this.transferables.MaxBy((TransferableOneWay x) => x.CountToTransfer);
            int num = 0;
            for (int i = 0; i < this.transferables.Count; i++)
            {
                if (this.transferables[i] != transferableOneWay)
                {
                    if (this.transferables[i].CountToTransfer > 0)
                    {
                        this.transporters[num % this.transporters.Count].AddToTheToLoadList(this.transferables[i], this.transferables[i].CountToTransfer);
                        num++;
                    }
                }
            }

            if (num < this.transporters.Count)
            {
                int num2 = transferableOneWay.CountToTransfer;
                int num3 = num2 / (this.transporters.Count - num);
                for (int j = num; j < this.transporters.Count; j++)
                {
                    int num4 = (j != this.transporters.Count - 1) ? num3 : num2;
                    if (num4 > 0)
                    {
                        this.transporters[j].AddToTheToLoadList(transferableOneWay, num4);
                    }
                    num2 -= num4;
                }
            }
            else
            {
                this.transporters[num % this.transporters.Count].AddToTheToLoadList(transferableOneWay, transferableOneWay.CountToTransfer);
            }
        }

        private int CreateAndAssignNewTransportersGroup()
        {
            int nextTransporterGroupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
            for (int i = 0; i < this.transporters.Count; i++)
            {
                this.transporters[i].groupID = nextTransporterGroupID;
            }
            return nextTransporterGroupID;
        }

        private bool CheckForErrors(List<Pawn> pawns)
        {
            if (!this.transferables.Any((TransferableOneWay x) => x.CountToTransfer != 0))
            {
                Messages.Message("CantSendEmptyTransportPods".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (this.MassUsage > this.MassCapacity)
            {
                this.FlashMass();
                Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            Pawn pawn = pawns.Find((Pawn x) => !x.MapHeld.reachability.CanReach(x.PositionHeld, this.transporters[0].parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)));
            if (pawn != null)
            {
                Messages.Message("PawnCantReachTransporters".Translate(pawn.LabelShort, pawn).CapitalizeFirst(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            Map map = this.transporters[0].parent.Map;
            for (int i = 0; i < this.transferables.Count; i++)
            {
                if (this.transferables[i].ThingDef.category == ThingCategory.Item)
                {
                    int countToTransfer = this.transferables[i].CountToTransfer;
                    int num = 0;
                    if (countToTransfer > 0)
                    {
                        for (int j = 0; j < this.transferables[i].things.Count; j++)
                        {
                            Thing thing = this.transferables[i].things[j];
                            if (map.reachability.CanReach(thing.Position, this.transporters[0].parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                            {
                                num += thing.stackCount;
                                if (num >= countToTransfer)
                                {
                                    break;
                                }
                            }
                        }
                        if (num < countToTransfer)
                        {
                            if (countToTransfer == 1)
                            {
                                Messages.Message("TransporterItemIsUnreachableSingle".Translate(this.transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, false);
                            }
                            else
                            {
                                Messages.Message("TransporterItemIsUnreachableMulti".Translate(countToTransfer, this.transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, false);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void AddPawnsToTransferables()
        {
            List<Pawn> list = CaravanFormingUtility.AllSendablePawns(this.map, false, false, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                this.AddToTransferables(list[i]);
            }
            /* Debugging */
            /*foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned.Where(x => x.Faction == Faction.OfPlayer))
            {
                Log.Message("Testing: " + p.LabelShort);
                Log.Message("DownCheck: " + !p.Downed + " | MentalCheck: " + !p.InMentalState + " | PrisonerCheck: " + (p.IsPrisonerOfColony || p.Faction == Faction.OfPlayer) + " | LordCheck " + (p.GetLord() is null || p.GetLord().LordJob is LordJob_VoluntarilyJoinable));
                Log.Message("-------------");
            }
            Log.Message("=================");*/
        }

        private void AddItemsToTransferables()
        {
            List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(this.map, false, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                this.AddToTransferables(list[i]);
            }
        }

        private void AddContentsToTransferables()
        {
            foreach(CompTransporter transporter in this.transporters)
            {
                foreach(Thing t in transporter.innerContainer)
                {
                    if(t is Pawn) //Remove to add items inside transporter
                        this.AddToTransferablesSelected(t);
                }
                if(transporter.leftToLoad != null)
                {
                    foreach (TransferableOneWay t in transporter.leftToLoad)
                    {
                        if (t.AnyThing is Pawn)
                            this.AddToTransferablesSelected(t.AnyThing);
                    }
                }
            }
        }

        private void FlashMass()
        {
            this.lastMassFlashTime = Time.time;
        }

        private float InventoryMassUsage()
        {
            float num = 0f;
            foreach(CompTransporter transporter in this.transporters)
            {
                foreach(Thing t in transporter.innerContainer)
                {
                    if(!(t is Pawn))
                        num += t.GetStatValue(StatDefOf.Mass, true) * t.stackCount;
                }
            }
            return num;
        }

        private void SetToLoadEverything()
        {
            for (int i = 0; i < this.transferables.Count; i++)
            {
                this.transferables[i].AdjustTo(this.transferables[i].GetMaximumToTransfer());
            }
            this.CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            this.massUsageDirty = true;
            this.caravanMassUsageDirty = true;
            this.caravanMassCapacityDirty = true;
            this.tilesPerDayDirty = true;
            this.daysWorthOfFoodDirty = true;
            this.foragedFoodPerDayDirty = true;
            this.visibilityDirty = true;
        }

        private Map map;

        private List<CompTransporter> transporters;

        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget pawnsTransfer;

        private TransferableOneWayWidget itemsTransfer;

        private Dialog_ReloadTransporters.Tab tab;

        private float lastMassFlashTime = -9999f;

        private bool massUsageDirty = true;

        private float cachedMassUsage;

        private bool caravanMassUsageDirty = true;

        private float cachedCaravanMassUsage;

        private bool caravanMassCapacityDirty = true;

        private float cachedCaravanMassCapacity;

        private string cachedCaravanMassCapacityExplanation;

        private bool tilesPerDayDirty = true;

        private float cachedTilesPerDay;

        private string cachedTilesPerDayExplanation;

        private bool daysWorthOfFoodDirty = true;

        private Pair<float, float> cachedDaysWorthOfFood;

        private bool foragedFoodPerDayDirty = true;

        private Pair<ThingDef, float> cachedForagedFoodPerDay;

        private string cachedForagedFoodPerDayExplanation;

        private bool visibilityDirty = true;

        private float cachedVisibility;

        private string cachedVisibilityExplanation;

        private const float TitleRectHeight = 35f;

        private const float BottomAreaHeight = 55f;

        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private static List<TabRecord> tabsList = new List<TabRecord>();

        private enum Tab
        {
            Pawns,
            Items
        }
    }
}
