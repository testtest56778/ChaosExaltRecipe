using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loki;
using Loki.Common;
using Newtonsoft.Json;

namespace ChaosExaltRecipe
{
    class Settings : JsonSettings
    {
        private static Settings _instance;
        public static Settings Instance => _instance ?? (_instance = new Settings());

        private Settings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, "ExaltRecipe.json"))
        {
        }

        public bool ChaosRecipeEnabled { get; set; }
        public bool ExaltRecipeEnabled { get; set; }
        public string ChaosStashTab { get; set; }
        public string ShaperStashTab { get; set; }
        public string ElderStashTab { get; set; }
        public int MinILvl { get; set; } = 60;
        public bool AlwaysUpdateStashData { get; set; }
        public int[] MaxChaosItemCounts { get; set; } = { 2, 2, 2, 2, 2, 10, 20, 20 };
        public int[] MaxShaperItemCounts { get; set; } = { 2, 2, 2, 2, 2, 10, 20, 20 };
        public int[] MaxElderItemCounts { get; set; } = { 2, 2, 2, 2, 2, 10, 20, 20 };

        public int GetMaxChaosItemCount(int itemType)
        {
            return MaxChaosItemCounts[itemType];
        }

        public int GetMaxShaperItemCount(int itemType)
        {
            return MaxShaperItemCounts[itemType];
        }

        public int GetMaxElderItemCount(int itemType)
        {
            return MaxElderItemCounts[itemType];
        }

    }
}
