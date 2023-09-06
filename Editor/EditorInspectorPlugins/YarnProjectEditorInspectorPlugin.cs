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
    using Godot.Collections;

    /// <summary>
    /// This class is responsible for the yarn project editor inspector plugin.
    /// </summary>
    public partial class YarnProjectEditorInspectorPlugin : EditorInspectorPlugin {
        /// <inheritdoc/>
        public override bool _CanHandle(GodotObject targetObject) {

            return targetObject is YarnProject;
        }

        /// <inheritdoc/>
        public override void _ParseBegin(GodotObject targetObject) { }

        /// <inheritdoc/>
        public override bool _ParseProperty(GodotObject targetObject, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide) {
            // GD.Print(name);

            switch (name) {
                case "searchAssembliesForActions":
                case "SourceScripts":
                    // AddPropertyEditor(name, new SourceScriptsEditorProperty());
                    return false;
                case "baseLocalization":
                    AddPropertyEditor(name, new LocalizationPopupEditorProperty());
                    return true;
                default:
                    return true;
            }
        }
    }
}
#endif