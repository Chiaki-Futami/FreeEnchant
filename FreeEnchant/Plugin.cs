using BepInEx;
using HarmonyLib;
using Oc;
using Oc.Item;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        public const string PLUGIN_VER = "1.1.1.0";

        private const string PROCESS_NAME = "Craftopia.exe";

        private const string AUTHOR_NAME = "@chiaki_p";

        private const int MAX_COLUMN_LENGTH = 5;

        private const string TEXT_HALF_BLANK = " ";
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
            //check culture and set en, if not ja.
            if (!Thread.CurrentThread.CurrentUICulture.Name.StartsWith("ja") &&
                 !Thread.CurrentThread.CurrentUICulture.Name.StartsWith("en"))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en", false);
            }

            OutputLog(LogLevel.Info, PLUGIN_NAME + TEXT_HALF_BLANK + GetCultureString("Version") + TEXT_HALF_BLANK + PLUGIN_VER);
            OutputLog(LogLevel.Info, PLUGIN_NAME + TEXT_HALF_BLANK + GetCultureString("LoadStart"));

            try
            {
                var harmony = new Harmony(PLUGIN_GUID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                InitWindow();

                selectedEncDic = new Dictionary<int, SoEnchantment>();
            }
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
            finally
            {
                OutputLog(LogLevel.Info, PLUGIN_NAME + TEXT_HALF_BLANK + GetCultureString("LoadEnd"));
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
                DrawCenteredLabel(GetCultureString("TopLabel1"), textStyle);
                DrawCenteredLabel(GetCultureString("TopLabel2"), textStyle);
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
                DrawCenteredLabel(GetCultureString("SelectingEnchant") + String.Join(", ", selectString), textStyle2);
                GUILayout.EndHorizontal();

                DrawCenteredLabel(String.Empty);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(GetCultureString("ApplyButton"), applyStyle))
                    DoEnchant();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(GetCultureString("CloseButton")))
                    CloseWindow();

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                //create enchant buttons dynamically
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
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
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
                        name = GetCultureString("Rarity_Common");
                        styleState.textColor = textColorCommon;
                        break;

                    case EnchantRarity.Uncommon:
                        name = GetCultureString("Rarity_Uncommon");
                        styleState.textColor = textColorRare;
                        break;
                    case EnchantRarity.Rare:
                        name = GetCultureString("Rarity_Rare");
                        styleState.textColor = textColorRare;
                        break;

                    case EnchantRarity.SuperRare:
                        name = GetCultureString("Rarity_SuperRare");
                        styleState.textColor = textColorSuperRare;
                        break;

                    case EnchantRarity.UltraRare:
                        name = GetCultureString("Rarity_UltraRare");
                        styleState.textColor = textColorSuperRare;
                        break;

                    case EnchantRarity.Epic:
                        name = GetCultureString("Rarity_Epic");
                        styleState.textColor = textColorEpic;
                        break;

                    case EnchantRarity.Legendary:
                        name = GetCultureString("Rarity_Legendary");
                        styleState.textColor = textColorLegendary;
                        break;

                    case EnchantRarity.OverLegendary:
                        name = GetCultureString("Rarity_OverLegendary");
                        styleState.textColor = textColorLegendary;
                        break;

                    default:
                        name = GetCultureString("Rarity_Unknown");
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
                    //make selected encs red
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
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
        }

        private void AddSelectDic(SoEnchantment enc)
        {
            try
            {
                if (selectedEncDic.ContainsKey(enc.ID))
                {
                    //already included, delete
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
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
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
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
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
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
        }

        private string GetCultureString(string name) 
        {
            return Properties.Resources.ResourceManager.GetString(name) ?? "";
        }

        public enum LogLevel
        {
            Info = 0,
            Warning = 1,
            Debug = 2,
            Error = 9,
        }

        public void OutputLog(LogLevel logLevel, string logString)
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    Logger.LogInfo(logString);
                    break;
                case LogLevel.Warning:
                    Logger.LogWarning(logString);
                    break;
                case LogLevel.Debug:
                    Logger.LogDebug(logString);
                    break;
                case LogLevel.Error:
                    Logger.LogError(logString);
                    break;
                default:
                    return;
            }
        }
    }
}
