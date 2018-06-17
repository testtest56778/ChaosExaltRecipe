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
    class StashElderExaltRecipeTask : ErrorReporter, ITask
    {
        private static bool _shouldUpdateStashData;
        private bool _elderStashTabIsFull;

        public async Task<bool> Run()
        {
            if (!settings.Instance.ExaltRecipeEnabled)
                return false;

            if (_elderStashTabIsFull || ErrorLimitReached)
                return false;

            var area = World.CurrentArea;

            if (!area.IsTown && !area.IsHideoutArea)
                return false;

            if (_shouldUpdateStashData)
            {
                GlobalLog.Debug("[StashRecipeTask] Updating elder exalt recipe stash data (every Start)");

                if (settings.Instance.ExaltRecipeEnabled)
                {
                    if (!await OpenElderRecipeTab())
                        return true;
                    else
                        Class1.ElderStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Elder);
                }

                _shouldUpdateStashData = false;
            }
            else
            {
                if (AnyItemToStash)
                {
                    GlobalLog.Debug("[StashRecipeTask] Updating elder exalt recipe stash data before actually stashing the items.");

                    if (!await OpenElderRecipeTab())
                        return true;
                    else
                        Class1.ElderStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Elder);
                }
                else
                {
                    GlobalLog.Info("[StashRecipeTask] AnyItemsToStash empty, No items to stash for elder exalt recipe.");
                    await Coroutines.CloseBlockingWindows();
                    return false;
                }
            }

            var itemsToStash = ItemsToStash;
            if (itemsToStash.Count == 0)
            {
                GlobalLog.Info("[StashRecipeTask] No items to stash for elder exalt recipe.");
                await Coroutines.CloseBlockingWindows();
                return false;
            }

            GlobalLog.Info($"[StashRecipeTask] {itemsToStash.Count} items to stash for elder exalt recipe.");

            await OpenElderRecipeTab();

            foreach (var item in itemsToStash.OrderBy(i => i.Position, Position.Comparer.Instance))
            {
                if (item.RecipeType != RecipeData.ExaltRecipeItemType.Elder)
                    continue;

                GlobalLog.Debug($"[StashRecipeTask] Now stashing \"{item.Name}\" for elder exalt recipe.");
                var itemPos = item.Position;
                if (!Inventories.StashTabCanFitItem(itemPos))
                {
                    GlobalLog.Error("[StashRecipeTask] Stash tab for elder exalt recipe is full and must be cleaned.");
                    _elderStashTabIsFull = true;
                    return true;
                }
                if (!await Inventories.FastMoveFromInventory(itemPos))
                {
                    ReportError();
                    return true;
                }
                Class1.ElderStashData.IncreaseItemCount(item.ItemType);
                GlobalLog.Info($"[Events] Item stashed ({item.FullName})");
                Utility.BroadcastMessage(this, Events.Messages.ItemStashedEvent, item);

            }
            await Wait.SleepSafe(300);
            Class1.ElderStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Elder);
            Class1.ElderStashData.Log();
            return true;
        }

        public StashElderExaltRecipeTask()
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

        private async Task<bool> OpenElderRecipeTab()
        {
            // Check Elder Tab
            if (!await Inventories.OpenStashTab(Settings.Instance.ElderStashTab))
            {
                ReportError();
                return false;
            }
            var elderTabInfo = LokiPoe.InGameState.StashUi.StashTabInfo;
            if (elderTabInfo.IsPremiumSpecial)
            {
                GlobalLog.Error($"[StashRecipeTask] Invalid elder stash tab type: {elderTabInfo.TabType}. This tab cannot be used for exalt recipe.");
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

                    if (!RecipeData.IsItemForElderExaltRecipe(item, out var type))
                        continue;
                    else
                    {
                        if (Class1.ElderStashData.GetItemCount(type) >= Settings.Instance.GetMaxElderItemCount(type))
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
                var inventoryElderData = new RecipeData();

                foreach (var item in Inventories.InventoryItems)
                {
                    if (!RecipeData.IsItemForElderExaltRecipe(item, out var type))
                        continue;
                    else
                    {
                        if (Class1.ElderStashData.GetItemCount(type) + inventoryElderData.GetItemCount(type) >= Settings.Instance.GetMaxElderItemCount(type))
                            continue;
                        inventoryElderData.IncreaseItemCount(type);
                        result.Add(new RecipeItem(item, type, RecipeData.ExaltRecipeItemType.Elder));
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

        public string Name => "StashElderExaltRecipeTask";
        public string Description => "Task that stashes items for elder exalt recipe.";
        public string Author => "Testtest";
        public string Version => "1.0";

        #endregion
    }
}
