using Godot;
using Godot.Collections;
using System.Reflection;
using System.Linq;

using FileAccess = Godot.FileAccess;

namespace Yarn.GodotYarn {
    [Tool/*, GlobalClass, Icon("res://addons/YarnSpinner-Godot/Editor/Icons/YarnProject Icon.svg")*/]
    public partial class YarnProject : Resource {
        [Export] public byte[] compiledYarnProgram;
        [Export] public Localization baseLocalization;

        [Export] public Array<Localization> localizations = new Array<Localization>();
        [Export] public LineMetadata lineMetadata;
        [Export] public LocalizationType localizationType;

        [Export] public Declaration[] declarations = new Declaration[0];


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
        [Export] public string[] searchAssembliesForActions = new string[0];

        /// <summary>
        /// The names of all nodes contained within the <see cref="Program"/>.
        /// </summary>
        public string[] NodeNames {
            get {
                return Program.Nodes.Keys.ToArray();
            }
        }


        private string[] sourceScripts = null;

        // private string defaultLanguage = null;

        public YarnProject() {
            baseLocalization = new Localization();
        }

        [Export(PropertyHint.File, "*.yarn")]
        public string[] SourceScripts {
            set {
                sourceScripts = value;

                #if TOOLS

                var result = YarnProjectEditorCompiler.CompileYarnProject(this);

                GD.Print("Compilation result: ", result);

                #endif
            }
            get { return sourceScripts; }
        }

        /// <summary>
        /// The cached result of reading the default values from the <see
        /// cref="Program"/>.
        /// </summary>
        private System.Collections.Generic.Dictionary<string, System.IConvertible> initialValues;

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

        // ok assumption is that this can be lazy loaded and then kept around
        // as not every node has headers you care about but some will and be read A LOT
        // so we will fill a dict on request and just keep it around
        // is somewhat unnecessary as people can get this out themselves if they want
        // but I think peeps will wanna use headers like a dictionary
        // so we will do the transformation for you
        private Dictionary<string, Dictionary<string, Array<string>>>nodeHeaders = new Dictionary<string, Dictionary<string, Array<string>>>();

        /// <summary>
        /// Gets the headers for the requested node.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the values are extracted from
        /// <see cref="Program"/> and cached inside <see cref="nodeHeaders"/>.
        /// Future calls will then return the cached values.
        /// </remarks>
        public Dictionary<string, Array<string>> GetHeaders(string nodeName) {
            // if the headers have already been extracted just return that
            Dictionary<string, Array<string>> existingValues;

            if (this.nodeHeaders.TryGetValue(nodeName, out existingValues)) {
                return existingValues;
            }

            // headers haven't been extracted so we look inside the program
            Node rawNode;
            if (Program.Nodes.TryGetValue(nodeName, out rawNode) == false) {
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
            Dictionary<string, Array<string>> headers = new Dictionary<string, Array<string>>();
            foreach (var pair in rawHeaders) {
                Array<string> values;

                if (headers.TryGetValue(pair.Key, out values)) {
                    values.Add(pair.Value);
                }
                else {
                    values = new Array<string>();
                    values.Add(pair.Value);
                }
                headers[pair.Key] = values;
            }

            // this.nodeHeaders[nodeName] = headers;

            return headers;
        }

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
        [System.Obsolete("Use the Program property instead, which caches its return value.")]
        public Program GetProgram() {
            return Program.Parser.ParseFrom(compiledYarnProgram);
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
                if(cachedProgram == null) {
                    cachedProgram = Program.Parser.ParseFrom(compiledYarnProgram);
                }
                return cachedProgram;
            }
        }
    }

    public enum LocalizationType {
        YarnInternal, Godot
    }
}