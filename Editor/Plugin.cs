#if TOOLS
using Godot;

namespace Yarn.GodotYarn.Editor {
    [Tool]
    public partial class Plugin : EditorPlugin {
        public const string ADDON_PATH = "res://addons/YarnSpinner-Godot/";
        public const string RUNTIME_PATH = ADDON_PATH + "Runtime/";
        public const string EDITOR_PATH = ADDON_PATH + "Editor/";

        private YarnImporterPlugin _yarnImporterPlugin;
        private YarnProjectEditorInspectorPlugin _yarnProjectInspector;


        public override void _EnterTree() {
            _yarnImporterPlugin = new YarnImporterPlugin();
            AddImportPlugin(_yarnImporterPlugin);

            _yarnProjectInspector = new YarnProjectEditorInspectorPlugin();
            AddInspectorPlugin(_yarnProjectInspector);

            var gui = GetEditorInterface().GetBaseControl();


            AddCustomType(
                "YarnScript",
                "Resource",
                ResourceLoader.Load<Script>(RUNTIME_PATH + "YarnScript.cs"),
                ResourceLoader.Load<Texture2D>(EDITOR_PATH + "Icons/YarnScript Icon.svg")
            );
            AddCustomType(
                "YarnProject",
                "Resource",
                ResourceLoader.Load<Script>(RUNTIME_PATH + "YarnProject.cs"),
                ResourceLoader.Load<Texture2D>(EDITOR_PATH + "Icons/YarnProject Icon.svg")
            );
            AddCustomType(
                "DialogueRunner",
                "Control",
                ResourceLoader.Load<Script>(RUNTIME_PATH + "DialogueRunner.cs"),
                gui.GetThemeIcon("Control", "EditorIcons")
            );
            AddCustomType(
                "DialogueAdvanceInput",
                "Node",
                ResourceLoader.Load<Script>(RUNTIME_PATH + "Views/DialogueAdvanceInput.cs"),
                gui.GetThemeIcon("Node", "EditorIcons")
            );
            AddCustomType(
                "TextLineProvider",
                "Node",
                ResourceLoader.Load<Script>(RUNTIME_PATH + "LineProviders/TextLineProvider.cs"),
                gui.GetThemeIcon("Node", "EditorIcons")
            );
            AddCustomType(
                "Declaration",
                "Resource",
                ResourceLoader.Load<Script>(RUNTIME_PATH + "Declaration.cs"),
                gui.GetThemeIcon("Object", "EditorIcons")
            );
            AddCustomType(
                "LanguageToSourceAsset",
                "Resource",
                ResourceLoader.Load<Script>(RUNTIME_PATH + "LanguageToSourceAsset.cs"),
                gui.GetThemeIcon("Object", "EditorIcons")
            );

            AddAutoloadSingleton("NodeFindUtility", RUNTIME_PATH + "NodeFindUtility.cs");
            AddAutoloadSingleton("DefaultActions", RUNTIME_PATH + "Commands/DefaultActions.cs");

            GD.Print("YarnSpinner-Godot plugin initialized");
        }

        public override void _ExitTree() {
            RemoveAutoloadSingleton("NodeFindUtility");
            RemoveAutoloadSingleton("DefaultActions");

            RemoveCustomType("TextLineProvider");
            RemoveCustomType("DialogueAdvanceInput");
            RemoveCustomType("LanguageToSourceAsset");
            RemoveCustomType("Declaration");
            RemoveCustomType("DialogueRunner");
            RemoveCustomType("YarnProject");
            RemoveCustomType("YarnScript");

            if (_yarnImporterPlugin != null) {
                RemoveImportPlugin(_yarnImporterPlugin);
                _yarnImporterPlugin = null;
            }

            if (_yarnProjectInspector != null) {
                RemoveInspectorPlugin(_yarnProjectInspector);
                _yarnProjectInspector = null;
            }
        }
    }
}
#endif