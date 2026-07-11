using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SMBZModsMenu;
using TinyJSON;
using MelonLoader;

public class ModUpdateMonitor : MonoBehaviour
{
    public ModEntry mod;

    TMPro.TextMeshProUGUI Comp_UpdateLabel;
    string foundName;

    void Awake()
    {
        Comp_UpdateLabel = GetComponent<TMPro.TextMeshProUGUI>();
    }

    void Start()
    {
        CheckForUpdates();
    }

    public void CheckForUpdates()
    {
        if (Comp_UpdateLabel == null) return;
        if (mod == null || mod.updateLocation == ModUpdateLocation.None)
        {
            Comp_UpdateLabel.enabled = false;
            return;
        }

        StartCoroutine(CheckForUpdates_Work());
    }

    IEnumerator CheckForUpdates_Work()
    {
        Comp_UpdateLabel.enabled = true;

        if (mod.updateLocation == ModUpdateLocation.Github)
        {
            yield return CheckForUpdates_GitHub();
            yield break;
        }


        if (mod.updateLocation == ModUpdateLocation.Gamebanana_Name)
        {
            if (mod.gamebananaCacheID == null)
                mod.gamebananaCacheID = Melon<Core>.Instance.gamebananaCacheCategory.CreateEntry(mod.updateRepo, string.Empty);

            if (mod.gamebananaCacheID.Value != string.Empty)
            {
                yield return CheckForUpdates_GameBanana_ID(mod.gamebananaCacheID.Value);
                yield break;
            }

            yield return CheckForUpdates_GameBanana_Name();
            yield break;
        }


        if (mod.updateLocation == ModUpdateLocation.Gamebanana_ID)
        {
            yield return CheckForUpdates_GameBanana_ID(mod.updateRepo);
            yield break;
        }
    }

    IEnumerator CheckForUpdates_GitHub()
    {
        Comp_UpdateLabel.text = "Checking for updates on GitHub...";
        UnityWebRequest www = UnityWebRequest.Get($"https://api.github.com/repos/{mod.updateRepo}/releases");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Comp_UpdateLabel.text = $"Failed to check for updates. {www.result}";
            yield break;
        }

        Variant json = JSON.Load(www.downloadHandler.text);
        if (json == null)
        {
            Comp_UpdateLabel.text = "Failed to check for updates. JSON parse failed";
            yield break;
        }

        ProxyObject jsonObj = json as ProxyObject;
        if (jsonObj != null)
        {
            Comp_UpdateLabel.text = $"Failed to check for updates: {jsonObj["status"]} {jsonObj["message"]}";
            yield break;
        }

        ProxyArray jsonArray = json as ProxyArray;
        if (jsonArray == null)
        {
            Comp_UpdateLabel.text = "Failed to check for updates. JSON is not an array";
            yield break;
        }

        if (jsonArray.Count == 0)
        {
            Comp_UpdateLabel.text = "No updates available";
            yield break;
        }

        ProxyObject latestRelease = jsonArray[0] as ProxyObject;
        if (latestRelease["tag_name"] != mod.info.Version && latestRelease["tag_name"] != $"v{mod.info.Version}")
            Comp_UpdateLabel.text = $"Update {latestRelease["tag_name"]} is available on GitHub!";
        else
            Comp_UpdateLabel.text = "No updates available";
    }

    IEnumerator GetGamebananaSubmissionName(string id)
    {
        UnityWebRequest www = UnityWebRequest.Get($"https://api.gamebanana.com/Core/Item/Data?itemtype=Mod&itemid={id}&fields=name");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            yield break;
        }

        Variant json = JSON.Load(www.downloadHandler.text);
        if (json == null)
        {
            yield break;
        }

        // error
        ProxyObject jsonObj = json as ProxyObject;
        if (jsonObj != null)
        {
            yield break;
        }

        // root JSON array
        ProxyArray jsonArray = json as ProxyArray;
        if (jsonArray == null)
        {
            yield break;
        }

        // first field from "fields" arg in the URL (name)
        foundName = jsonArray[0];
    }

    IEnumerator CheckForUpdates_GameBanana_Name()
    {
        int page = 1;

        Comp_UpdateLabel.text = "Checking for updates on GameBanana...";
        UnityWebRequest www;

        while (true)
        {
            www = UnityWebRequest.Get($"https://api.gamebanana.com/Core/List/New?gameid=20694&page={page}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Comp_UpdateLabel.text = $"Failed to check for updates. {www.result}";
                break;
            }

            Variant json = JSON.Load(www.downloadHandler.text);
            if (json == null)
            {
                Comp_UpdateLabel.text = "Failed to check for updates. JSON parse failed";
                yield break;
            }

            // error
            ProxyObject jsonObj = json as ProxyObject;
            if (jsonObj != null)
            {
                Comp_UpdateLabel.text = $"Failed to check for updates: {jsonObj["error_code"]}: {jsonObj["error"]}";
                yield break;
            }

            // array containing the list of submissions
            // each item in the array is another array with 2 items: string submissionType, int submissionID
            // check each submission that matches the mod's name case-insensitive
            ProxyArray jsonArray = json as ProxyArray;
            if (jsonArray == null)
            {
                Comp_UpdateLabel.text = "Failed to check for updates. JSON is not an array";
                yield break;
            }

            if (jsonArray.Count == 0)
            {
                Comp_UpdateLabel.text = $"Failed to check for updates. GameBanana submission '{mod.updateRepo}' not found";
                yield break;
            }

            foreach (ProxyArray submission in jsonArray)
            {
                (string name, int id) = (submission[0], submission[1]);
                if (name != "Mod") continue;

                yield return GetGamebananaSubmissionName(id.ToString());
                if (foundName.ToLower() == mod.updateRepo.ToLower())
                {
                    mod.gamebananaCacheID.Value = id.ToString();
                    yield return CheckForUpdates_GameBanana_ID(id.ToString());
                    yield break;
                }
            }

            page++;
        }
    }

    IEnumerator CheckForUpdates_GameBanana_ID(string id)
    {
        Comp_UpdateLabel.text = "Checking for updates on GameBanana...";
        UnityWebRequest www = UnityWebRequest.Get($"https://api.gamebanana.com/Core/Item/Data?itemtype=Mod&itemid={id}&fields=Updates().aGetLatestUpdates()");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Comp_UpdateLabel.text = $"Failed to check for updates. {www.result}";
            yield break;
        }

        Variant json = JSON.Load(www.downloadHandler.text);
        if (json == null)
        {
            Comp_UpdateLabel.text = "Failed to check for updates. JSON parse failed";
            yield break;
        }

        ProxyObject jsonObj = json as ProxyObject;
        if (jsonObj != null)
        {
            Comp_UpdateLabel.text = $"Failed to check for updates: {jsonObj["error_code"]}: {jsonObj["error"]}";
            yield break;
        }

        // root JSON array
        ProxyArray jsonArray = json as ProxyArray;
        if (jsonArray == null)
        {
            Comp_UpdateLabel.text = "Failed to check for updates. JSON is not an array";
            yield break;
        }

        // first field from "fields" arg in the URL (all updates)
        jsonArray = jsonArray[0] as ProxyArray;
        if (jsonArray == null || jsonArray.Count == 0)
        {
            Comp_UpdateLabel.text = "No updates available";
            yield break;
        }

        string latestVersion = jsonArray[0]["_sVersion"];
        if (latestVersion != mod.info.Version && latestVersion != $"v{mod.info.Version}")
            Comp_UpdateLabel.text = $"Update {latestVersion} is available on GameBanana!";
        else
            Comp_UpdateLabel.text = "No updates available";
    }
}