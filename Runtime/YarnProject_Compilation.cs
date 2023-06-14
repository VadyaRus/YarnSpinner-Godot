/*
using Godot;
using System.Collections.Generic;
using Yarn.Compiler;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;

using FileAccess = Godot.FileAccess;

namespace Yarn.GodotYarn {
    public partial class YarnProject : Resource {

        private static byte[] GetHash(string inputString) {
            using (HashAlgorithm algorithm = SHA256.Create()) {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

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

        private void Compile() {
            if (sourceScripts == null || sourceScripts.Length == 0 || languages == null || defaultLanguage == null) {
                return;
            }

            string content = "";
            foreach (var s in sourceScripts) {
                if(s == null) continue;

                using(FileAccess access = FileAccess.Open(s.ResourcePath, FileAccess.ModeFlags.Read)) {
                    content += access.GetAsText();
                }
            }

            if(string.IsNullOrEmpty(content)) {
                return;
            }

            // GD.Print(content);

            var job = CompilationJob.CreateFromString(sourceScripts[0].ResourcePath, content);
            if (declarations != null && declarations.Length > 0) {
                var finalDec = new List<Yarn.Compiler.Declaration>();
                Yarn.Compiler.Declaration v = new Yarn.Compiler.Declaration();
                foreach (var dec in declarations) {
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

            CompilationResult compilationResult = Compiler.Compiler.Compile(job);
            var errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0) {
                foreach (var error in errors) {
                    GD.PrintErr(error);
                }
                return;
            }

            if (compilationResult.Program == null) {
                GD.PrintErr("Internal error: Failed to compile: resulting program was null, but compiler did not report errors.");
                return;
            }

            using (var memoryStream = new MemoryStream())
            using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream)) {
                // Serialize the compiled program to memory
                compilationResult.Program.WriteTo(outputStream);
                outputStream.Flush();

                compiledYarnProgram = memoryStream.ToArray();
            }

            foreach (var lang in languages) {
                if (lang.LanguageID == string.Empty) {
                    GD.PrintErr($"Not creating a localization for {projectName} because the language ID wasn't provided. Add the language ID to the localization in the Yarn Project's inspector.");
                    continue;
                }

                GD.Print("Generating string file for language ", lang.LanguageID);

                IEnumerable<StringTableEntry> stringTable;

                // Where do we get our strings from? If it's the default
                // language, we'll pull it from the scripts. If it's from
                // any other source, we'll pull it from the CSVs.
                if (lang.LanguageID == defaultLanguage) {
                    // We'll use the program-supplied string table.
                    stringTable = compilationResult.StringTable.Select(x => new StringTableEntry {
                        ID = x.Key,
                        Language = defaultLanguage,
                        Text = x.Value.text,
                        File = x.Value.fileName,
                        Node = x.Value.nodeName,
                        LineNumber = x.Value.lineNumber.ToString(),
                        Lock = GetHashString(x.Value.text, 8),
                    });

                    // We don't need to add a default localization.
                    //shouldAddDefaultLocalization = false;
                }
                else {
                    try {
                        if (lang.StringFile == null) {
                            // We can't create this localization because we
                            // don't have any data for it.
    #if TOOLS
                            var temp = compilationResult.StringTable.Select(x => new StringTableEntry {
                                ID = x.Key,
                                Language = defaultLanguage,
                                Text = x.Value.text,
                                File = x.Value.fileName,
                                Node = x.Value.nodeName,
                                LineNumber = x.Value.lineNumber.ToString(),
                                Lock = GetHashString(x.Value.text, 8),
                            });

                            string target = string.Empty;
                            using(FileAccess access = FileAccess.Open(sourceScripts[0].ResourcePath, FileAccess.ModeFlags.Read)) {
                                target = access.GetPathAbsolute();
                            }
                            GD.Print(target);

                            target = target.Replace(".yarn", "(" + lang.LanguageID + ").csv.tres");

                            GD.Print(target);

                            GD.Print("Generate " + target);
                            using (var writer = new StreamWriter(target)) {
                                writer.WriteLine("language,id,text,file,node,lineNumber,lock,comment");
                                foreach (var item in temp) {
                                    writer.WriteLine($"{lang.LanguageID},{item.ID},{item.Text},{item.File},{item.Node},{item.LineNumber},{item.Lock},");
                                }

                                lang.StringFile = target;
                            }
    #else
                            GD.PushWarning($"Not creating a localization for {lang.LanguageID} in the Yarn Project {projectName} because a text asset containing the strings wasn't found. Add a .csv file containing the translated lines to the Yarn Project's inspector.");
                            continue;
    #endif
                        }

                        using(FileAccess access = FileAccess.Open(lang.StringFile, FileAccess.ModeFlags.Read)) {
                            string source = access.GetAsText();
                            stringTable = StringTableEntry.ParseFromCSV(source);
                        }
                    }
                    catch (System.ArgumentException e) {
                        GD.PushWarning($"Not creating a localization for {lang.LanguageID} in the Yarn Project {projectName} because an error was encountered during text parsing: {e}");
                        continue;
                    }
                }

                var newLocalization = new Localization();
                newLocalization.LocaleCode = lang.LanguageID;
                newLocalization.AddLocalizedStrings(stringTable);
                localizations.Add(newLocalization);

                if (lang.LanguageID == defaultLanguage) {
                    // If this is our default language, set it as such
                    baseLocalization = newLocalization;
                }
            }
        }
    }
}
*/