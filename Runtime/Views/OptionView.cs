using System;
using Godot;

namespace Yarn.GodotYarn {
    using Node = Godot.Node;

    public partial class OptionView : Button {
        [Export] public bool showCharacterName = false;

        public Action<DialogueOption> OnOptionSelected;

        DialogueOption _option;

        bool hasSubmittedOptionSelection = false;

        public DialogueOption Option {
            get => _option;
            set {
                _option = value;

                hasSubmittedOptionSelection = false;

                // When we're given an Option, use its text and update our
                // interactibility.
                if (showCharacterName) {
                    this.Text = value.Line.Text.Text;
                }
                else {
                    this.Text = value.Line.TextWithoutCharacterName.Text;
                }

                this.Disabled = value.IsAvailable == false;
            }
        }

        public void InvokeOptionSelected() {
            // We only want to invoke this once, because it's an error to
            // submit an option when the Dialogue Runner isn't expecting it. To
            // prevent this, we'll only invoke this if the flag hasn't been cleared already.
            if (hasSubmittedOptionSelection == false) {
                OnOptionSelected.Invoke(Option);
                hasSubmittedOptionSelection = true;
            }
        }

        public override void _Pressed() {
            InvokeOptionSelected();
        }
    }
}