#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FS_Core
{
    public partial class FSSystemsSetup
    {
        public static FSSystemInfo RopeSwingingSystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.Player,
            selected: false,
            systemName: "Rope Swinging System",
            displayName: "Rope Swing",
            prefabName: "Swinging Controller",
            welcomeEditorShowKey: "RopeSwingingSystem_WelcomeWindow_Opened",
            mobileControllerPrefabName: "Swing Mobile Controller"
        );

        static string RopeSwingingSystemWelcomeEditorKey => RopeSwingingSystemSetup.welcomeEditorShowKey;


        [InitializeOnLoadMethod]
        public static void LoadRopeSwingingSystem()
        {
            if (!string.IsNullOrEmpty(RopeSwingingSystemWelcomeEditorKey) && !EditorPrefs.GetBool(RopeSwingingSystemWelcomeEditorKey, false))  
            {
                SessionState.SetBool(welcomeWindowOpenKey, false);
                EditorPrefs.SetBool(RopeSwingingSystemWelcomeEditorKey, true);
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }
    }
}
#endif

