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
using Godot;
using Godot.Collections;

namespace Yarn.GodotYarn.Editor {
    [Tool]
    public partial class YarnImporterPlugin : EditorImportPlugin {
        private const string YARN_TRACKER_PATH = Plugin.AddonPath + ".tracked_yarn_files";

        public override string _GetImporterName() => "com.YarnSpinner-Godot.YarnScript";
        public override string _GetVisibleName() => "Yarn Script";
        public override float _GetPriority() => 1.0f;
        public override int _GetImportOrder() => 0;
        public override string[] _GetRecognizedExtensions() => new string[] { "yarn" };
        public override string _GetSaveExtension() => "tres";
        public override string _GetResourceType() => "Resource";
        public override int _GetPresetCount() => 1;
        public override string _GetPresetName(int presetIndex) => "Default";
        public override Array<Dictionary> _GetImportOptions(string path, int presetIndex) {
            return new Array<Dictionary>();
        }

        public override Error _Import(string sourceFile, string savePath, Dictionary options, Array<string> platformVariants, Array<string> genFiles) {
            // GD.Print("Importing -> ", sourceFile);

            // string content = string.Empty;

            // using(FileAccess access = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read)) {
            //     content = access.GetAsText();
            // }

            // After we're done caching the tracked files, we now import the yarn file

            string saveFilePath = $"{savePath}.{_GetSaveExtension()}";

            YarnScript yarnFile = new YarnScript();
            yarnFile.ResourcePath = sourceFile;
            yarnFile.ResourceName = sourceFile.GetFile();

            return ResourceSaver.Save(yarnFile, saveFilePath);
        }
    }
}
#endif