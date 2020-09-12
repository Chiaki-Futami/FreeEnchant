using BepInEx;
using HarmonyLib;
using Oc;
using Oc.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace FreeEnchant
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VER)]
    [BepInProcess(PROCESS_NAME)]
    public class Plugin : BaseUnityPlugin
    {
        #region 定数

        public const string PLUGIN_GUID = "craftopia.misc.free_enchant";
        public const string PLUGIN_NAME = "FreeEnchant";
        public const string PLUGIN_VER = "1.0.0.0";

        private const string PROCESS_NAME = "Craftopia.exe";

        private const string AUTHOR_NAME = "@chiaki_p";

        private const int MAX_COLUMN_LENGTH = 5;

        private const float GROUP_MARGIN_WIDTH = 10f;
        private const float GROUP_MARGIN_HEIGHT = 10f;

        private const String TEXT_COMMON = "コモン";
        private const String TEXT_UNCOMMON = "アンコモン";
        private const String TEXT_RARE = "レア";
        private const String TEXT_SUPER_RARE = "スーパーレア";
        private const String TEXT_ULTRA_RARE = "ウルトラレア";
        private const String TEXT_EPIC = "エピック";
        private const String TEXT_LEGENDARY = "レジェンダリー";
        private const String TEXT_OVER_LEGENDARY = "オーバーレジェンダリー";

        #endregion

        #region ウィンドウ設定値

        private Texture2D WindowBackground;
        private GUIStyle BackgroundStyle;

        private static readonly float WindowWidth = Mathf.Min(Screen.width, 640);
        private static readonly float WindowHeight = Screen.height < 560 ? Screen.height : Screen.height - 200;

        private Rect WindowRect = new Rect((Screen.width - WindowWidth) / 2f,
                                           (Screen.height - WindowHeight) / 2f,
                                           WindowWidth,
                                           WindowHeight);

        private string WindowTitle = PLUGIN_NAME + " v" + PLUGIN_VER;

        private Vector2 ScrollViewVector = Vector2.zero;

        private Color textColorCommon = new Color(255f / 255f, 255f / 255f, 255f / 255f);
        private Color textColorRare = new Color(055f / 255f, 222f / 255f, 066f / 255f);
        private Color textColorSuperRare = new Color(001f / 255f, 187f / 255f, 246f / 255f);
        private Color textColorEpic = new Color(243f / 255f, 001f / 255f, 222f / 255f);
        private Color textColorLegendary = new Color(232f / 255f, 190f / 255f, 002f / 255f);

        #endregion

        #region 状態変数

        public static bool WindowState = false;
        public static bool PressClosed = false;

        #endregion

        private List<SoEnchantment> encList;
        public static Dictionary<int, SoEnchantment> selectedEncDic;

        void Awake()
        {

            Logger.LogInfo(PLUGIN_NAME + " バージョン " + PLUGIN_VER);
            Logger.LogInfo(PLUGIN_NAME + " 読み込み開始");

            try
            {
                var harmony = new Harmony(PLUGIN_GUID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                InitWindow();

                selectedEncDic = new Dictionary<int, SoEnchantment>();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("エラー:" + ex.Message.ToString());
            }
            finally
            {
                Logger.LogInfo(PLUGIN_NAME + " 読み込み完了");
            }
        }

        private void InitWindow()
        {
            WindowBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            WindowBackground.SetPixel(0, 0, new Color(0.8f, 0.8f, 0.8f, 0.8f));
            WindowBackground.Apply();
            BackgroundStyle = new GUIStyle { normal = new GUIStyleState { background = WindowBackground } };
        }

        void OnGUI()
        {
            try
            {
                if (WindowState)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    WindowRect = GUI.Window(0, WindowRect, WindowFunc, WindowTitle);
                }
            }
            catch
            {
            }
        }

        void WindowFunc(int windowId)
        {
            try
            {
                if (encList.IsNullOrEmpty())
                {
                    var dataList = OcResidentData.EnchantDataList;
                    this.encList = dataList.GetAll().Where(x => x.IsEnabled == true).ToList();
                }

                ScrollViewVector = GUILayout.BeginScrollView(ScrollViewVector, false, true);

                var textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    richText = true
                };

                GUILayout.BeginVertical();
                DrawCenteredLabel(String.Empty);
                DrawCenteredLabel("付与するエンチャントを4つまで選択して、確定を押下してください。", textStyle);
                DrawCenteredLabel("選択中のエンチャントは赤文字表示され、再度選択すると解除されます。", textStyle);
                DrawCenteredLabel(String.Empty);

                var selectString = new List<string>();

                foreach (var keyValuePair in selectedEncDic)
                {
                    selectString.Add(keyValuePair.Value.DisplayName);
                }

                var textStyle2 = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    richText = true
                };

                var styleState = new GUIStyleState
                {
                    textColor = textColorLegendary
                };
                textStyle2.normal = styleState;

                var applyStyle = new GUIStyle(GUI.skin.button)
                {
                    richText = true
                };
                applyStyle.normal.textColor = Color.cyan;

                GUILayout.BeginHorizontal(GUI.skin.box);
                DrawCenteredLabel("現在選択中のエンチャント：" + String.Join(", ", selectString), textStyle2);
                GUILayout.EndHorizontal();

                DrawCenteredLabel(String.Empty);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("確定", applyStyle))
                    DoEnchant();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("閉じる"))
                    CloseWindow();

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                //ここからエンチャントボタン作成
                foreach (EnchantRarity rarity in Enum.GetValues(typeof(EnchantRarity)))
                {
                    DrawCenteredLabel(String.Empty);
                    CreateButtonByRarity(rarity);
                }

                DrawCenteredLabel(String.Empty);
                DrawCenteredLabel(String.Empty);

                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
            catch
            {

            }
        }

        private void CreateButtonByRarity(EnchantRarity value)
        {
            try
            {
                var textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    richText = true
                };

                var styleState = new GUIStyleState();

                string name = "";

                switch (value)
                {
                    case EnchantRarity.Common:
                        name = TEXT_COMMON;
                        styleState.textColor = textColorCommon;
                        break;

                    case EnchantRarity.Uncommon:
                        name = TEXT_UNCOMMON;
                        styleState.textColor = textColorRare;
                        break;
                    case EnchantRarity.Rare:
                        name = TEXT_RARE;
                        styleState.textColor = textColorRare;
                        break;

                    case EnchantRarity.SuperRare:
                        name = TEXT_SUPER_RARE;
                        styleState.textColor = textColorSuperRare;
                        break;

                    case EnchantRarity.UltraRare:
                        name = TEXT_ULTRA_RARE;
                        styleState.textColor = textColorSuperRare;
                        break;

                    case EnchantRarity.Epic:
                        name = TEXT_EPIC;
                        styleState.textColor = textColorEpic;
                        break;

                    case EnchantRarity.Legendary:
                        name = TEXT_LEGENDARY;
                        styleState.textColor = textColorLegendary;
                        break;

                    case EnchantRarity.OverLegendary:
                        name = TEXT_OVER_LEGENDARY;
                        styleState.textColor = textColorLegendary;
                        break;

                    default:
                        name = "レアリティ不明";
                        styleState.textColor = textColorCommon;
                        break;
                }

                textStyle.normal = styleState;
                DrawCenteredLabel(name, textStyle);

                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();

                int count = 1;
                foreach (var data in encList.Where(x => x.Rarity == value))
                {
                    //選択済みの物は赤にする
                    var style = new GUIStyle(GUI.skin.button)
                    {
                        richText = true
                    };

                    if (selectedEncDic.ContainsKey(data.ID))
                    {
                        style.normal.textColor = Color.red;
                    }

                    if (GUILayout.Button(data.DisplayName, style))
                        AddSelectDic(data);
                    GUILayout.FlexibleSpace();

                    if (count < MAX_COLUMN_LENGTH)
                    {
                        count++;
                    }
                    else
                    {
                        count = 1;

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }

                for (int i = count; i < MAX_COLUMN_LENGTH; i++)
                {
                    GUILayout.FlexibleSpace();
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

            }
            catch
            {

            }
        }

        private void AddSelectDic(SoEnchantment enc)
        {
            try
            {
                if (selectedEncDic.ContainsKey(enc.ID))
                {
                    //すでに含まれている場合、削除する
                    selectedEncDic.Remove(enc.ID);
                }
                else
                {
                    if (selectedEncDic.Count < 4)
                    {
                        selectedEncDic.Add(enc.ID, enc);
                    }
                }
            }
            catch
            {

            }
        }

        void DrawCenteredLabel(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void DrawCenteredLabel(string text, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(text, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void DoEnchant()
        {
            try
            {
                PressClosed = false;

                WindowState = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            catch
            {

            }
        }

        void CloseWindow()
        {
            try
            {
                //初期化
                selectedEncDic = new Dictionary<int, SoEnchantment>();
                PressClosed = true;

                WindowState = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            catch
            {

            }
        }
    }
}
