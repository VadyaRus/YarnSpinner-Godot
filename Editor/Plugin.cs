/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/
#if TOOLS
namespace Yarn.GodotYarn.Editor {
    using Godot;

    /// <summary>
    /// This class is responsible for the plugin.
    /// </summary>
    [Tool]
    public partial class Plugin : EditorPlugin {
        /// <summary>
        /// The path to the addon.
        /// </summary>
        public const string AddonPath = "res://addons/YarnSpinner-Godot/";

        /// <summary>
        /// The path to the runtime scripts.
        /// </summary>
        public const string RuntimePath = AddonPath + "Runtime/";

        /// <summary>
        /// The path to the editor scripts.
        /// </summary>
        public const string EditorPath = AddonPath + "Editor/";

        private YarnImporterPlugin yarnImporterPlugin;
        private YarnProjectEditorInspectorPlugin yarnProjectInspector;

        /// <inheritdoc/>
        public override void _EnterTree() {
            yarnImporterPlugin = new YarnImporterPlugin();
            AddImportPlugin(yarnImporterPlugin);

            yarnProjectInspector = new YarnProjectEditorInspectorPlugin();
            AddInspectorPlugin(yarnProjectInspector);

            var gui = GetEditorInterface().GetBaseControl();

            AddCustomType(
                "YarnScript",
                "Resource",
                ResourceLoader.Load<Script>(RuntimePath + "YarnScript.cs"),
                ResourceLoader.Load<Texture2D>(EditorPath + "Icons/YarnScript Icon.svg"));

            AddCustomType(
                "YarnProject",
                "Resource",
                ResourceLoader.Load<Script>(RuntimePath + "YarnProject.cs"),
                ResourceLoader.Load<Texture2D>(EditorPath + "Icons/YarnProject Icon.svg"));

            AddCustomType(
                "DialogueRunner",
                "Control",
                ResourceLoader.Load<Script>(RuntimePath + "DialogueRunner.cs"),
                gui.GetThemeIcon("Control", "EditorIcons"));

            AddCustomType(
                "DialogueAdvanceInput",
                "Node",
                ResourceLoader.Load<Script>(RuntimePath + "Views/DialogueAdvanceInput.cs"),
                gui.GetThemeIcon("Node", "EditorIcons"));

            AddCustomType(
                "TextLineProvider",
                "Node",
                ResourceLoader.Load<Script>(RuntimePath + "LineProviders/TextLineProvider.cs"),
                gui.GetThemeIcon("Node", "EditorIcons"));

            AddCustomType(
                "Declaration",
                "Resource",
                ResourceLoader.Load<Script>(RuntimePath + "Declaration.cs"),
                gui.GetThemeIcon("Object", "EditorIcons"));

            AddCustomType(
                "LanguageToSourceAsset",
                "Resource",
                ResourceLoader.Load<Script>(RuntimePath + "LanguageToSourceAsset.cs"),
                gui.GetThemeIcon("Object", "EditorIcons"));

            AddAutoloadSingleton("NodeFindUtility", RuntimePath + "NodeFindUtility.cs");
            AddAutoloadSingleton("DefaultActions", RuntimePath + "Commands/DefaultActions.cs");

            GD.Print("YarnSpinner-Godot plugin initialized");
        }

        /// <inheritdoc/>
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

            if (yarnImporterPlugin != null) {
                RemoveImportPlugin(yarnImporterPlugin);
                yarnImporterPlugin = null;
            }

            if (yarnProjectInspector != null) {
                RemoveInspectorPlugin(yarnProjectInspector);
                yarnProjectInspector = null;
            }
        }
    }
}
#endif