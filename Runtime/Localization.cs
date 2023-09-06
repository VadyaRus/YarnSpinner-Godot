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
namespace Yarn.GodotYarn {
    #if TOOLS
    using System.Linq;
    #endif
    using System;
    using System.Collections.Generic;
    using Godot;
    using Node = Godot.Node;

    /// <summary>
    /// Represents a single localization of a Yarn project.
    /// </summary>
    [Tool]
    public partial class Localization : Resource {
        [Export]
        private string stringFile;

        [Export]
        private string localeCode;

        [Export]
        private Godot.Collections.Dictionary<string, string> stringTable = new ();
        [Export]
        private Godot.Collections.Dictionary<string, GodotObject> assetTable = new ();

        private Godot.Collections.Dictionary<string, string> runtimeStringTable = new ();

        [Export]
        private bool containLocalizedAsset;

        [Export]
        private bool usesAddressableAssets;

        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> class.
        /// </summary>
        public Localization() {
            this.localeCode = System.Globalization.CultureInfo.CurrentCulture.Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> class.
        /// </summary>
        /// <param name="localeCode">The locale code for this localization.</param>
        public Localization(string localeCode) {
            this.localeCode = localeCode;
        }

        /// <summary>
        /// Gets or sets the string file for this localization.
        /// </summary>
        public string StringFile { get => stringFile; set => stringFile = value; }

        /// <summary>
        /// Gets or sets the locale code for this localization.
        /// </summary>
        public string LocaleCode { get => localeCode; set => localeCode = value; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Localization"/>
        /// contains assets that are linked to strings.
        /// </summary>
        public bool ContainLocalizedAsset {
            get => containLocalizedAsset;
            set => containLocalizedAsset = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Localization"/>
        /// makes use of Addressable Assets (<see langword="true"/>), or if it
        /// stores its assets as direct references (<see langword="false"/>).
        /// </summary>
        /// <remarks>
        /// If this property is <see langword="true"/>, <see
        /// cref="GetLocalizedObject"/> and <see
        /// cref="ContainsLocalizedObject"/> should not be used to
        /// retrieve localised objects. Instead, the Addressable Assets API
        /// should be used.
        /// </remarks>
        public bool UsesAddressableAssets {
            get => usesAddressableAssets;
            set => usesAddressableAssets = value;
        }

        /// <summary>
        /// Returns the localized string for the given key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>The localized string.</returns>
        public string GetLocalizedString(string key) {
            // GD.Print(key);

            if (runtimeStringTable.TryGetValue(key, out string result)) {
                return result;
            }

            if (stringTable.TryGetValue(key, out result)) {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Returns a boolean value indicating whether this <see
        /// cref="Localization"/> contains a string with the given key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns><see langword="true"/> if this Localization has a string
        /// for the given key; <see langword="false"/> otherwise.</returns>
        public bool ContainsLocalizedString(string key) => runtimeStringTable.ContainsKey(key) || stringTable.ContainsKey(key);

        /// <summary>
        /// Adds a new string to the runtime string table.
        /// </summary>
        /// <remarks>
        /// This method updates the localisation's runtime string table, which
        /// is useful for adding or changing the localisation during gameplay or
        /// in a built player. It doesn't modify the asset on disk, and any
        /// changes made will be lost when gameplay ends.
        /// </remarks>
        /// <param name="key">The key for this string (generally, the line
        /// ID.)</param>
        /// <param name="value">The user-facing text for this string, in the
        /// language specified by <see cref="LocaleCode"/>.</param>
        public void AddLocalizedString(string key, string value) {
            if (runtimeStringTable.ContainsKey(key) == false) {
                runtimeStringTable.Add(key, value);
            }
            else {
                runtimeStringTable[key] = value;
            }
        }

        /// <summary>
        /// Adds a collection of strings to the runtime string table.
        /// </summary>
        /// <inheritdoc cref="AddLocalizedString(string, string)"
        /// path="/remarks"/>
        /// <param name="strings">The collection of keys and strings to
        /// add.</param>
        public void AddLocalizedStrings(IEnumerable<KeyValuePair<string, string>> strings) {
            foreach (var entry in strings) {
                AddLocalizedString(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Adds a collection of strings to the runtime string table.
        /// </summary>
        /// <inheritdoc cref="AddLocalizedString(string, string)"
        /// path="/remarks"/>
        /// <param name="strings">The collection of <see
        /// cref="StringTableEntry"/> objects to add.</param>
        public void AddLocalizedStrings(IEnumerable<StringTableEntry> stringTableEntries) {
            foreach (var entry in stringTableEntries) {
                // GD.Print("add " + entry.ID + ": " + entry.Text);
                AddLocalizedString(entry.ID, entry.Text);
            }
        }

        /// <summary>
        /// Returns the localized object for the given key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>The localized object.</returns>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        public T GetLocalizedObject<T>(string key)
        where T : Node {
            if (usesAddressableAssets) {
                GD.PushWarning($"Localization {key} uses addressable assets. Use the Addressable Assets API to load the asset.");
            }

            assetTable.TryGetValue(key, out var result);

            if (result is T resultAsTargetObject) {
                return resultAsTargetObject;
            }

            return null;
        }

        /// <summary>
        /// Sets the localized object for a given key.
        /// </summary>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The object to set.</param>
        /// <typeparam name="T">The type of object to set.</typeparam>
        public void SetLocalizedObject<T>(string key, T value)
        where T : GodotObject => assetTable.Add(key, value);

        /// <summary>
        /// Returns a boolean value indicating whether this <see cref="Localization"/> contains an object with the given key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns><see langword="true"/> if this Localization has an object for the given key; <see langword="false"/> otherwise.</returns>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        public bool ContainsLocalizedObject<T>(string key)
        where T : GodotObject => assetTable.ContainsKey(key) && assetTable[key] is T;

        /// <summary>
        /// Adds a new object to the runtime asset table.
        /// </summary>
        /// <param name="key">The key for this object (generally, the line ID.)</param>
        /// <param name="value">The object to add.</param>
        /// <typeparam name="T">The type of object to add.</typeparam>
        public void AddLocalizedObject<T>(string key, T value)
        where T : GodotObject => assetTable.Add(key, value);

        /// <summary>
        /// Adds a collection of objects to the runtime asset table.
        /// </summary>
        /// <param name="objects">The collection of <see cref="GodotObject"/>.</param>
        /// <typeparam name="T">The type of object to add.</typeparam>
        public void AddLocalizedObjects<T>(IEnumerable<KeyValuePair<string, T>> objects)
        where T : GodotObject {
            foreach (var entry in objects) {
                if (assetTable.ContainsKey(entry.Key) == false) {
                    assetTable.Add(entry.Key, entry.Value);
                }
                else {
                    assetTable[entry.Key] = entry.Value;
                }
            }
        }

        /// <summary>
        /// Clears the entries for this localization.
        /// </summary>
        public virtual void Clear() {
            stringTable.Clear();
            assetTable.Clear();
            runtimeStringTable.Clear();
        }

        /// <summary>
        /// Gets the line IDs present in this localization.
        /// </summary>
        /// <remarks>
        /// The line IDs can be used to access the localized text or asset
        /// associated with a line.
        /// </remarks>
        /// <returns>The line IDs.</returns>
        public IEnumerable<string> GetLineIDs() {
            var allKeys = new List<string>();

            var runtimeKeys = runtimeStringTable.Keys;
            var compileTimeKeys = stringTable.Keys;

            allKeys.AddRange(runtimeKeys);
            allKeys.AddRange(compileTimeKeys);

            return allKeys;
        }

        /// <summary>
        /// Returns the address that should be used to fetch an asset suitable
        /// for a specific line in a specific language.
        /// </summary>
        /// <remarks>
        /// This method is useful for creating an address for use with the
        /// Addressable Assets system.
        /// </remarks>
        /// <param name="lineID">The line ID to use when generating the
        /// address.</param>
        /// <param name="language">The language to use when generating the
        /// address.</param>
        /// <returns>The address to use.</returns>
        internal static string GetAddressForLine(string lineID, string language) {
            return $"line_{language}_{lineID.Replace("line:", string.Empty)}";
        }

#if TOOLS
        /// <summary>
        /// Adds a new string to the string table.
        /// </summary>
        /// <remarks>
        /// This method updates the localisation asset on disk. It is not
        /// recommended to call this method during play mode, because changes
        /// will persist after you leave and may cause conflicts.
        /// </remarks>
        /// <param name="key">The key for this string (generally, the line
        /// ID.)</param>
        /// <param name="value">The user-facing text for this string, in the
        /// language specified by <see cref="LocaleCode"/>.</param>
        internal void AddLocalizedStringToAsset(string key, string value) {
            if (stringTable.ContainsKey(key) == false) {
                stringTable.Add(key, value);
            }
            else {
                stringTable[key] = value;
            }
            this.EmitChanged();
        }

        /// <summary>
        /// Adds a collection of strings to the string table.
        /// </summary>
        /// <inheritdoc cref="AddLocalizedStringToAsset(string, string)"
        /// path="/remarks"/>
        /// <param name="strings">The collection of <see
        /// cref="StringTableEntry"/> objects to add.</param>
        internal void AddLocalizedStringsToAsset(IEnumerable<StringTableEntry> stringTableEntries) {
            foreach (var entry in stringTableEntries) {
                // GD.Print("add " + entry.ID + ": " + entry.Text);
                AddLocalizedStringToAsset(entry.ID, entry.Text);
            }
        }
#endif
    }
}