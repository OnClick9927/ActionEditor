using System.IO;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    [FilePath("ProjectSettings/ActionEditorSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ActionEditorProjectSettings :
        ScriptableSingleton<ActionEditorProjectSettings>
    {
        [SerializeField] private Prefs.SerializedData data = new();
        [SerializeField, HideInInspector] private bool legacySettingsMigrated;

        internal Prefs.SerializedData Data
        {
            get
            {
                EnsureLoaded();
                return data;
            }
        }

        internal void EnsureLoaded()
        {
            data ??= new Prefs.SerializedData();
            if (legacySettingsMigrated) return;

            string legacyPath = Path.Combine(Application.dataPath, "Editor",
                "ActionEditor.txt");
            if (File.Exists(legacyPath))
            {
                string json = File.ReadAllText(legacyPath);
                if (!string.IsNullOrWhiteSpace(json))
                    JsonUtility.FromJsonOverwrite(json, data);
            }
            legacySettingsMigrated = true;
            Save(true);
        }

        internal void SaveSettings()
        {
            EnsureLoaded();
            Save(true);
        }
    }
}
