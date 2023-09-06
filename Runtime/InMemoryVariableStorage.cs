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
using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Yarn.GodotYarn {
    public partial class InMemoryVariableStorage : VariableStorageBehaviour, IEnumerable<KeyValuePair<string, object>> {
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private Dictionary<string, System.Type> variableTypes = new Dictionary<string, System.Type>(); // needed for serialization

        public bool showDebug;
        [Export]
        private string password = "";

        internal void Update() {
            if (showDebug) {
                var text = GetDebugList();
                GD.Print(text);
            }
        }

        public string GetDebugList() {
            var stringBuilder = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, object> item in variables) {
                stringBuilder.AppendLine(string.Format("{0} = {1} ({2})",
                                                        item.Key,
                                                        item.Value.ToString(),
                                                        variableTypes[item.Key].ToString().Substring("System.".Length)));
            }
            return stringBuilder.ToString();
        }

        #region Setters

        void SetVariable(string name, Yarn.IType type, string value) {
            if (type == Yarn.BuiltinTypes.Boolean) {
                bool newBool;
                if (bool.TryParse(value, out newBool)) {
                    SetValue(name, newBool);
                }
                else {
                    throw new System.InvalidCastException($"Couldn't initialize default variable {name} with value {value} as Bool");
                }
            }
            else if (type == Yarn.BuiltinTypes.Number) {
                float newNumber;
                if (float.TryParse(value, out newNumber)) {
                    SetValue(name, newNumber);
                }
                else {
                    throw new System.InvalidCastException($"Couldn't initialize default variable {name} with value {value} as Number (Float)");
                }
            }
            else if (type == Yarn.BuiltinTypes.String) {
                SetValue(name, value);
            }
            else {
                throw new System.ArgumentOutOfRangeException($"Unsupported type {type.Name}");
            }
        }
        private void ValidateVariableName(string variableName) {
            if (variableName.StartsWith("$") == false) {
                throw new System.ArgumentException($"{variableName} is not a valid variable name: Variable names must start with a '$'. (Did you mean to use '${variableName}'?)");
            }
        }

        public override void SetValue(string variableName, string stringValue) {
            ValidateVariableName(variableName);

            variables[variableName] = stringValue;
            variableTypes[variableName] = typeof(string);
        }

        public override void SetValue(string variableName, float floatValue) {
            ValidateVariableName(variableName);

            variables[variableName] = floatValue;
            variableTypes[variableName] = typeof(float);
        }

        public override void SetValue(string variableName, bool boolValue) {
            ValidateVariableName(variableName);

            variables[variableName] = boolValue;
            variableTypes[variableName] = typeof(bool);
        }

        public override bool TryGetValue<T>(string variableName, out T result) {
            ValidateVariableName(variableName);

            // If we don't have a variable with this name, return the null
            // value
            if (variables.ContainsKey(variableName) == false) {
                result = default;
                return false;
            }

            var resultObject = variables[variableName];

            if (typeof(T).IsAssignableFrom(resultObject.GetType())) {
                result = (T)resultObject;
                return true;
            }
            else {
                throw new System.InvalidCastException($"Variable {variableName} exists, but is the wrong type (expected {typeof(T)}, got {resultObject.GetType()}");
            }
        }

        public override void Clear() {
            variables.Clear();
            variableTypes.Clear();
        }

        #endregion

        public override bool Contains(string variableName) {
            return variables.ContainsKey(variableName);
        }

        #region Save/Load

        [System.Serializable] class StringDictionary : Godot.Collections.Dictionary<string, string> { } // serializable dictionary workaround

        static string[] SEPARATOR = new string[] { "/" }; // used for serialization

        public string SerializeAllVariablesToJSON(bool prettyPrint = false) {
            // "objects" aren't serializable by JsonUtility...
            var serializableVariables = new StringDictionary();
            foreach (var variable in variables) {
                var jsonType = variableTypes[variable.Key];
                var jsonKey = $"{jsonType}{SEPARATOR[0]}{variable.Key}"; // ... so we have to encode the System.Object type into the JSON key
                var jsonValue = System.Convert.ChangeType(variable.Value, jsonType);
                serializableVariables.Add(jsonKey, jsonValue.ToString());
            }
            //var saveData = JsonUtility.ToJson(serializableVariables, prettyPrint);
            var saveData = Json.Stringify(serializableVariables);
            // Debug.Log(saveData);
            return saveData;
        }

        public void DeserializeAllVariablesFromJSON(string jsonData) {
            // Debug.Log(jsonData);
            //var serializedVariables = JsonUtility.FromJson<StringDictionary>(jsonData);
            var serializedVariables = Json.ParseString(jsonData).AsGodotDictionary<string, string>();
            foreach (var variable in serializedVariables) {
                var serializedKey = variable.Key.Split(SEPARATOR, 2, System.StringSplitOptions.None);
                var jsonType = System.Type.GetType(serializedKey[0]);
                var jsonKey = serializedKey[1];
                var jsonValue = variable.Value;
                SetVariable(jsonKey, TypeMappings[jsonType], jsonValue);
            }
        }

        const string DEFAULT_PLAYER_PREFS_KEY = "DefaultYarnVariableStorage";

        public void SaveToPlayerPrefs() {
            SaveToPlayerPrefs(DEFAULT_PLAYER_PREFS_KEY);
        }

        public void SaveToPlayerPrefs(string playerPrefsKey) {
            var saveData = SerializeAllVariablesToJSON();
            var config = new ConfigFile();
            config.SetValue("yarn", playerPrefsKey, saveData);
            if (string.IsNullOrEmpty(password)) {
                config.Save("user://yarn.cfg");
            }
            else {
                config.SaveEncrypted("user://yarn.cfg", Encoding.ASCII.GetBytes(password));
            }
            GD.Print($"Variables saved to PlayerPrefs with key {playerPrefsKey}");
        }

        public void SaveToFile(string filepath) {
            var saveData = SerializeAllVariablesToJSON();
            System.IO.File.WriteAllText(filepath, saveData, System.Text.Encoding.UTF8);
            GD.Print($"Variables saved to file {filepath}");
        }

        public void LoadFromPlayerPrefs() {
            LoadFromPlayerPrefs(DEFAULT_PLAYER_PREFS_KEY);
        }

        public void LoadFromPlayerPrefs(string playerPrefsKey) {
            var config = new ConfigFile();
            Error err = Error.Ok;
            if (string.IsNullOrEmpty(password)) {
                err = config.Load("user://yarn.cfg");
            }
            else {
                err = config.LoadEncrypted("user://yarn.cfg", Encoding.ASCII.GetBytes(password));
            }

            if (err == Error.Ok && config.HasSectionKey("yarn", playerPrefsKey)) {
                var saveData = config.GetValue("yarn", playerPrefsKey).AsString();
                DeserializeAllVariablesFromJSON(saveData);
                GD.Print($"Variables loaded from PlayerPrefs under key {playerPrefsKey}");
            }
            else {
                GD.PrintErr($"No PlayerPrefs key {playerPrefsKey} found, so no variables loaded.");
            }
        }

        public void LoadFromFile(string filepath) {
            var saveData = System.IO.File.ReadAllText(filepath, System.Text.Encoding.UTF8);
            DeserializeAllVariablesFromJSON(saveData);
            GD.Print($"Variables loaded from file {filepath}");
        }

        public static readonly Dictionary<System.Type, Yarn.IType> TypeMappings = new Dictionary<System.Type, Yarn.IType>
            {
                    { typeof(string), Yarn.BuiltinTypes.String },
                    { typeof(bool), Yarn.BuiltinTypes.Boolean },
                    { typeof(int), Yarn.BuiltinTypes.Number },
                    { typeof(float), Yarn.BuiltinTypes.Number },
                    { typeof(double), Yarn.BuiltinTypes.Number },
                    { typeof(sbyte), Yarn.BuiltinTypes.Number },
                    { typeof(byte), Yarn.BuiltinTypes.Number },
                    { typeof(short), Yarn.BuiltinTypes.Number },
                    { typeof(ushort), Yarn.BuiltinTypes.Number },
                    { typeof(uint), Yarn.BuiltinTypes.Number },
                    { typeof(long), Yarn.BuiltinTypes.Number },
                    { typeof(ulong), Yarn.BuiltinTypes.Number },
                    { typeof(decimal), Yarn.BuiltinTypes.Number },
                };

        #endregion

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
            return ((IEnumerable<KeyValuePair<string, object>>)variables).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<KeyValuePair<string, object>>)variables).GetEnumerator();
        }
    }
}