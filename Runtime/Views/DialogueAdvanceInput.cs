using Godot;

namespace Yarn.GodotYarn {
    /// <summary>
    /// A component that listens for user input, and uses it to notify a
    /// dialogue view that the user wishes to advance to the next step in the
    /// dialogue.
    /// </summary>
    /// <remarks>
    /// <para>This class may be used with the Unity Input System, or the legacy
    /// Input Manager. The specific type of input it's looking for is configured
    /// via the <see cref="continueActionType"/> field.</para>
    /// <para>When the configured input occurs, this component calls the <see
    /// cref="DialogueViewBase.UserRequestedViewAdvancement"/> method on its
    /// <see cref="dialogueView"/>.
    /// </para>
    /// </remarks>
    // [GlobalClass]
    public partial class DialogueAdvanceInput : Godot.Node {
        /// <summary>
        /// The type of input that this Node is listening for in order to signal that its dialogue view should advance.
        /// </summary>
        public enum ContinueActionType {
            /// <summary>
            /// The component is listening for no input. This component will not
            /// signal to <see cref="dialogueView"/> that it should advance.
            /// </summary>
            None,

            /// <summary>
            /// The component is listening for a key on the keyboard to be
            /// pressed.
            /// </summary>
            /// <remarks>
            /// <para style="info">This input will only be used if the legacy
            /// Input Manager is enabled.</para>
            /// </remarks>
            KeyCode,

            /// <summary>
            /// The component is listening for the action configured in <see
            /// cref="continueAction"/> to be performed.
            /// </summary>
            /// <remarks>
            /// <para style="info">This input will only be used if the Input
            /// System is installed and enabled.</para>
            /// </remarks>
            InputAction
        }

        /// <summary>
        /// The dialogue view that will be notified when the user performs the
        /// advance input (as configured by <see cref="continueActionType"/> and
        /// related fields.)
        /// </summary>
        /// <remarks>
        /// When the input is performed, this dialogue view will have its <see
        /// cref="DialogueViewBase.UserRequestedViewAdvancement"/> method
        /// called.
        /// </remarks>
        [Export] public DialogueViewBase dialogueView;

        /// <summary>
        /// The type of input that this component is listening for.
        /// </summary>
        /// <seealso cref="ContinueActionType"/>
        [Export]
        public ContinueActionType continueActionType = ContinueActionType.KeyCode;

        /// <summary>
        /// The keyboard key that this component is listening for.
        /// </summary>
        /// <remarks>
        /// <para style="info">
        /// This value is only used when <see cref="continueActionType"/> is
        /// <see cref="ContinueActionType.KeyCode"/>.
        /// </para>
        /// </remarks>
        [Export]
        public Key continueActionKeyCode = Key.Space;

        private bool _keyState;

        [Export]
        public string continueActionName = "ui_accept";

        public override void _Ready() {
            if (dialogueView == null) {
                GD.PrintErr("Unable to set default values, there is no project set");
                return;
            }
        }

        public override void _Process(double delta) {
            if (dialogueView == null) {
                return;
            }

            // We need to be configured to use a keycode or action to interrupt/continue
            // lines.
            if (continueActionType == ContinueActionType.KeyCode) {
                // Has the keycode been pressed this frame?
                if (IsContinueKeyJustPressed()) {
                    // Indicate that we want to skip/continue.
                    dialogueView.UserRequestedViewAdvancement();
                }
            }
            else if (continueActionType == ContinueActionType.InputAction) {
                // Has the action been pressed this frame?
                if (Input.IsActionJustPressed(continueActionName)) {
                    // Indicate that we want to skip/continue.
                    dialogueView.UserRequestedViewAdvancement();
                }
            }
        }

        private bool IsContinueKeyJustPressed() {
            if (Input.IsKeyPressed(continueActionKeyCode) == false && _keyState) {
                _keyState = false;
            }
            else if (Input.IsKeyPressed(continueActionKeyCode) && _keyState == false) {
                _keyState = true;
                return true;
            }

            return false;
        }
    }
}