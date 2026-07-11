using MelonLoader;
using UnityEngine.Events;

namespace SMBZModsMenu
{
    public enum ModUpdateLocation
    {
        None,
        Github,

        /// <summary>
        /// GameBanana submission by its' exact name
        /// </summary>
        Gamebanana_Name,

        /// <summary>
        /// GameBanana submission by its' ID
        /// </summary>
        Gamebanana_ID,
    };

    public class ModEntry
    {
        /// <summary>
        /// MelonInfoAttribute of the mod (MelonMod.Info)
        /// </summary>
        public MelonInfoAttribute info;

        /// <summary>
        /// function to be called when clicking the Reload button for this mod.
        /// can be left null.
        /// </summary>
        public UnityAction reloadFunction;

        /// <summary>
        /// online location to check for updates
        /// </summary>
        public ModUpdateLocation updateLocation;

        /// <summary>
        /// github repository (username/repo) or gamebanana submission name/ID
        /// </summary>
        public string updateRepo;

        /// <summary>
        /// used when ModUpdateLocation is Gamebanana_Name.
        /// after finding the ID of the submission, it is saved here
        /// to avoid searching through gamebanana submissions again
        /// </summary>
        internal MelonPreferences_Entry<string> gamebananaCacheID;
    }
}
