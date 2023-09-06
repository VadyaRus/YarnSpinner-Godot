using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Yarn.GodotYarn {
    public partial class DialogueRunner : Control {
        /// <summary>
        /// The <see cref="YarnProject"/> asset that should be loaded on
        /// scene start.
        /// </summary>
        [Export]
        public YarnProject yarnProject;

        /// <summary>
        /// The variable storage object.
        /// </summary>
        [Export] VariableStorageBehaviour _variableStorage;

        /// <inheritdoc cref="_variableStorage"/>
        public VariableStorageBehaviour VariableStorage {
            get => _variableStorage;
            set {
                _variableStorage = value;
                if (_dialogue != null) {
                    _dialogue.VariableStorage = value;
                }
            }
        }

        [Export] private NodePath[] views = new NodePath[0];

        /// <summary>
        /// The View classes that will present the dialogue to the user.
        /// </summary>
        public DialogueViewBase[] dialogueViews = new DialogueViewBase[0];

        /// <summary>The name of the node to start from.</summary>
        /// <remarks>
        /// This value is used to select a node to start from when <see
        /// cref="startAutomatically"/> is called.
        /// </remarks>
        [Export] public string startNode = Yarn.Dialogue.DefaultStartNodeName;

        /// <summary>
        /// Whether the DialogueRunner should automatically start running
        /// dialogue after the scene loads.
        /// </summary>
        /// <remarks>
        /// The node specified by <see cref="startNode"/> will be used.
        /// </remarks>
        [Export] public bool startAutomatically = true;

        /// <summary>
        /// If true, when an option is selected, it's as though it were a
        /// line.
        /// </summary>
        public bool runSelectedOptionAsLine;

        [Export] public LineProviderBehaviour lineProvider;

        /// <summary>
        /// If true, will print <see cref="GD.Print"/> messages every time it enters a
        /// node, and other frequent events.
        /// </summary>
        [Export] public bool verboseLogging = true;

        /// <summary>
        /// Gets a value that indicates if the dialogue is actively
        /// running.
        /// </summary>
        public bool IsDialogueRunning { get; set; }

        /// <summary>
        /// A Godot event that is called when a node starts running.
        /// </summary>
        /// <remarks>
        /// This event receives as a parameter the name of the node that is
        /// about to start running.
        /// </remarks>
        /// <seealso cref="Dialogue.NodeStartHandler"/>
        [Signal]
        public delegate void onNodeStartEventHandler(string str);

        /// <summary>
        /// A Godot event that is called when a node is complete.
        /// </summary>
        /// <remarks>
        /// This event receives as a parameter the name of the node that
        /// just finished running.
        /// </remarks>
        /// <seealso cref="Dialogue.NodeCompleteHandler"/>
        [Signal]
        public delegate void onNodeCompleteEventHandler(string str);

        /// <summary>
        /// A Godot event that is called when the dialogue starts running.
        /// </summary>
        [Signal]
        public delegate void onDialogueStartEventHandler();

        /// <summary>
        /// A Godot event that is called once the dialogue has completed.
        /// </summary>
        /// <seealso cref="Dialogue.DialogueCompleteHandler"/>
        [Signal]
        public delegate void onDialogueCompleteEventHandler();

        /// <summary>
        /// A <see cref="Signal"/> that is called when a <see
        /// cref="Command"/> is received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method to dispatch a command to other parts of your game.
        /// This method is only called if the <see cref="Command"/> has not been
        /// handled by a command handler that has been added to the <see
        /// cref="DialogueRunner"/>, or by a method on a <see
        /// cref="Godot.Node"/> in the scene with the attribute <see
        /// cref="YarnCommandAttribute"/>.
        /// </para>
        /// <para style="hint">
        /// When a command is delivered in this way, the <see
        /// cref="DialogueRunner"/> will not pause execution. If you want a
        /// command to make the DialogueRunner pause execution, see <see
        /// cref="AddCommandHandler(string, Delegate)"/>.
        /// </para>
        /// <para>
        /// This method receives the full text of the command, as it appears
        /// between the <c>&lt;&lt;</c> and <c>&gt;&gt;</c> markers.
        /// </para>
        /// </remarks>
        /// <seealso cref="AddCommandHandler(string, Delegate)"/>
        /// <seealso cref="AddCommandHandler(string, Delegate)"/>
        /// <seealso cref="YarnCommandAttribute"/>
        [Signal]
        public delegate void onCommandEventHandler(string str);

        /// <summary>
        /// Gets the name of the current node that is being run.
        /// </summary>
        /// <seealso cref="Dialogue.currentNode"/>
        public string CurrentNodeName => Dialogue.CurrentNode;

        /// <summary>
        /// Gets the underlying <see cref="Dialogue"/> object that runs the
        /// Yarn code.
        /// </summary>
        public Yarn.Dialogue Dialogue => _dialogue ?? (_dialogue = CreateDialogueInstance());

        /// <summary>
        /// A flag used to detect if an options handler attempts to set the
        /// selected option on the same frame that options were provided.
        /// </summary>
        /// <remarks>
        /// This field is set to false by <see
        /// cref="HandleOptions(OptionSet)"/> immediately before calling
        /// <see cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> on all objects in <see cref="dialogueViews"/>,
        /// and set to true immediately after. If a call to <see
        /// cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> calls its completion hander on the same frame,
        /// an error is generated.
        /// </remarks>
        private bool IsOptionSelectionAllowed = false;

        private List<IEnumerator> coroutines = new List<IEnumerator>();

        /// <summary>
        /// Replaces this DialogueRunner's yarn project with the provided
        /// project.
        /// </summary>
        public void SetProject(YarnProject newProject) {
            // Load all of the commands and functions from the assemblies that
            // this project wants to load from.
            ActionManager.AddActionsFromAssemblies(newProject.searchAssembliesForActions);

            // Register any new functions that we found as part of doing this.
            ActionManager.RegisterFunctions(Dialogue.Library);

            Dialogue.SetProgram(newProject.Program);

            if (lineProvider != null) {
                lineProvider.YarnProject = newProject;
            }
        }

        /// <summary>
        /// Loads any initial variables declared in the program and loads that variable with its default declaration value into the variable storage.
        /// Any variable that is already in the storage will be skipped, the assumption is that this means the value has been overridden at some point and shouldn't be otherwise touched.
        /// Can force an override of the existing values with the default if that is desired.
        /// </summary>
        public void SetInitialVariables(bool overrideExistingValues = false) {
            if (yarnProject == null) {
                GD.PrintErr("Unable to set default values, there is no project set");
                return;
            }

            // grabbing all the initial values from the program and inserting them into the storage
            // we first need to make sure that the value isn't already set in the storage
            var values = yarnProject.Program.InitialValues;
            foreach (var pair in values) {
                if (overrideExistingValues == false && VariableStorage.Contains(pair.Key)) {
                    continue;
                }
                var value = pair.Value;
                switch (value.ValueCase) {
                    case Yarn.Operand.ValueOneofCase.StringValue: {
                            VariableStorage.SetValue(pair.Key, value.StringValue);
                            break;
                        }
                    case Yarn.Operand.ValueOneofCase.BoolValue: {
                            VariableStorage.SetValue(pair.Key, value.BoolValue);
                            break;
                        }
                    case Yarn.Operand.ValueOneofCase.FloatValue: {
                            VariableStorage.SetValue(pair.Key, value.FloatValue);
                            break;
                        }
                    default: {
                            GD.PrintErr($"{pair.Key} is of an invalid type: {value.ValueCase}");
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Start the dialogue from a specific node.
        /// </summary>
        /// <param name="startNode">The name of the node to start running
        /// from.</param>
        public void StartDialogue(string startNode) {
            // If the dialogue is currently executing instructions, then
            // calling ContinueDialogue() at the end of this method will
            // cause confusing results. Report an error and stop here.
            if (Dialogue.IsActive) {
                GD.PushError($"Can't start dialogue from node {startNode}: the dialogue is currently in the middle of running. Stop the dialogue first.");
                return;
            }

            // Stop any processes that might be running already
            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false) {
                    continue;
                }

                // dialogueView.StopAllCoroutines();
            }

            // Get it going

            // Mark that we're in conversation.
            IsDialogueRunning = true;

            EmitSignal("onDialogueStart");

            // Signal that we're starting up.
            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false) continue;

                dialogueView.DialogueStarted();
            }

            // Request that the dialogue select the current node. This
            // will prepare the dialogue for running; as a side effect,
            // our prepareForLines delegate may be called.
            Dialogue.SetNode(startNode);

            if (lineProvider.LinesAvailable == false) {
                // The line provider isn't ready to give us our lines
                // yet. We need to start a coroutine that waits for
                // them to finish loading, and then runs the dialogue.
                ContinueDialogueWhenLinesAvailable();
            }
            else {
                ContinueDialogue();
            }
        }

        private async void ContinueDialogueWhenLinesAvailable() {
            // Wait until lineProvider.LinesAvailable becomes true
            while(lineProvider.LinesAvailable == false) {
                await ToSignal(GetTree(), "process_frame");
            }

            // And then run our dialogue.
            ContinueDialogue();
        }

        /// <summary>
        /// Starts running the dialogue again.
        /// </summary>
        /// <remarks>
        /// If <paramref name="nodeName"/> is null, the node specified by
        /// <see cref="startNode"/> is attempted, followed the currently
        /// running node. If none of these options are available, an <see
        /// cref="ArgumentNullException"/> is thrown.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when a node to
        /// restart the dialogue from cannot be found.</exception>
        [Obsolete("Use " + nameof(StartDialogue) + "(nodeName) instead.")]
        public void ResetDialogue(string nodeName = null) {
            nodeName = nodeName ?? startNode ?? CurrentNodeName ?? throw new ArgumentNullException($"Cannot reset dialogue: couldn't figure out a node to restart the dialogue from.");

            StartDialogue(nodeName);
        }

        /// <summary>
        /// Unloads all nodes from the <see cref="Dialogue"/>.
        /// </summary>
        public void Clear() {
            System.Diagnostics.Debug.Assert(IsDialogueRunning == false, "You cannot clear the dialogue system while a dialogue is running.");
            Dialogue.UnloadAll();
        }

        /// <summary>
        /// Stops the <see cref="Dialogue"/>.
        /// </summary>
        public void Stop() {
            IsDialogueRunning = false;
            Dialogue.Stop();
        }

        /// <summary>
        /// Returns `true` when a node named `nodeName` has been loaded.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns>`true` if the node is loaded, `false`
        /// otherwise/</returns>
        public bool NodeExists(string nodeName) => Dialogue.NodeExists(nodeName);

        /// <summary>
        /// Returns the collection of tags that the node associated with
        /// the node named `nodeName`.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns>The collection of tags associated with the node, or
        /// `null` if no node with that name exists.</returns>
        public IEnumerable<string> GetTagsForNode(String nodeName) => Dialogue.GetTagsForNode(nodeName);


        /// <summary>
        /// Sets the dialogue views and makes sure the callback <see cref="DialogueViewBase.MarkLineComplete"/>
        /// will respond correctly.
        /// </summary>
        /// <param name="views">The array of views to be assigned.</param>
        public void SetDialogueViews(DialogueViewBase[] views) {
            foreach (var view in views) {
                if (view == null) {
                    // GD.PrintErr("The 'Dialogue Views' field contains a NULL element.");
                    continue;
                }

                view.requestInterrupt = OnViewRequestedInterrupt;
            }

            dialogueViews = views;
        }

        #region Private Properties/Variables/Procedures

        /// <summary>
        /// The <see cref="LocalizedLine"/> currently being displayed on
        /// the dialogue views.
        /// </summary>
        internal LocalizedLine CurrentLine { get; private set; }

        /// <summary>
        ///  The collection of dialogue views that are currently either
        ///  delivering a line, or dismissing a line from being on screen.
        /// </summary>
        private readonly HashSet<DialogueViewBase> ActiveDialogueViews = new HashSet<DialogueViewBase>();

        Action<int> selectAction;

        /// Maps the names of commands to action delegates.
        Dictionary<string, Delegate> commandHandlers = new Dictionary<string, Delegate>();

        /// <summary>
        /// The underlying object that executes Yarn instructions
        /// and provides lines, options and commands.
        /// </summary>
        /// <remarks>
        /// Automatically created on first access.
        /// </remarks>
        private Yarn.Dialogue _dialogue;

        /// <summary>
        /// The current set of options that we're presenting.
        /// </summary>
        /// <remarks>
        /// This value is <see langword="null"/> when the <see
        /// cref="DialogueRunner"/> is not currently presenting options.
        /// </remarks>
        private Yarn.OptionSet currentOptions;

        public override void _Ready() {
            if (lineProvider == null) {
                // If we don't have a line provider, create a
                // TextLineProvider and make it use that.

                // Create the temporary line provider and the line database
                lineProvider = new TextLineProvider();
                AddChild(lineProvider);

                // Let the user know what we're doing.
                if (verboseLogging) {
                    GD.Print($"Dialogue Runner has no LineProvider; creating a {nameof(TextLineProvider)}.", this);
                }
            }

            dialogueViews = new DialogueViewBase[views.Length];
            for (int i = 0; i < views.Length; ++i) {
                dialogueViews[i] = GetNode<DialogueViewBase>(views[i]);
            }

            if (dialogueViews.Length == 0) {
                GD.PrintErr($"Dialogue Runner doesn't have any dialogue views set up. No lines or options will be visible.");
                return;
            }

            foreach (var dialogueView in dialogueViews) {
                // Skip any null or disabled dialogue views
                if (dialogueView == null || dialogueView.Visible == false) {
                    continue;
                }

                dialogueView.requestInterrupt = OnViewRequestedInterrupt;
            }

            if (yarnProject != null) {
                if (Dialogue.IsActive) {
                    GD.PrintErr($"DialogueRunner wanted to load a Yarn Project in its Start method, but the Dialogue was already running one. The Dialogue Runner may not behave as you expect.");
                }

                // Load this new Yarn Project.
                SetProject(yarnProject);

                if (startAutomatically) {
                    StartDialogue(startNode);
                }
            }
        }

        Yarn.Dialogue CreateDialogueInstance() {
            if (VariableStorage == null) {
                // If we don't have a variable storage, create an
                // InMemoryVariableStorage and make it use that.

                VariableStorage = new InMemoryVariableStorage();
                AddChild(VariableStorage);

                // Let the user know what we're doing.
                if (verboseLogging) {
                    GD.Print($"Dialogue Runner has no Variable Storage; creating a {nameof(InMemoryVariableStorage)}", this);
                }
            }

            // Create the main Dialogue runner, and pass our
            // variableStorage to it
            var dialogue = new Yarn.Dialogue(VariableStorage) {
                // Set up the logging system.
                LogDebugMessage = delegate (string message) {
                    if (verboseLogging) {
                        GD.Print(message);
                    }
                },
                LogErrorMessage = delegate (string message) {
                    GD.PrintErr(message);
                },

                LineHandler = HandleLine,
                CommandHandler = HandleCommand,
                OptionsHandler = HandleOptions,
                NodeStartHandler = (node) => {
                    EmitSignal("onNodeStart", node);
                },
                NodeCompleteHandler = (node) => {
                    EmitSignal("onNodeComplete", node);
                },
                DialogueCompleteHandler = HandleDialogueComplete,
                PrepareForLinesHandler = PrepareForLines
            };

            selectAction = SelectedOption;
            return dialogue;
        }

        void HandleOptions(Yarn.OptionSet options) {
            currentOptions = options;

            DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
            for (int i = 0; i < options.Options.Length; ++i) {
                // Localize the line associated with the option
                var localisedLine = lineProvider.GetLocalizedLine(options.Options[i].Line);
                var text = Yarn.Dialogue.ExpandSubstitutions(localisedLine.RawText, options.Options[i].Line.Substitutions);

                Dialogue.LanguageCode = lineProvider.LocaleCode;

                try {
                    localisedLine.Text = Dialogue.ParseMarkup(text);
                }
                catch(Yarn.Markup.MarkupParseException e) {
                    // Parsing the markup failed. We'll log a warning, and
                    // produce a markup result that just contains the raw text.
                    GD.PushWarning($"Failed to parse markup in \"{text}\": {e.Message}");
                    localisedLine.Text = new Yarn.Markup.MarkupParseResult {
                        Text = text,
                        Attributes = new List<Markup.MarkupAttribute>()
                    };
                }

                optionSet[i] = new DialogueOption {
                    TextID = options.Options[i].Line.ID,
                    DialogueOptionID = options.Options[i].ID,
                    Line = localisedLine,
                    IsAvailable = options.Options[i].IsAvailable,
                };
            }

            // Don't allow selecting options on the same frame that we
            // provide them
            IsOptionSelectionAllowed = false;

            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false) continue;

                dialogueView.RunOptions(optionSet, selectAction);
            }

            IsOptionSelectionAllowed = true;
        }

        void HandleDialogueComplete() {
            IsDialogueRunning = false;
            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false) continue;

                dialogueView.DialogueComplete();
            }

            EmitSignal("onDialogueComplete");
        }

        async void HandleCommand(Yarn.Command command) {
            GD.Print($"[HandleCommand] {command.Text}");

            CommandDispatchResult dispatchResult;

            // Try looking in the command handlers first
            dispatchResult = await DispatchCommandToRegisteredHandlers(command, ContinueDialogue);

            if (dispatchResult != CommandDispatchResult.NotFound) {
                // We found the command! We don't need to keep looking. (It may
                // have succeeded or failed; if it failed, it logged something
                // to the console or otherwise communicated to the developer
                // that something went wrong. Either way, we don't need to do
                // anything more here.)
                return;
            }

            // We didn't find it in the comand handlers. Try looking in the
            // game objects. If it is, continue dialogue.
            dispatchResult = await DispatchCommandToGameObject(command, ContinueDialogue);

            if (dispatchResult != CommandDispatchResult.NotFound) {
                // As before: we found a handler for this command, so we stop
                // looking.
                return;
            }

            // We didn't find a method in our C# code to invoke. Try invoking on
            // the publicly exposed Godot Signal.
            EmitSignal("onCommand", command.Text);

            // Whether we successfully handled it via the Godot Signal or not,
            // attempting to handle the command this way doesn't interrupt the
            // dialogue, so we'll continue it now.
            ContinueDialogue();
        }

        /// <summary>
        /// Forward the line to the dialogue UI.
        /// </summary>
        /// <param name="line">The line to send to the dialogue views.</param>
        private void HandleLine(Yarn.Line line) {
            // Get the localized line from our line provider
            CurrentLine = lineProvider.GetLocalizedLine(line);

            // Expand substitutions
            var text = Yarn.Dialogue.ExpandSubstitutions(CurrentLine.RawText, CurrentLine.Substitutions);

            if (text == null) {
                GD.PushWarning($"Dialogue Runner couldn't expand substitutions in Yarn Project [{ yarnProject.ResourceName }] node [{ CurrentNodeName }] with line ID [{ CurrentLine.TextID }]. "
                    + "This usually happens because it couldn't find text in the Localization. The line may not be tagged properly. "
                    + "Try re-importing this Yarn Program. "
                    + "For now, Dialogue Runner will swap in CurrentLine.RawText.");
                text = CurrentLine.RawText;
            }

            // Render the markup
            Dialogue.LanguageCode = lineProvider.LocaleCode;

            try {
                CurrentLine.Text = Dialogue.ParseMarkup(text);
            }
            catch (Yarn.Markup.MarkupParseException e) {
                // Parsing the markup failed. We'll log a warning, and
                // produce a markup result that just contains the raw text.
                GD.PushWarning($"Failed to parse markup in \"{text}\": {e.Message}");
                CurrentLine.Text = new Yarn.Markup.MarkupParseResult {
                    Text = text,
                    Attributes = new List<Markup.MarkupAttribute>()
                };
            }

            // Clear the set of active dialogue views, just in case
            ActiveDialogueViews.Clear();

            // the following is broken up into two stages because otherwise if the
            // first view happens to finish first once it calls dialogue complete
            // it will empty the set of active views resulting in the line being considered
            // finished by the runner despite there being a bunch of views still waiting
            // so we do it over two loops.
            // the first finds every active view and flags it as such
            // the second then goes through them all and gives them the line

            // Mark this dialogue view as active
            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false) {
                    continue;
                }

                ActiveDialogueViews.Add(dialogueView);
            }
            // Send line to all active dialogue views
            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false) {
                    continue;
                }

                dialogueView.RunLine(CurrentLine,
                    () => DialogueViewCompletedDelivery(dialogueView));
            }
        }

        // called by the runner when a view has signalled that it needs to interrupt the current line
        void InterruptLine() {
            ActiveDialogueViews.Clear();

            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false || dialogueView.CanProcess() == false) {
                    continue;
                }

                ActiveDialogueViews.Add(dialogueView);
            }

            foreach (var dialogueView in dialogueViews) {
                dialogueView.InterruptLine(CurrentLine, ()=> DialogueViewCompletedInterrupt(dialogueView));
            }
        }

        /// <summary>
        /// Indicates to the DialogueRunner that the user has selected an
        /// option
        /// </summary>
        /// <param name="optionIndex">The index of the option that was
        /// selected.</param>
        /// <exception cref="InvalidOperationException">Thrown when the
        /// <see cref="IsOptionSelectionAllowed"/> field is <see
        /// langword="true"/>, which is the case when <see
        /// cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> is in the middle of being called.</exception>
        void SelectedOption(int optionIndex) {
            if (IsOptionSelectionAllowed == false) {
                throw new InvalidOperationException("Selecting an option on the same frame that options are provided is not allowed. Wait at least one frame before selecting an option.");
            }

            // Mark that this is the currently selected option in the
            // Dialogue
            Dialogue.SetSelectedOption(optionIndex);

            if (runSelectedOptionAsLine) {
                foreach (var option in currentOptions.Options) {
                    if (option.ID == optionIndex) {
                        HandleLine(option.Line);
                        return;
                    }
                }

                GD.PrintErr($"Can't run selected option ({optionIndex}) as a line: couldn't find the option's associated {nameof(Yarn.Line)} object");
                ContinueDialogue();
            }
            else {
                ContinueDialogue();
            }
        }

        /// <summary>
        /// Parses the command string inside <paramref name="command"/>,
        /// attempts to find a suitable handler from <see
        /// cref="commandHandlers"/>, and invokes it if found.
        /// </summary>
        /// <param name="command">The <see cref="Command"/> to run.</param>
        /// <param name="onSuccessfulDispatch">A method to run if a command
        /// was successfully dispatched to a game object. This method is
        /// not called if a registered command handler is not
        /// found.</param>
        /// <returns>True if the command was dispatched to a game object;
        /// false otherwise.</returns>
        Task<CommandDispatchResult> DispatchCommandToRegisteredHandlers(Command command, Action onSuccessfulDispatch) {
            return DispatchCommandToRegisteredHandlers(command.Text, onSuccessfulDispatch);
        }

        /// <inheritdoc cref="DispatchCommandToRegisteredHandlers(Command,
        /// Action)"/>
        /// <param name="command">The text of the command to
        /// dispatch.</param>
        internal async Task<CommandDispatchResult> DispatchCommandToRegisteredHandlers(string command, Action onSuccessfulDispatch) {
            var commandTokens = SplitCommandText(command).ToArray();

            if (commandTokens.Length == 0) {
                // Nothing to do.
                return CommandDispatchResult.NotFound;
            }

            var firstWord = commandTokens[0];

            if (commandHandlers.ContainsKey(firstWord) == false) {
                // We don't have a registered handler for this command, but
                // some other part of the game might.
                return CommandDispatchResult.NotFound;
            }

            var @delegate = commandHandlers[firstWord];
            var methodInfo = @delegate.Method;

            object[] finalParameters;

            try {
                finalParameters = ActionManager.ParseArgs(methodInfo, commandTokens);
            }
            catch (ArgumentException e) {
                GD.PushError($"Can't run command {firstWord}: {e.Message}");
                return CommandDispatchResult.Failed;
            }

            if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType)) {
                // This delegate returns a YieldInstruction of some kind
                // (e.g. a Coroutine). Run it, and wait for it to finish
                // before calling onSuccessfulDispatch.
                await WaitForYieldInstruction(@delegate, finalParameters, onSuccessfulDispatch);
            }
            else if (typeof(void) == methodInfo.ReturnType) {
                // This method does not return anything. Invoke it and call
                // our completion handler.
                @delegate.DynamicInvoke(finalParameters);

                onSuccessfulDispatch();
            }
            else {
                GD.PushError($"Cannot run command {firstWord}: the provided delegate does not return a valid type (permitted return types are YieldInstruction or void)");
                return CommandDispatchResult.Failed;
            }

            return CommandDispatchResult.Success;
        }

        private static async Task WaitForYieldInstruction(Delegate @theDelegate, object[] finalParametersToUse, Action onSuccessfulDispatch) {
            // Invoke the delegate.
            var yieldInstruction = @theDelegate.DynamicInvoke(finalParametersToUse) as Task;

            // GD.Print("Wait for yield");

            await yieldInstruction;

            // GD.Print("Done yielding");

            // Call the completion handler.
            onSuccessfulDispatch();
        }

        internal Task<CommandDispatchResult> DispatchCommandToGameObject(Yarn.Command command, Action onSuccessfulDispatch) {
            // Call out to the string version of this method, because
            // Yarn.Command's constructor is only accessible from inside
            // Yarn Spinner, but we want to be able to unit test. So, we
            // extract it, and call the underlying implementation, which is
            // testable.
            return DispatchCommandToGameObject(command.Text, onSuccessfulDispatch);
        }

        internal async Task<CommandDispatchResult> DispatchCommandToGameObject(string command, System.Action onSuccessfulDispatch) {
            if (string.IsNullOrEmpty(command)) {
                throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));
            }

            if (onSuccessfulDispatch is null) {
                throw new ArgumentNullException(nameof(onSuccessfulDispatch));
            }

            CommandDispatchResult commandExecutionResult = ActionManager.TryExecuteCommand(SplitCommandText(command).ToArray(), out object returnValue);
            if (commandExecutionResult != CommandDispatchResult.Success) {
                return commandExecutionResult;
            }

            var task = returnValue as Task;

            if (task != null) {
                // Start the task. When it's done, it will continue execution.
                // Wait for this command task to complete
                await task;
                // And then signal that we're done
                onSuccessfulDispatch.Invoke();
            }
            else {
                // no task, so we're done!
                onSuccessfulDispatch();
            }
            return CommandDispatchResult.Success;
        }

        private void PrepareForLines(IEnumerable<string> lineIDs) {
            lineProvider.PrepareForLines(lineIDs);
        }

        /// <summary>
        /// Called when a <see cref="DialogueViewBase"/> has finished
        /// delivering its line.
        /// </summary>
        /// <param name="dialogueView">The view that finished delivering
        /// the line.</param>
        private void DialogueViewCompletedDelivery(DialogueViewBase dialogueView) {
            // A dialogue view just completed its delivery. Remove it from
            // the set of active views.
            ActiveDialogueViews.Remove(dialogueView);

            // Have all of the views completed?
            if (ActiveDialogueViews.Count == 0) {
                DismissLineFromViews(dialogueViews);
            }
        }

        // this is similar to the above but for the interrupt
        // main difference is a line continues automatically every interrupt finishes
        private void DialogueViewCompletedInterrupt(DialogueViewBase dialogueView) {
            ActiveDialogueViews.Remove(dialogueView);

            if (ActiveDialogueViews.Count == 0) {
                DismissLineFromViews(dialogueViews);
            }
        }

        void ContinueDialogue() {
            CurrentLine = null;
            Dialogue.Continue();
        }

        /// <summary>
        /// Called by a <see cref="DialogueViewBase"/> derived class from
        /// <see cref="dialogueViews"/> to inform the <see
        /// cref="DialogueRunner"/> that the user intents to proceed to the
        /// next line.
        /// </summary>
        public void OnViewRequestedInterrupt() {
            if (CurrentLine == null) {
                // There's no active line, so there's nothing that can be
                // done here.
                GD.PushWarning($"Dialogue runner was asked to advance but there is no current line");
                return;
            }

            // asked to advance when there are no active views
            // this means the views have already processed the lines as needed
            // so we can ignore this action
            if (ActiveDialogueViews.Count == 0) {
                GD.Print("user requested advance, all views finished, ignoring interrupt");
                return;
            }

            // now because lines are fully responsible for advancement the only advancement allowed is interruption
            InterruptLine();
        }

        private void DismissLineFromViews(IEnumerable<DialogueViewBase> dialogueViews) {
            ActiveDialogueViews.Clear();

            foreach (var dialogueView in dialogueViews) {
                // Skip any dialogueView that is null or not enabled
                if (dialogueView == null || dialogueView.Visible == false) {
                    continue;
                }

                // we do this in two passes - first by adding each
                // dialogueView into ActiveDialogueViews, then by asking
                // them to dismiss the line - because calling
                // view.DismissLine might immediately call its completion
                // handler (which means that we'd be repeatedly returning
                // to zero active dialogue views, which means
                // DialogueViewCompletedDismissal will mark the line as
                // entirely done)
                ActiveDialogueViews.Add(dialogueView);
            }

            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null || dialogueView.Visible == false) {
                    continue;
                }

                dialogueView.DismissLine(() => DialogueViewCompletedDismissal(dialogueView));
            }
        }

        private void DialogueViewCompletedDismissal(DialogueViewBase dialogueView) {
            // A dialogue view just completed dismissing its line. Remove
            // it from the set of active views.
            ActiveDialogueViews.Remove(dialogueView);

            // Have all of the views completed dismissal?
            if (ActiveDialogueViews.Count == 0) {
                // Then we're ready to continue to the next piece of
                // content.
                ContinueDialogue();
            }
        }
        #endregion

        /// <summary>
        /// Splits input into a number of non-empty sub-strings, separated
        /// by whitespace, and grouping double-quoted strings into a single
        /// sub-string.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <returns>A collection of sub-strings.</returns>
        /// <remarks>
        /// This method behaves similarly to the <see
        /// cref="string.Split(char[], StringSplitOptions)"/> method with
        /// the <see cref="StringSplitOptions"/> parameter set to <see
        /// cref="StringSplitOptions.RemoveEmptyEntries"/>, with the
        /// following differences:
        ///
        /// <list type="bullet">
        /// <item>Text that appears inside a pair of double-quote
        /// characters will not be split.</item>
        ///
        /// <item>Text that appears after a double-quote character and
        /// before the end of the input will not be split (that is, an
        /// unterminated double-quoted string will be treated as though it
        /// had been terminated at the end of the input.)</item>
        ///
        /// <item>When inside a pair of double-quote characters, the string
        /// <c>\\</c> will be converted to <c>\</c>, and the string
        /// <c>\"</c> will be converted to <c>"</c>.</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<string> SplitCommandText(string input) {
            var reader = new System.IO.StringReader(input.Normalize());

            int c;

            var results = new List<string>();
            var currentComponent = new System.Text.StringBuilder();

            while ((c = reader.Read()) != -1) {
                if (char.IsWhiteSpace((char)c)) {
                    if (currentComponent.Length > 0) {
                        // We've reached the end of a run of visible
                        // characters. Add this run to the result list and
                        // prepare for the next one.
                        results.Add(currentComponent.ToString());
                        currentComponent.Clear();
                    }
                    else {
                        // We encountered a whitespace character, but
                        // didn't have any characters queued up. Skip this
                        // character.
                    }

                    continue;
                }
                else if (c == '\"') {
                    // We've entered a quoted string!
                    while (true) {
                        c = reader.Read();
                        if (c == -1) {
                            // Oops, we ended the input while parsing a
                            // quoted string! Dump our current word
                            // immediately and return.
                            results.Add(currentComponent.ToString());
                            return results;
                        }
                        else if (c == '\\') {
                            // Possibly an escaped character!
                            var next = reader.Peek();
                            if (next == '\\' || next == '\"') {
                                // It is! Skip the \ and use the character after it.
                                reader.Read();
                                currentComponent.Append((char)next);
                            }
                            else {
                                // Oops, an invalid escape. Add the \ and
                                // whatever is after it.
                                currentComponent.Append((char)c);
                            }
                        }
                        else if (c == '\"') {
                            // The end of a string!
                            break;
                        }
                        else {
                            // Any other character. Add it to the buffer.
                            currentComponent.Append((char)c);
                        }
                    }

                    results.Add(currentComponent.ToString());
                    currentComponent.Clear();
                }
                else {
                    currentComponent.Append((char)c);
                }
            }

            if (currentComponent.Length > 0) {
                results.Add(currentComponent.ToString());
            }

            return results;
        }
    }
}