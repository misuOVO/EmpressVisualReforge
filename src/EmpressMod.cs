using System;
using HarmonyLib;
using tContentPatch;
using tContentPatch.ModLoad;
using tContentPatch.Patch;

namespace EmpressVisualReforge
{
    /// <summary>
    /// 光之女皇 视觉重铸
    /// 独立扩展模组: 让光之女皇(Empress of Light)的技能射弹发光。
    /// </summary>
    public sealed class EmpressMod : Mod
    {
        private const string PatchId = "EmpressVisualReforge.patch";
        private Harmony harmony = null;
        private static string modDir = null;

        internal static string ModDir { get { return modDir; } }

        internal static void LogError(string message)
        {
            if (modDir == null || message == null) return;
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(modDir, "patch-errors.txt"),
                    "\n[运行时] " + message + "\n");
            }
            catch { }
        }

        public override void Load(ModObject mo)
        {
            modDir = mo?.modPath;
            EmpressGlow.Load(modDir);
        }

        public override void AddPatch(IAddPatch addPatch)
        {
            harmony = new Harmony(PatchId);
            System.Text.StringBuilder failures = new System.Text.StringBuilder();
            foreach (System.Type type in typeof(EmpressMod).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (System.Exception ex)
                {
                    System.Exception e = (ex is HarmonyException && ex.InnerException != null) ? ex.InnerException : ex;
                    failures.AppendLine(type.Name + ": " + e.Message);
                }
            }
            if (failures.Length > 0 && modDir != null)
            {
                try
                {
                    System.IO.File.WriteAllText(
                        System.IO.Path.Combine(modDir, "patch-errors.txt"),
                        "以下补丁类应用失败:\n" + failures);
                }
                catch { }
            }
        }

        public override void Unload()
        {
            EmpressGlow.Unload();
            harmony?.UnpatchAll(PatchId);
            harmony = null;
        }
    }
}
