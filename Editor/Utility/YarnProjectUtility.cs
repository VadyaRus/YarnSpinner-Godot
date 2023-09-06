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
/*
#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;

using Yarn.GodotYarn;

namespace Yarn.GodotYarn.Editor {
    /// <summary>
    /// Contains methods for performing high-level operations on Yarn
    /// projects, and their associated localization files.
    /// </summary>
    internal static class YarnProjectUtility {
        /// <summary>
        /// Verifies the TextAsset referred to by <paramref
        /// name="destinationLocalizationAsset"/>, and updates it if
        /// necessary.
        /// </summary>
        /// <param name="baseLocalizationStrings">A collection of <see
        /// cref="StringTableEntry"/></param>
        /// <param name="language">The language that <paramref
        /// name="destinationLocalizationAsset"/> provides strings
        /// for.false</param>
        /// <param name="destinationLocalizationAsset">A TextAsset
        /// containing localized strings in CSV format.</param>
        /// <returns>Whether <paramref
        /// name="destinationLocalizationAsset"/> was modified.</returns>
        private static bool UpdateLocalizationFile(EditorInterface editorInterface, IEnumerable<StringTableEntry> baseLocalizationStrings, string language, TextAsset destinationLocalizationAsset) {
            var translatedStrings = StringTableEntry.ParseFromCSV(destinationLocalizationAsset.text);

            // Convert both enumerables to dictionaries, for easier lookup
            var baseDictionary = baseLocalizationStrings.ToDictionary(entry => entry.ID);
            var translatedDictionary = translatedStrings.ToDictionary(entry => entry.ID);

            // The list of line IDs present in each localisation
            var baseIDs = baseLocalizationStrings.Select(entry => entry.ID);
            var translatedIDs = translatedStrings.Select(entry => entry.ID);

            // The list of line IDs that are ONLY present in each
            // localisation
            var onlyInBaseIDs = baseIDs.Except(translatedIDs);
            var onlyInTranslatedIDs = translatedIDs.Except(baseIDs);

            // Tracks if the translated localisation needed modifications
            // (either new lines added, old lines removed, or changed lines
            // flagged)
            var modificationsNeeded = false;

            // Remove every entry whose ID is only present in the
            // translated set. This entry has been removed from the base
            // localization.
            foreach (var id in onlyInTranslatedIDs.ToList()) {
                translatedDictionary.Remove(id);
                modificationsNeeded = true;
            }

            // Conversely, for every entry that is only present in the base
            // localisation, we need to create a new entry for it.
            foreach (var id in onlyInBaseIDs) {
                StringTableEntry baseEntry = baseDictionary[id];
                var newEntry = new StringTableEntry(baseEntry) {
                    // Empty this text, so that it's apparent that a
                    // translated version needs to be provided.
                    Text = string.Empty,
                    Language = language,
                };
                translatedDictionary.Add(id, newEntry);
                modificationsNeeded = true;
            }
            // Finally, we need to check for any entries in the translated
            // localisation that:
            // 1. have the same line ID as one in the base, but
            // 2. have a different Lock (the hash of the text), which
            //    indicates that the base text has changed.

            // First, get the list of IDs that are in both base and
            // translated, and then filter this list to any where the lock
            // values differ
            var outOfDateLockIDs = baseDictionary.Keys
                .Intersect(translatedDictionary.Keys)
                .Where(id => baseDictionary[id].Lock != translatedDictionary[id].Lock);

            // Now loop over all of these, and update our translated
            // dictionary to include a note that it needs attention
            foreach (var id in outOfDateLockIDs) {
                // Get the translated entry as it currently exists
                var entry = translatedDictionary[id];

                // Include a note that this entry is out of date
                entry.Text = $"(NEEDS UPDATE) {entry.Text}";

                // Update the lock to match the new one
                entry.Lock = baseDictionary[id].Lock;

                // Put this modified entry back in the table
                translatedDictionary[id] = entry;

                modificationsNeeded = true;
            }

            // We're all done!
            if (modificationsNeeded == false) {
                // No changes needed to be done to the translated string
                // table entries. Stop here.
                return false;
            }

            // We need to produce a replacement CSV file for the translated
            // entries.

            var outputStringEntries = translatedDictionary.Values
                .OrderBy(entry => entry.File)
                .ThenBy(entry => int.Parse(entry.LineNumber));

            var outputCSV = StringTableEntry.CreateCSV(outputStringEntries);

            // Write out the replacement text to this existing file,
            // replacing its existing contents
            editorInterface.GetResourceFilesystem().GetFilesystemPath()
            var outputFile = AssetDatabase.GetAssetPath(destinationLocalizationAsset);
            File.WriteAllText(outputFile, outputCSV, System.Text.Encoding.UTF8);

            // Tell the asset database that the file needs to be reimported
            AssetDatabase.ImportAsset(outputFile);

            // Signal that the file was changed
            return true;
        }

       internal static void ConvertImplicitVariableDeclarationsToExplicit(YarnProjectImporter yarnProjectImporter) {
            var allFilePaths = yarnProjectImporter.sourceScripts.Select(textAsset => AssetDatabase.GetAssetPath(textAsset));

            var library = new Library();
            ActionManager.RegisterFunctions(library);

            var explicitDeclarationsCompilerJob = Compiler.CompilationJob.CreateFromFiles(AssetDatabase.GetAssetPath(yarnProjectImporter));
            explicitDeclarationsCompilerJob.Library = library;

            Compiler.CompilationResult explicitResult;

            try {
                explicitResult = Compiler.Compiler.Compile(explicitDeclarationsCompilerJob);
            } catch (System.Exception e) {
                Debug.LogError($"Compile error: {e}");
                return;
            }

            var implicitDeclarationsCompilerJob = Compiler.CompilationJob.CreateFromFiles(allFilePaths, library);
            implicitDeclarationsCompilerJob.CompilationType = Compiler.CompilationJob.Type.DeclarationsOnly;
            implicitDeclarationsCompilerJob.VariableDeclarations = explicitResult.Declarations;

            Compiler.CompilationResult implicitResult;

            try {
                implicitResult = Compiler.Compiler.Compile(implicitDeclarationsCompilerJob);
            } catch (System.Exception e) {
                Debug.LogError($"Compile error: {e}");
                return;
            }

            var implicitDeclarations = implicitResult.Declarations.Where(d => !(d.Type is Yarn.FunctionType) && d.IsImplicit);

            var output = Yarn.Compiler.Utility.GenerateYarnFileWithDeclarations(explicitResult.Declarations.Concat(implicitDeclarations), "Program");

            File.WriteAllText(yarnProjectImporter.assetPath, output, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(yarnProjectImporter.assetPath);

        }
    }
}

#endif
*/