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
namespace Yarn.GodotYarn {
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Godot;
    using Yarn.Compiler;

    using FileAccess = Godot.FileAccess;

    /// <summary>
    /// This class is responsible for the Yarn project editor compilation.
    /// </summary>
    public static class YarnProjectEditorCompiler {
        /// <summary>
        /// Compiles the yarn project.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns>The error code.</returns>
        public static Error CompileYarnProject(YarnProject project) {
            if (project.SourceScripts == null || project.SourceScripts.Length == 0 || project == null || project.baseLocalization == null) {
                return Error.InvalidData;
            }

            string content = string.Empty;
            foreach (var s in project.SourceScripts) {
                if (s == null) continue;

                if (FileAccess.FileExists(s) == false) {
                    continue;
                }

                using FileAccess access = FileAccess.Open(s, FileAccess.ModeFlags.Read);
                content += access.GetAsText();
            }

            if (string.IsNullOrEmpty(content)) {
                return Error.CompilationFailed;
            }

            // GD.Print(content);

            var job = CompilationJob.CreateFromString(project.ResourcePath, content);

            if (project.declarations != null && project.declarations.Length > 0) {
                var finalDec = new List<Yarn.Compiler.Declaration>();
                Yarn.Compiler.Declaration v = new ();
                foreach (var dec in project.declarations) {
                    switch (dec.Type) {
                        case DeclarationType.STRING:
                            v = Yarn.Compiler.Declaration.CreateVariable(dec.Name, Yarn.BuiltinTypes.String, dec.DefaultValue);
                            break;
                        case DeclarationType.BOOLEAN:
                            v = Yarn.Compiler.Declaration.CreateVariable(dec.Name, Yarn.BuiltinTypes.Boolean, bool.Parse(dec.DefaultValue));
                            break;
                        case DeclarationType.NUMBER:
                            v = Yarn.Compiler.Declaration.CreateVariable(dec.Name, Yarn.BuiltinTypes.Number, float.Parse(dec.DefaultValue));
                            break;
                    }

                    finalDec.Add(v);
                }

                job.VariableDeclarations = finalDec;
            }

            CompilationResult compilationResult = Compiler.Compile(job);
            var errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Any()) {
                foreach (var error in errors) {
                    GD.PrintErr(error);
                }
                return Error.CompilationFailed;
            }

            if (compilationResult.Program == null) {
                GD.PrintErr("Internal error: Failed to compile: resulting program was null, but compiler did not report errors.");
                return Error.CompilationFailed;
            }

            using (var memoryStream = new MemoryStream())
            using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream)) {
                // Serialize the compiled program to memory
                compilationResult.Program.WriteTo(outputStream);
                outputStream.Flush();

                project.compiledYarnProgram = memoryStream.ToArray();
            }

            if (project.localizations.Contains(project.baseLocalization) == false) {
                project.localizations.Add(project.baseLocalization);
            }

            foreach (var lang in project.localizations) {
                if (lang.LocaleCode == string.Empty) {
                    GD.PrintErr($"Not creating a localization for {project.ResourceName} because the language ID wasn't provided. Add the language ID to the localization in the Yarn Project's inspector.");
                    continue;
                }

                GD.Print("Generating string file for language ", lang.LocaleCode);

                IEnumerable<StringTableEntry> stringTable;

                // Where do we get our strings from? If it's the default
                // language, we'll pull it from the scripts. If it's from
                // any other source, we'll pull it from the CSVs.
                if (lang.LocaleCode == project.baseLocalization.LocaleCode) {
                    // We'll use the program-supplied string table.
                    stringTable = compilationResult.StringTable.Select(x => new StringTableEntry {
                        // ID = $"line:-{x.Value.nodeName}-{x.Value.lineNumber}",
                        ID = x.Key,
                        Language = project.baseLocalization.LocaleCode,
                        Text = x.Value.text,
                        File = x.Value.fileName,
                        Node = x.Value.nodeName,
                        LineNumber = x.Value.lineNumber.ToString(),
                        Lock = GetHashString(x.Value.text, 8),
                    });

                    // We don't need to add a default localization.
                    // shouldAddDefaultLocalization = false;
                }
                else {
                    try {
                        if (lang.StringFile == null) {
                            // We can't create this localization because we
                            // don't have any data for it.
    #if TOOLS
                            var temp = compilationResult.StringTable.Select(x => new StringTableEntry {
                                ID = x.Key,
                                Language = project.baseLocalization.LocaleCode,
                                Text = x.Value.text,
                                File = x.Value.fileName,
                                Node = x.Value.nodeName,
                                LineNumber = x.Value.lineNumber.ToString(),
                                Lock = GetHashString(x.Value.text, 8),
                            });

                            string target = string.Empty;
                            using (FileAccess access = FileAccess.Open(project.ResourcePath, FileAccess.ModeFlags.Read)) {
                                target = access.GetPathAbsolute();
                            }
                            GD.Print(target);

                            target = target.Replace(".tres", "(" + lang.LocaleCode + ").csv.tres");

                            GD.Print(target);

                            GD.Print("Generate " + target);
                            using var writer = new StreamWriter(target);
                            writer.WriteLine("language,id,text,file,node,lineNumber,lock,comment");
                            foreach (var item in temp) {
                                writer.WriteLine($"{lang.LocaleCode},{item.ID},{item.Text},{item.File},{item.Node},{item.LineNumber},{item.Lock},");
                            }

                            lang.StringFile = target;
#else
                            GD.PushWarning($"Not creating a localization for {lang.LocaleCode} in the Yarn Project {project.ResourceName} because a text asset containing the strings wasn't found. Add a .csv file containing the translated lines to the Yarn Project's inspector.");
                            continue;
#endif
                        }

                        using (FileAccess access = FileAccess.Open(lang.StringFile, FileAccess.ModeFlags.Read)) {
                            string source = access.GetAsText();
                            stringTable = StringTableEntry.ParseFromCSV(source);
                        }
                    }
                    catch (System.ArgumentException e) {
                        GD.PushWarning($"Not creating a localization for {lang.LocaleCode} in the Yarn Project {project.ResourceName} because an error was encountered during text parsing: {e}");
                        continue;
                    }
                }

                if (lang.LocaleCode == project.baseLocalization.LocaleCode) {
                    // If this is our default language, set it as such
                    project.baseLocalization.Clear();
                    project.baseLocalization.AddLocalizedStringsToAsset(stringTable);
                }
                else {
                    var newLocalization = new Localization();
                    newLocalization.LocaleCode = lang.LocaleCode;
                    newLocalization.AddLocalizedStringsToAsset(stringTable);
                    project.localizations.Add(newLocalization);
                }
            }

            return Error.Ok;
        }

        /// <summary>
        /// Returns a string that represents the hash of the given string.
        /// </summary>
        /// <param name="inputString">The string to hash.</param>
        /// <param name="limitCharacters">The number of characters to return. If -1, returns the entire string.</param>
        /// <returns>A string representing the hash of the given string.</returns>
        internal static string GetHashString(string inputString, int limitCharacters = -1) {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString)) {
                sb.Append(b.ToString("x2"));
            }

            if (limitCharacters == -1) {
                // Return the entire string
                return sb.ToString();
            }
            else {
                // Return a substring (or the entire string, if
                // limitCharacters is longer than the string)
                return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
            }
        }

        private static byte[] GetHash(string inputString) {
            using HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
    }
}
#endif