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
    using System.Linq;
    using System.Reflection;
    using Godot;
    using Godot.Collections;

    using FileAccess = Godot.FileAccess;

    /// <summary>
    /// This class is responsible for the yarn project resource.
    /// </summary>
    [Tool]
    #if GODOT4_1_OR_GREATER
    [GlobalClass]
    [Icon("res://addons/YarnSpinner-Godot/Editor/Icons/YarnProject Icon.svg")]
    #endif
    public partial class YarnProject : Resource {
        [Export]
        public byte[] compiledYarnProgram;

        [Export]
        public Localization baseLocalization;

        [Export]
        public Array<Localization> localizations = new Array<Localization>();

        [Export]
        public LineMetadata lineMetadata;

        [Export]
        public LocalizationType localizationType;

        [Export]
        public Declaration[] declarations = new Declaration[0];

        /// <summary>
        /// The cached result of deserializing <see
        /// cref="compiledYarnProgram"/>.
        /// </summary>
        private Program cachedProgram = null;

        /// <summary>
        /// The names of assemblies that <see cref="ActionManager"/> should look
        /// for commands and functions in when this project is loaded into a
        /// <see cref="DialogueRunner"/>.
        /// </summary>
        [Export]
        public string[] searchAssembliesForActions = System.Array.Empty<string>();

        private string[] sourceScripts = null;

        /// <summary>
        /// The cached result of reading the default values from the <see
        /// cref="Program"/>.
        /// </summary>
        private System.Collections.Generic.Dictionary<string, System.IConvertible> initialValues;

        // ok assumption is that this can be lazy loaded and then kept around
        // as not every node has headers you care about but some will and be read A LOT
        // so we will fill a dict on request and just keep it around
        // is somewhat unnecessary as people can get this out themselves if they want
        // but I think peeps will wanna use headers like a dictionary
        // so we will do the transformation for you
        private Dictionary<string, Dictionary<string, Array<string>>> nodeHeaders = new ();

        // private string defaultLanguage = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="YarnProject"/> class.
        /// </summary>
        public YarnProject() {
            baseLocalization = new Localization();
        }

        /// <summary>
        /// Gets the names of all nodes contained within the <see cref="Program"/>.
        /// </summary>
        public string[] NodeNames {
            get {
                return Program.Nodes.Keys.ToArray();
            }
        }

        [Export(PropertyHint.File, "*.yarn")]
        public string[] SourceScripts {
            get => sourceScripts;
            set {
                sourceScripts = value;

                #if TOOLS

                var result = YarnProjectEditorCompiler.CompileYarnProject(this);

                GD.Print($"Yarn Project compilation result: {result}");

                #endif
            }
        }

        /// <summary>
        /// The default values of all declared or inferred variables in the
        /// <see cref="Program"/>.
        /// Organised by their name as written in the yarn files.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, System.IConvertible> InitialValues {
            get {
                if (initialValues != null) {
                    return initialValues;
                }

                initialValues = new System.Collections.Generic.Dictionary<string, System.IConvertible>();

                foreach (var pair in Program.InitialValues) {
                    var value = pair.Value;
                    switch (value.ValueCase) {
                        case Yarn.Operand.ValueOneofCase.StringValue: {
                            initialValues[pair.Key] = value.StringValue;
                            break;
                        }
                        case Yarn.Operand.ValueOneofCase.BoolValue: {
                            initialValues[pair.Key] = value.BoolValue;
                            break;
                        }
                        case Yarn.Operand.ValueOneofCase.FloatValue: {
                            initialValues[pair.Key] = value.FloatValue;
                            break;
                        }
                        default: {
                            GD.PushWarning($"{pair.Key} is of an invalid type: {value.ValueCase}");
                            break;
                        }
                    }
                }
                return initialValues;
            }
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the program stored in <see
        /// cref="compiledYarnProgram"/> is deserialized and cached. Future
        /// calls to this method will return the cached value.
        /// </remarks>
        public Program Program {
            get {
                cachedProgram ??= Program.Parser.ParseFrom(compiledYarnProgram);
                return cachedProgram;
            }
        }

        /// <summary>
        /// Gets the headers for the requested node.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the values are extracted from
        /// <see cref="Program"/> and cached inside <see cref="nodeHeaders"/>.
        /// Future calls will then return the cached values.
        /// </remarks>
        /// <param name="nodeName">The name of the node to get the headers.</param>
        /// <returns>The headers for the requested node.</returns>
        public Dictionary<string, Array<string>> GetHeaders(string nodeName) {
            // if the headers have already been extracted just return that
            if (this.nodeHeaders.TryGetValue(nodeName, out Dictionary<string, Array<string>> existingValues)) {
                return existingValues;
            }

            // headers haven't been extracted so we look inside the program
            if (Program.Nodes.TryGetValue(nodeName, out Yarn.Node rawNode) == false) {
                return new Dictionary<string, Array<string>>();
            }

            var rawHeaders = rawNode.Headers;

            // this should NEVER happen
            // because there will always be at least the title, right?
            if (rawHeaders == null || rawHeaders.Count == 0) {
                return new Dictionary<string, Array<string>>();
            }

            // ok so this is an array of (string, string) tuples
            // with potentially duplicated keys inside the array
            // we'll convert it all into a dict of string arrays
            Dictionary<string, Array<string>> headers = new ();
            foreach (var pair in rawHeaders) {
                if (headers.TryGetValue(pair.Key, out Array<string> values)) {
                    values.Add(pair.Value);
                }
                else {
                    values = new () {
                        pair.Value,
                    };
                }
                headers[pair.Key] = values;
            }

            // this.nodeHeaders[nodeName] = headers;

            return headers;
        }

        /// <summary>
        /// Gets the localized string for the given locale code.
        /// </summary>
        /// <param name="localeCode">The locale code.</param>
        /// <returns>The <see cref="Localization" /> for the given locale code.</returns>
        public Localization GetLocalization(string localeCode) {
            // If localeCode is null, we use the base localization.
            if (localeCode == null) {
                return baseLocalization;
            }

            foreach (var loc in localizations) {
                if (loc.LocaleCode == localeCode) {
                    return loc;
                }
            }

            // We didn't find a localization. Fall back to the Base
            // localization.
            return baseLocalization;
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        /// <returns>The Yarn Program stored in this project.</returns>
        [System.Obsolete("Use the Program property instead, which caches its return value.")]
        public Program GetProgram() {
            return Program.Parser.ParseFrom(compiledYarnProgram);
        }
    }
}