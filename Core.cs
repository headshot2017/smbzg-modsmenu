using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonPriority(100)]
[assembly: MelonInfo(typeof(SMBZModsMenu.Core), "SMBZModsMenu", "1.0.0", "Headshotnoby/headshot2017", null)]
[assembly: MelonGame("Jonathan Miller aka Zethros", "SMBZ-G")]

namespace SMBZModsMenu
{
    public class Core : MelonMod
    {
        public static List<Func<bool>> BeforeSkins = [];
        public static List<Func<bool>> BeforeMainMenu = [];
        public static List<ModEntry> ModEntries = [];
        public static bool SkinsLoaded = false;
        public static bool MenuLoaded = false;

        GameObject ButtonPrefab;

        public MelonPreferences_Category gamebananaCacheCategory;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");

            gamebananaCacheCategory = MelonPreferences.CreateCategory("GameBananaCache");
            gamebananaCacheCategory.SetFilePath("UserData/SMBZModsMenu.cfg");

            ModEntries.Add(new()
            {
                info = Info,
                updateLocation = ModUpdateLocation.Github,
                updateRepo = "headshot2017/smbzg-modsmenu"
            });
        }

        public override void OnLateInitializeMelon()
        {
            foreach (var mod in RegisteredMelons)
            {
                if (!ModEntries.Exists(x => x.info.Name == mod.Info.Name))
                    ModEntries.Add(new() { info = mod.Info });
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                // yoink a button gameobject as a prefab
                if (ButtonPrefab == null)
                {
                    ButtonPrefab = GameObject.Instantiate(GameObject.Find("Canvas").transform.Find("Panel_Credits").Find("Button_Close").gameObject);
                    GameObject.Destroy(ButtonPrefab.GetComponentInChildren<LocalizedText>());
                    ButtonPrefab.SetActive(false);
                    GameObject.DontDestroyOnLoad(ButtonPrefab);
                }
            }
            if (sceneName == "Options")
                LoadOptions();
        }

        void LoadOptions()
        {
            if (ButtonPrefab == null) return;

            OptionsMenuScript optionsScript = GameObject.FindObjectOfType<OptionsMenuScript>();

            // i hate working with unity UI at runtime

            GameObject dummyTabButton = optionsScript.TabButtonList[0].gameObject;
            GameObject dummyTabContent = optionsScript.TabContentList[0];
            GameObject dummyTabViewportContent = dummyTabContent.transform.Find("Scroll View").Find("Viewport").Find("Content").gameObject;
            GameObject tabButtonsRoot = dummyTabButton.transform.parent.gameObject;
            GameObject tabContentRoot = dummyTabContent.transform.parent.gameObject;

            GameObject modsTabButton = GameObject.Instantiate(dummyTabButton, tabButtonsRoot.transform);
            TMPro.TextMeshProUGUI text = modsTabButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Button button = modsTabButton.GetComponent<Button>();
            modsTabButton.name = "TabButton_Mods";
            text.text = "Mods";

            // do not trigger the call to optionsScript.SwitchPage() that was added in the editor
            button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
            button.onClick.AddListener(() => optionsScript.SwitchPage(2));

            GameObject modsTabContent = GameObject.Instantiate(dummyTabContent, tabContentRoot.transform);
            GameObject scrollContent = modsTabContent.transform.Find("Scroll View").Find("Viewport").Find("Content").gameObject;
            modsTabContent.name = "Page_Mods";
            scrollContent.transform.RemoveAllChildren();
            scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 0);

            foreach (var mod in ModEntries)
            {
                GameObject modObj = new($"ModEntry_${mod.info.Name}");
                modObj.transform.SetParent(scrollContent.transform, false);
                modObj.AddComponent<RectTransform>().anchoredPosition = new Vector2(-640, 0);
                modObj.transform.localPosition = new Vector2(0, 0);

                GameObject labelObj = GameObject.Instantiate(dummyTabViewportContent.transform.Find("Label_Resolution").gameObject, modObj.transform);
                GameObject.Destroy(labelObj.GetComponent<LocalizedText>());
                labelObj.name = $"Label";

                GameObject labelVersionObj = GameObject.Instantiate(dummyTabViewportContent.transform.Find("Label_Resolution").gameObject, modObj.transform);
                GameObject.Destroy(labelVersionObj.GetComponent<LocalizedText>());
                labelVersionObj.name = $"Label_Version";

                if (mod.updateLocation != ModUpdateLocation.None)
                {
                    GameObject labelUpdateObj = GameObject.Instantiate(dummyTabViewportContent.transform.Find("Label_Resolution").gameObject, modObj.transform);
                    GameObject.Destroy(labelUpdateObj.GetComponent<LocalizedText>());
                    labelUpdateObj.name = $"Label_Update";

                    TMPro.TextMeshProUGUI labelTextUpdate = labelUpdateObj.GetComponent<TMPro.TextMeshProUGUI>();
                    labelTextUpdate.fontSize = 26;
                    labelTextUpdate.enableWordWrapping = false;

                    labelUpdateObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    labelUpdateObj.transform.localPosition = new Vector2(640, 50);

                    ModUpdateMonitor monitor = labelUpdateObj.AddComponent<ModUpdateMonitor>();
                    monitor.mod = mod;
                }

                TMPro.TextMeshProUGUI labelText = labelObj.GetComponent<TMPro.TextMeshProUGUI>();
                labelText.text = mod.info.Name;
                labelText.enableWordWrapping = false;

                TMPro.TextMeshProUGUI labelTextVersion = labelVersionObj.GetComponent<TMPro.TextMeshProUGUI>();
                labelTextVersion.fontSize = 20;
                labelTextVersion.text = $"v{mod.info.Version}";
                labelTextVersion.enableWordWrapping = false;

                labelObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                labelVersionObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                labelObj.transform.localPosition = new Vector2(0, 50);
                labelVersionObj.transform.localPosition = new Vector2(0, -24+50);

                if (mod.reloadFunction != null)
                {
                    Button buttonClone = GameObject.Instantiate(ButtonPrefab, modObj.transform).GetComponent<Button>();
                    Text buttonText = buttonClone.GetComponentInChildren<Text>();
                    buttonClone.gameObject.SetActive(true);
                    buttonClone.name = $"Button_Reload";
                    buttonClone.transform.localPosition = new Vector2(384-2000, -50);
                    buttonClone.transform.localScale = Vector3.one;
                    buttonClone.onClick.RemoveAllListeners();
                    buttonClone.onClick.AddListener(() =>
                    {
                        LoggerInstance.Msg($"Reloading {mod.info.Name}...");
                        mod.reloadFunction();
                    });
                    buttonText.text = "Reload";
                }
            }

            VerticalLayoutGroup layout = scrollContent.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = scrollContent.AddComponent<ContentSizeFitter>();
            ScrollRect scroll = modsTabContent.transform.Find("Scroll View").GetComponent<ScrollRect>();
            layout.spacing = 64;
            layout.padding.top = 20;
            layout.padding.bottom = 40;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.horizontalNormalizedPosition = 0.5f;
            scroll.horizontal = false;
            scroll.inertia = true;

            optionsScript.TabButtonList.Add(button);
            optionsScript.TabContentList.Add(modsTabContent);
            optionsScript.SwitchPage(0);
        }

        [HarmonyPatch(typeof(CharacterSkinManager), "Awake")]
        private static class SkinManagerAwakePatch
        {
            private static bool Prefix(CharacterSkinManager __instance)
            {
                if (CharacterSkinManager.ins != null)
                {
                    GameObject.Destroy(__instance.gameObject);
                    return false;
                }

                CharacterSkinManager.ins = __instance;
                // do not call RefreshCharacterSkinDataFromFile();
                return false;
            }
        }

        [HarmonyPatch(typeof(IntroScript), "Awake")]
        private static class IntroAwakePatch
        {
            private static void Postfix(IntroScript __instance)
            {
                Debug.Log("Loading mods... Check the MelonLoader console window for loading progress");
                MelonLogger.Msg($"Loading mods...");
            }
        }

        [HarmonyPatch(typeof(IntroScript), "Update")]
        private static class IntroUpdatePatch
        {
            private static bool Prefix(IntroScript __instance)
            {
                if (!SkinsLoaded && (BeforeSkins.Count == 0 || BeforeSkins.All(callback => callback() == true)))
                {
                    MelonLogger.Msg($"Now loading character skins...");
                    CharacterSkinManager.ins.RefreshCharacterSkinDataFromFile();
                    SkinsLoaded = true;
                }

                if (!MenuLoaded && SkinsLoaded && !CharacterSkinManager.ins.IsLoading && (BeforeMainMenu.Count == 0 || BeforeMainMenu.All(callback => callback() == true)))
                {
                    if (!GC.ins.enabled)
                    {
                        Debug.Log("GC instance is disabled. Enabling...");
                        GC.ins.enabled = true;
                    }

                    Debug.Log("All mods loaded, Loading Main Menu...");
                    MelonLogger.Msg("All mods loaded, Loading Main Menu...");
                    GC.ins.LoadScene("MainMenu");
                    MenuLoaded = true;
                }

                return false;
            }
        }
    }
}