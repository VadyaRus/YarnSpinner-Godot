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
using System.Collections;
using System.Collections.Generic;
using Godot;

namespace Yarn.GodotYarn {
    using Node = Godot.Node;

    public partial class OptionListView : DialogueViewBase {
        [Export]
        Control optionGroup;
        [Export]
        PackedScene optionViewPrefab;
        [Export]
        RichTextLabel lastLineText;
        /// <summary>
        /// The time that the fade effect will take to fade lines in.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Export(PropertyHint.Range, "0, 1, or_greater")]
        float fadeInTime = 0.25f;
        /// <summary>
        /// The time that the fade effect will take to fade lines out.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Export(PropertyHint.Range, "0, 1, or_greater")]
        float fadeOutTime = 0.05f;

        [Export]
        bool showUnavailableOptions = false;

        /// <summary>
        /// A cached pool of OptionView objects so that we can reuse them
        /// </summary>
        List<OptionView> optionViews = new List<OptionView>();

        /// <summary>
        /// The method we should call when an option has been selected.
        /// </summary>
        Action<int> OnOptionSelected;

        /// <summary>
        /// The line we saw most recently.
        /// </summary>
        LocalizedLine lastSeenLine;

        public override void _Ready() {
            Color c = this.Modulate;
            c.A = 0;
            this.Modulate = c;

            this.MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
            // Don't do anything with this line except note it and
            // immediately indicate that we're finished with it. RunOptions
            // will use it to display the text of the previous line.

            this.lastSeenLine = dialogueLine;
            onDialogueLineFinished?.Invoke();
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) {
            RunOptionsInternal(dialogueOptions, onOptionSelected);
        }

        private async void RunOptionsInternal(DialogueOption[] dialogueOptions, Action<int> onOptionSelected) {
            // Hide all existing option views
            foreach (var optionView in optionViews) {
                optionView.Visible = false;
            }

            // If we don't already have enough option views, create more
            while (dialogueOptions.Length > optionViews.Count) {
                var optionView = CreateNewOptionView();
                optionView.Visible = false;
            }

            // Set up all of the option views
            int optionViewsCreated = 0;

            for (int i = 0; i < dialogueOptions.Length; i++) {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false && showUnavailableOptions == false) {
                    // Don't show this option.
                    continue;
                }

                optionView.Visible = true;

                optionView.Option = option;

                // The first available option is selected by default
                if (optionViewsCreated == 0) {
                    optionView.GrabFocus();
                }

                optionViewsCreated += 1;
            }

            // Update the last line, if one is configured
            if (lastLineText != null) {
                if (lastSeenLine != null) {
                    lastLineText.Visible = true;
                    lastLineText.Text = lastSeenLine.Text.Text;
                } else {
                    lastLineText.Visible = false;
                }
            }

            // Note the delegate to call when an option is selected
            OnOptionSelected = onOptionSelected;

            // Fade it all in
            await Effects.FadeAlpha(this, 0, 1, fadeInTime);
        }

        /// <summary>
        /// Creates and configures a new <see cref="OptionView"/>, and adds
        /// it to <see cref="optionViews"/>.
        /// </summary>
        OptionView CreateNewOptionView() {
            var optionView = optionViewPrefab.Instantiate<OptionView>();
            optionGroup.AddChild(optionView);

            optionView.OnOptionSelected = OptionViewWasSelected;
            optionViews.Add(optionView);

            return optionView;
        }

        /// <summary>
        /// Called by <see cref="OptionView"/> objects.
        /// </summary>
        void OptionViewWasSelected(DialogueOption option) {
            OptionViewWasSelectedInternal(option);
        }

        private async void OptionViewWasSelectedInternal(DialogueOption selectedOption) {
            await Effects.FadeAlpha(this, 1, 0, fadeOutTime);
            OnOptionSelected(selectedOption.DialogueOptionID);
        }
    }
}