using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Default.EXtensions;
using Loki.Bot;
using settings = ChaosExaltRecipe.Settings;

namespace ChaosExaltRecipe
{
    class SellElderExaltRecipeTask : ErrorReporter, ITask
    {
        public async Task<bool> Run()
        {
            if (ErrorLimitReached)
                return false;

            var area = World.CurrentArea;
            if (!area.IsTown && !area.IsHideoutArea)
                return false;

            if (settings.Instance.ExaltRecipeEnabled)
            {
                {
                    if (!Class1.ElderStashData.HasCompleteSet())
                    {
                        GlobalLog.Info("[SellRecipeTask] No Elder exalted recipe set to sell.");
                        return false;
                    }

                    GlobalLog.Debug("[SellRecipeTask] Now going to take and sell a set of Elder items for exalt recipe.");

                    if (!await Inventories.OpenStashTab(Settings.Instance.ElderStashTab))
                    {
                        ReportError();
                        return true;
                    }

                    Class1.ElderStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Elder);

                    if (!Class1.ElderStashData.HasCompleteSet())
                    {
                        GlobalLog.Error("[SellRecipeTask] Saved Elder stash data does not match actual stash data.");
                        await Coroutines.CloseBlockingWindows();
                        return false;
                    }

                    var takenItemData = new RecipeData();

                    int lowestItemType = RecipeItemType.None;
                    var lowestItem = Inventories.StashTabItems
                        .OrderBy(i => i.ItemLevel)
                        .FirstOrDefault(i => RecipeData.IsItemForElderExaltRecipe(i, out lowestItemType));

                    if (lowestItem == null)
                    {
                        GlobalLog.Error("[SellRecipeTask] Unknown error. Fail to find actual item for Elder exalt recipe.");
                        ReportError();
                        return true;
                    }

                    GlobalLog.Debug($"[SellRecipeTask] Now taking \"{lowestItem.Name}\" (iLvl: {lowestItem.ItemLevel})");

                    if (!await Inventories.FastMoveFromStashTab(lowestItem.LocationTopLeft))
                    {
                        ReportError();
                        return true;
                    }

                    takenItemData.IncreaseItemCount(lowestItemType);

                    while (true)
                    {
                        int type = RecipeItemType.None;

                        for (int i = 0; i < RecipeItemType.TotalTypeCount; i++)
                        {
                            var count = takenItemData.GetItemCount(i);
                            if (count == 0 || (count == 1 && i == RecipeItemType.Ring))
                            {
                                type = i;
                                break;
                            }
                        }

                        if (type == RecipeItemType.None)
                            break;

                        var item = Inventories.StashTabItems
                            .Where(i => RecipeData.IsItemForElderExaltRecipe(i, out var t) && t == type)
                            .OrderByDescending(i => i.ItemLevel)
                            .FirstOrDefault();

                        if (item == null)
                        {
                            GlobalLog.Error("[SellRecipeTask] Unknown error. Fail to find actual item for Elder exalt recipe.");
                            ReportError();
                            return true;
                        }

                        GlobalLog.Debug($"[SellRecipeTask] Now taking \"{item.Name}\" (iLvl: {item.ItemLevel})");

                        if (!await Inventories.FastMoveFromStashTab(item.LocationTopLeft))
                        {
                            ReportError();
                            return true;
                        }

                        takenItemData.IncreaseItemCount(type);
                    }

                    await Wait.SleepSafe(300);

                    Class1.ElderStashData.SyncWithStashTab(RecipeData.ExaltRecipeItemType.Elder);

                    var recipeItems = Inventories.InventoryItems
                        .Where(i => RecipeData.IsItemForElderExaltRecipe(i, out var t))
                        .Select(i => i.LocationTopLeft)
                        .ToList();

                    if (recipeItems.Count == 0)
                    {
                        GlobalLog.Error("[SellRecipeTask] Unknown error. There are no items in player's inventory after taking them from stash.");
                        ReportError();
                        return true;
                    }

                    if (!await TownNpcs.SellItems(recipeItems))
                        ReportError();
                }
            }
            else
                return false;

            return true;
        }

        public SellElderExaltRecipeTask()
        {
            ErrorLimitMessage = "[SellRecipeTask] Too many errors. This task will be disabled until combat area change.";
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


        #region Unused interface methods

        public void Tick()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public string Name => "SellElderExaltRecipeTask";
        public string Description => "Task that sells items for Elder exalt recipe";
        public string Author => "Testtest";
        public string Version => "1.0";

        #endregion


    }

}
