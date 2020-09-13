using HarmonyLib;
using Oc.Em;
using Oc.Item;

namespace FreeEnchant
{
    [HarmonyPatch(typeof(OcEm_NPC_Event), "CloseUI")]
    class NpcCloseOverride
    {
        [HarmonyPatch(typeof(OcEm_NPC_ChestBase), "CloseUI")]
        private static void NPC_ChestBase_CloseUI_Dummy_Override()
        {

        }

        [HarmonyPatch(typeof(OcEm_NPC_Event), "CloseUI")]
        private static void Postfix(OcEm_NPC_Event __instance)
        {
            try
            {
                var chestItemList = Traverse.Create(__instance).Field("_ChestItemList").GetValue<OcItem_ChestItemList>();

                if (!chestItemList.checkIsEmpty())
                {
                    Plugin.WindowState = true;
                }
            }
            catch
            {
            }
        }
    }
}
