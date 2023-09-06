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
using System;
using Godot;

namespace Yarn.GodotYarn {
    using Node = Godot.Node;

    public partial class OptionView : Button {
        [Export]
        public bool showCharacterName = false;

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