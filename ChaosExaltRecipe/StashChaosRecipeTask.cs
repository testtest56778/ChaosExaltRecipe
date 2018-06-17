using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Default.EXtensions;
using Default.EXtensions.CachedObjects;
using Default.EXtensions.Positions;
using Loki.Bot;
using Loki.Common;
using Loki.Game;
using Loki.Game.Objects;
using settings = ChaosExaltRecipe.Settings;

namespace ChaosExaltRecipe
{
    class StashChaosRecipeTask : ErrorReporter, ITask
    {
        private static bool _shouldUpdateStashData;
        private bool _chaosStashTabIsFull;

        public async Task<bool> Run()
        {
            if (!settings.Instance.ChaosRecipeEnabled)
                return false;

            if (_chaosStashTabIsFull || ErrorLimitReached)
                return false;
            
            var area = World.CurrentArea;

            if (!area.IsTown && !area.IsHideoutArea)
                return false;

            if (_shouldUpdateStashData)
            {
                GlobalLog.Debug("[StashRecipeTask] Updating chaos recipe stash data (every Start)");

                if (!await OpenChaosRecipeTab())
                    return true;
                else
                    Class1.ChaosStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Chaos);
                _shouldUpdateStashData = false;
            }
            else
            {
                if (AnyItemToStash)
                {
                    GlobalLog.Debug("[StashRecipeTask] Updating chaos recipe stash data before actually stashing the items.");

                    if (settings.Instance.ChaosRecipeEnabled)
                    {
                        if (!await OpenChaosRecipeTab())
                            return true;
                        else
                            Class1.ChaosStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Chaos);
                    }
                }
                else
                {
                    GlobalLog.Info("[StashRecipeTask] AnyItemsToStash empty, No items to stash for chaos recipe.");
                    await Coroutines.CloseBlockingWindows();
                    return false;
                }
            }

            var itemsToStash = ItemsToStash;
            if (itemsToStash.Count == 0)
            {
                GlobalLog.Info("[StashRecipeTask] No items to stash for chaos recipe.");
                await Coroutines.CloseBlockingWindows();
                return false;
            }

            GlobalLog.Info($"[StashRecipeTask] {itemsToStash.Count} items to stash for chaos recipe.");

            await OpenChaosRecipeTab();

            foreach (var item in itemsToStash.OrderBy(i => i.Position, Position.Comparer.Instance))
            {
                if (item.RecipeType != RecipeData.ExaltRecipeItemType.Chaos)
                    continue;

                GlobalLog.Debug($"[StashRecipeTask] Now stashing \"{item.Name}\" for chaos recipe.");
                var itemPos = item.Position;
                if (!Inventories.StashTabCanFitItem(itemPos))
                {
                    GlobalLog.Error("[StashRecipeTask] Stash tab for chaos recipe is full and must be cleaned.");
                    _chaosStashTabIsFull = true;
                    return true;
                }
                if (!await Inventories.FastMoveFromInventory(itemPos))
                {
                    ReportError();
                    return true;
                }
                Class1.ChaosStashData.IncreaseItemCount(item.ItemType);
                GlobalLog.Info($"[Events] Item stashed ({item.FullName})");
                Utility.BroadcastMessage(this, Events.Messages.ItemStashedEvent, item);
                Class1.ChaosStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Chaos);
                Class1.ChaosStashData.Log();
            }
            await Wait.SleepSafe(300);

            return true;
        }

        public StashChaosRecipeTask()
        {
            ErrorLimitMessage = "[StashRecipeTask] Too many errors. This task will be disabled until combat area change.";
        }

        public void Start()
        {
            if (Settings.Instance.AlwaysUpdateStashData)
                _shouldUpdateStashData = true;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.CombatAreaChanged)
            {
                ResetErrors();
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        private async Task<bool> OpenChaosRecipeTab()
        {
            // Check Chaos Tab
            if (!await Inventories.OpenStashTab(settings.Instance.ChaosStashTab))
            {
                ReportError();
                return false;
            }
            var chaosTabInfo = LokiPoe.InGameState.StashUi.StashTabInfo;
            if (chaosTabInfo.IsPremiumSpecial)
            {
                GlobalLog.Error($"[StashRecipeTask] Invalid chaos stash tab type: {chaosTabInfo.TabType}. This tab cannot be used for chaos recipe.");
                BotManager.Stop();
                return false;
            }
            return true;
        }

        private static bool AnyItemToStash
        {
            get
            {
                foreach (var item in Inventories.InventoryItems)
                {
                    if (!RecipeData.IsItemForChaosRecipe(item, out var type4))
                        continue;
                    else
                    {
                        if (Class1.ChaosStashData.GetItemCount(type4) >= Settings.Instance.GetMaxChaosItemCount(type4))
                            continue;
                    }
                    return true;
                }
                return false;
            }
        }


        private static List<RecipeItem> ItemsToStash
        {
            get
            {
                var result = new List<RecipeItem>();
                var inventoryChaosData = new RecipeData();

                foreach (var item in Inventories.InventoryItems)
                {
                    if (!RecipeData.IsItemForChaosRecipe(item, out var type4))
                        continue;
                    else
                    {
                        if (Class1.ChaosStashData.GetItemCount(type4) + inventoryChaosData.GetItemCount(type4) >= Settings.Instance.GetMaxChaosItemCount(type4))
                            continue;
                        inventoryChaosData.IncreaseItemCount(type4);
                        result.Add(new RecipeItem(item, type4, RecipeData.ExaltRecipeItemType.Chaos));
                        continue;
                    }
                }
                return result;
            }
        }

        private class RecipeItem : CachedItem
        {
            public Vector2i Position { get; }
            public int ItemType { get; }
            public RecipeData.ExaltRecipeItemType RecipeType { get; }

            public RecipeItem(Item item, int itemType, RecipeData.ExaltRecipeItemType recipeType) : base(item)
            {
                Position = item.LocationTopLeft;
                ItemType = itemType;
                RecipeType = recipeType;
            }
        }

        #region Unused interface methods

        public void Tick()
        {
        }

        public void Stop()
        {
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public string Name => "StashChaosRecipeTask";
        public string Description => "Task that stashes items for chaos recipe.";
        public string Author => "Testtest";
        public string Version => "1.0";

        #endregion

    }
}