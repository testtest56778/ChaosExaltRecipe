using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;
using log4net;
using Default.EXtensions;
using Default.EXtensions.Global;
using Loki;
using Loki.Bot;
using Loki.Common;
using Loki.Game.GameData;
using Loki.Game.Objects;
using settings = ChaosExaltRecipe.Settings;


namespace ChaosExaltRecipe
{
    public class Class1 : IPlugin, IStartStopEvents
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private UserControl1 _gui;

        public static readonly string ChaosStashDataPath = Path.Combine(Configuration.Instance.Path, "ChaosExaltRecipeStashData.json");
        public static readonly RecipeData ChaosStashData = RecipeData.LoadFromJson(ChaosStashDataPath);

        public static readonly string ShaperStashDataPath = Path.Combine(Configuration.Instance.Path, "ExaltRecipeShaperStashData.json");
        public static readonly RecipeData ShaperStashData = RecipeData.LoadFromJson(ShaperStashDataPath);

        public static readonly string ElderStashDataPath = Path.Combine(Configuration.Instance.Path, "ExaltRecipeElderStashData.json");
        public static readonly RecipeData ElderStashData = RecipeData.LoadFromJson(ElderStashDataPath);

        public static readonly RecipeData ChaosPickupData = new RecipeData();
        public static readonly RecipeData ShaperPickupData = new RecipeData();
        public static readonly RecipeData ElderPickupData = new RecipeData();

        public static bool ShouldPickupShaper(Item item)
        {
            if (!RecipeData.IsItemForShaperExaltRecipe(item, out var type))
                return false;

            if (ShaperStashData.GetItemCount(type) + ShaperPickupData.GetItemCount(type) >= settings.Instance.GetMaxShaperItemCount(type))
                return false;

            Log.Warn($"[ChaosExaltRecipe] Adding \"{item.Name}\" (iLvl: {item.ItemLevel}) for pickup.");
            ShaperPickupData.IncreaseItemCount(type);
            return true;
        }

        public static bool ShouldPickupElder(Item item)
        {
            if (!RecipeData.IsItemForElderExaltRecipe(item, out var type))
                return false;

            if (ElderStashData.GetItemCount(type) + ElderPickupData.GetItemCount(type) >= settings.Instance.GetMaxElderItemCount(type))
                return false;

            Log.Warn($"[ChaosExaltRecipe] Adding \"{item.Name}\" (iLvl: {item.ItemLevel}) for pickup.");
            ElderPickupData.IncreaseItemCount(type);
            return true;
        }

        public static bool ShouldPickupChaos(Item item)
        {
            if (!RecipeData.IsItemForChaosRecipe(item, out var type))
                return false;

            if (ChaosStashData.GetItemCount(type) + ChaosPickupData.GetItemCount(type) >= settings.Instance.GetMaxChaosItemCount(type))
                return false;

            Log.Warn($"[ChaosExaltRecipe] Adding \"{item.Name}\" (iLvl: {item.ItemLevel}) for pickup.");
            ChaosPickupData.IncreaseItemCount(type);
            return true;
        }


        public void Start()
        {
            Log.DebugFormat("[ChaosExaltRecipe] Start");

            if (PluginManager.EnabledPlugins.Any(p => p.Name.Equals("chaosrecipe", StringComparison.InvariantCultureIgnoreCase)))
            {
                Log.Error($"[{Name}] ChaosRecipe is enabled. This could cause conflicts between the 2 plugins, please disable ChaosRecipe.");
                BotManager.Stop();
                return;
            }

            if (settings.Instance.ChaosRecipeEnabled && settings.Instance.ChaosStashTab == "")
            {
                Log.Error($"[{Name}] Chaos Recipe is enabled in Chaos Exalt Recipe plugin, but a stash tab was not set. Please set a valid stash tab for the chaos recipe.");
                BotManager.Stop();
                return;
            }

            if (settings.Instance.ExaltRecipeEnabled && settings.Instance.ShaperStashTab == "" && settings.Instance.ElderStashTab == "")
            {
                Log.Error($"[{Name}] Exalt Recipe is enabled in Chaos Exalt Recipe plugin, but a stash tab was not set. Please set a valid stash tab for the shaper/elder exalt recipe.");
                BotManager.Stop();
                return;
            }


            // Getting things set up now
            var taskManager = BotStructure.TaskManager;

            if (settings.Instance.ChaosRecipeEnabled)
            {
                if (!CombatAreaCache.AddPickupItemEvaluator("ChaosExaltRecipePickupEvaluatorShaper", ShouldPickupChaos))
                    Log.Error("[ChaosExaltRecipe] Fail to add pickup item evaluator.");
                taskManager.AddBefore(new StashChaosRecipeTask(), "IdTask");
                taskManager.AddBefore(new SellChaosRecipeTask(), "VendorTask");
            }
            else
            {
                if (!CombatAreaCache.RemovePickupItemEvaluator("ChaosExaltRecipePickupEvaluatorShaper"))
                    Log.Error("[ChaosExaltRecipe] Fail to remove pickup item evaluator.");
            }

            if (settings.Instance.ExaltRecipeEnabled)
            {

                if (!CombatAreaCache.AddPickupItemEvaluator("ExaltRecipePickupEvaluatorShaper", ShouldPickupShaper))
                    Log.Error("[ChaosExaltRecipe] Fail to add pickup item evaluator.");
                if (!CombatAreaCache.AddPickupItemEvaluator("ExaltRecipePickupEvaluatorElder", ShouldPickupElder))
                    Log.Error("[ChaosExaltRecipe] Fail to add pickup item evaluator.");
                taskManager.AddBefore(new StashShaperExaltRecipeTask(), "IdTask");
                taskManager.AddBefore(new StashElderExaltRecipeTask(), "IdTask");
                taskManager.AddBefore(new SellShaperExaltRecipeTask(), "VendorTask");
                taskManager.AddBefore(new SellElderExaltRecipeTask(), "VendorTask");
            }
            else
            {
                if (!CombatAreaCache.RemovePickupItemEvaluator("ExaltRecipePickupEvaluatorShaper"))
                    Log.Error("[ChaosExaltRecipe] Fail to remove pickup item evaluator.");
                if (!CombatAreaCache.RemovePickupItemEvaluator("ExaltRecipePickupEvaluatorElder"))
                    Log.Error("[ChaosExaltRecipe] Fail to remove pickup item evaluator.");
            }
        }

        public void Enable()
        {
            if(settings.Instance.ChaosRecipeEnabled)
                if (!CombatAreaCache.AddPickupItemEvaluator("ChaosExaltRecipePickupEvaluatorShaper", ShouldPickupChaos))
                    Log.Error("[ChaosExaltRecipe] Fail to add pickup item evaluator.");
            if (settings.Instance.ExaltRecipeEnabled)
            {

                if (!CombatAreaCache.AddPickupItemEvaluator("ExaltRecipePickupEvaluatorShaper", ShouldPickupShaper))
                    Log.Error("[ChaosExaltRecipe] Fail to add pickup item evaluator.");
                if (!CombatAreaCache.AddPickupItemEvaluator("ExaltRecipePickupEvaluatorElder", ShouldPickupElder))
                    Log.Error("[ChaosExaltRecipe] Fail to add pickup item evaluator.");
            }
        }

        public void Disable()
        {
            if (!CombatAreaCache.RemovePickupItemEvaluator("ChaosExaltRecipePickupEvaluatorShaper"))
                Log.Error("[ChaosExaltRecipe] Fail to remove pickup item evaluator.");
            if (!CombatAreaCache.RemovePickupItemEvaluator("ExaltRecipePickupEvaluatorShaper"))
                Log.Error("[ChaosExaltRecipe] Fail to remove pickup item evaluator.");
            if (!CombatAreaCache.RemovePickupItemEvaluator("ExaltRecipePickupEvaluatorElder"))
                Log.Error("[ChaosExaltRecipe] Fail to remove pickup item evaluator.");
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                var area = message.GetInput<DatWorldAreaWrapper>(3);
                if (area.IsTown || area.IsHideoutArea)
                {
                    Log.Info("[ChaosExaltRecipe] Resetting pickup data.");
                    ChaosPickupData.Reset();
                    ShaperPickupData.Reset();
                    ElderPickupData.Reset();
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        #region Unused interface methods
        public void Stop()
        {
        }

        public void Initialize()
        {
        }

        public void Deinitialize()
        {
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }
        #endregion

        public string Name => "ChaosExaltRecipe";
        public string Description => "Plugin to loot and vendor chaos/shaper/elder items.";
        public string Author => "Testtest";
        public string Version => "1.0";
        public UserControl Control => _gui ?? (_gui = new UserControl1());
        public JsonSettings Settings => settings.Instance;

    }
}
