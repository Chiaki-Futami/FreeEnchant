using HarmonyLib;
using Oc;
using Oc.Em;
using Oc.Item;
using Oc.Item.UI;
using System.Collections.Generic;
using System.Linq;

namespace FreeEnchant
{
    [HarmonyPatch(typeof(OcEm_NPC_Event), "OpenUI")]
    class NpcOpenOverride
    {
        [HarmonyPatch(typeof(OcEm_NPC_Event), "OpenUI")]
        private static bool Prefix(OcEm_NPC_Event __instance)
        {
            try
            {
                if (!Plugin.PressClosed)
                {

                    var chestItemList = Traverse.Create(__instance).Field("_ChestItemList").GetValue<OcItem_ChestItemList>();
                    int chestSize = chestItemList.ChestSize;

                    var inventoryMng = SingletonMonoBehaviour<OcItemUI_InventoryMng>.Inst;
                    var ocItemDataMng = SingletonMonoBehaviour<OcItemDataMng>.Inst;

                    if (SingletonMonoBehaviour<OcNetMng>.Inst.needEmLocalCtrl() && chestItemList.Item.Length != 0)
                    {

                        OcItem[] itemArray = new OcItem[chestSize];
                        int[] enchantArray = Plugin.selectedEncDic.Keys.ToArray();

                        for (int i = 0; i < chestSize; i++)
                        {
                            var chestItem = chestItemList.Item[i];

                            OcItem ocItem = ocItemDataMng.CreateItem(chestItem.Id, chestItem.Level, enchantArray);

                            //エンチャントのソート
                            ocItem.SortEnchantSlot();
                            itemArray[i] = ocItem;
                        }

                        int itemArrayLength = itemArray.Length;

                        for (int j = 0; j < itemArrayLength; j++)
                        {
                            if (chestItemList.Amount[j] != 0)
                            {
                                inventoryMng.TryTakeItem(itemArray[j], chestItemList.Amount[j], OcItemUI_InventoryMng.ItemTakeTrigger.Drop);
                                chestItemList.Item[j] = new OcItem(ocItemDataMng.EmptyData, 0);
                            }
                        }
                    }

                }

                Plugin.selectedEncDic = new Dictionary<int, SoEnchantment>();
            }
            catch
            {

            }

            return true;
        }
    }
}
