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
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yarn.GodotYarn {
    /// <summary>
    /// A Dialogue View that presents lines of dialogue, using Godot UI
    /// elements.
    /// </summary>
    // [GlobalClass]
    public partial class LineView : DialogueViewBase {
        /// <summary>
        /// The <see cref="RichTextLabel"/> object that displays the text of
        /// dialogue lines.
        /// </summary>
        [Export]
        RichTextLabel _lineText;
        [Export]
        bool _autoAdvance;
        /// <summary>
        /// Controls whether the line view should fade in when lines appear, and
        /// fade out when lines disappear.
        /// </summary>
        /// <remarks><para>If this value is <see langword="true"/>, the <see
        /// cref="Control"/> object's alpha property will animate from 0 to
        /// 1 over the course of <see cref="fadeInTime"/> seconds when lines
        /// appear, and animate from 1 to zero over the course of <see
        /// cref="fadeOutTime"/> seconds when lines disappear.</para>
        /// <para>If this value is <see langword="false"/>, the <see
        /// cref="Control"/> object will appear instantaneously.</para>
        /// </remarks>
        /// <seealso cref="Control"/>
        /// <seealso cref="fadeInTime"/>
        /// <seealso cref="fadeOutTime"/>
        [Export]
        bool _useFadeEffect;

        /// <summary>
        /// The time that the fade effect will take to fade lines in.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Export(PropertyHint.Range, "0, 1, or_greater")]
        internal float fadeInTime = 0.25f;

        /// <summary>
        /// The time that the fade effect will take to fade lines out.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Export(PropertyHint.Range, "0, 1, or_greater")]
        internal float fadeOutTime = 0.05f;

        /// <summary>
        /// Controls whether the text of <see cref="lineText"/> should be
        /// gradually revealed over time.
        /// </summary>
        /// <remarks><para>If this value is <see langword="true"/>, the <see
        /// cref="lineText"/> object's <see
        /// cref="TMP_Text.maxVisibleCharacters"/> property will animate from 0
        /// to the length of the text, at a rate of <see
        /// cref="typewriterEffectSpeed"/> letters per second when the line
        /// appears. <see cref="onCharacterTyped"/> is called for every new
        /// character that is revealed.</para>
        /// <para>If this value is <see langword="false"/>, the <see
        /// cref="lineText"/> will all be revealed at the same time.</para>
        /// <para style="note">If <see cref="useFadeEffect"/> is <see
        /// langword="true"/>, the typewriter effect will run after the fade-in
        /// is complete.</para>
        /// </remarks>
        /// <seealso cref="lineText"/>
        /// <seealso cref="onCharacterTyped"/>
        /// <seealso cref="typewriterEffectSpeed"/>
        [Export]
        internal bool _useTypewriterEffect;

        /// <summary>
        /// The number of characters per second that should appear during a
        /// typewriter effect.
        /// </summary>
        /// <seealso cref="useTypewriterEffect"/>
        [Export(PropertyHint.Range, "0, 1, or_greater")]
        internal float _typewriterEffectSpeed = 0f;

        [Signal]
public delegate void onCharacterTypedEventHandler();

        /// <summary>
        /// The game object that represents an on-screen button that the user
        /// can click to continue to the next piece of dialogue.
        /// </summary>
        /// <remarks>
        /// <para>This game object will be made inactive when a line begins
        /// appearing, and active when the line has finished appearing.</para>
        /// <para>
        /// This field will generally refer to an object that has a <see
        /// cref="Button"/> component on it that, when clicked, calls <see
        /// cref="OnContinueClicked"/>. However, if your game requires specific
        /// UI needs, you can provide any object you need.</para>
        /// </remarks>
        /// <seealso cref="autoAdvance"/>
        [Export]
        internal Button _continueButton = null;

        /// <summary>
        /// The amount of time to wait after any line
        /// </summary>
        [Export(PropertyHint.Range, "0, 1, or_greater")]
        internal float _holdTime = 1f;

        /// <summary>
        /// Controls whether the <see cref="lineText"/> object will show the
        /// character name present in the line or not.
        /// </summary>
        /// <remarks>
        /// <para style="note">This value is only used if <see
        /// cref="characterNameText"/> is <see langword="null"/>.</para>
        /// <para>If this value is <see langword="true"/>, any character names
        /// present in a line will be shown in the <see cref="lineText"/>
        /// object.</para>
        /// <para>If this value is <see langword="false"/>, character names will
        /// not be shown in the <see cref="lineText"/> object.</para>
        /// </remarks>
        [Export]
        internal bool _showCharacterNameInLineView = true;

        /// <summary>
        /// The <see cref="Label"/> object that displays the character
        /// names found in dialogue lines.
        /// </summary>
        /// <remarks>
        /// If the <see cref="LineView"/> receives a line that does not contain
        /// a character name, this object will be left blank.
        /// </remarks>
        [Export]
        Label _characterNameText;

       /// <summary>
        /// The current <see cref="LocalizedLine"/> that this line view is
        /// displaying.
        /// </summary>
        LocalizedLine _currentLine = null;

        /// <summary>
        /// A stop token that is used to interrupt the current animation.
        /// </summary>
        private CancellationTokenSource cancellationToken;

        public override void _EnterTree() {
            Color c = this.Modulate;
            c.A = 0;
            this.Modulate = c;
            this.MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void DismissLine(Action onDismissalComplete) {
            _currentLine = null;

            DismissLineInternal(onDismissalComplete);
        }

        private async void DismissLineInternal(Action onDismissalComplete) {
            // disabling interaction temporarily while dismissing the line
            // we don't want people to interrupt a dismissal
            this.MouseFilter = MouseFilterEnum.Ignore;

            // If we're using a fade effect, run it, and wait for it to finish.
            if (_useFadeEffect) {
                await Effects.FadeAlpha(this, 1, 0, fadeOutTime);
            }

            Color c = this.Modulate;
            c.A = 0;
            this.Modulate = c;
            this.MouseFilter = MouseFilterEnum.Ignore;
            // turning interaction back on, if it needs it
            // canvasGroup.interactable = interactable;
            onDismissalComplete();
        }

        /// <inheritdoc/>
        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
            // Stop any coroutines currently running on this line view (for
            // example, any other RunLine that might be running)

            // Begin running the line as an async function.
            RunLineInternal(dialogueLine, onDialogueLineFinished);
        }

        private async void RunLineInternal(LocalizedLine dialogueLine, Action onDialogueLineFinished) {
            _currentLine = dialogueLine;

            // Run any presentations as a single coroutine. If this is stopped,
            // which UserRequestedViewAdvancement can do, then we will stop all
            // of the animations at once.
            _lineText.Visible = true;

            // canvasGroup.gameObject.SetActive(true);

            // Hide the continue button until presentation is complete (if
            // we have one).
            if (_continueButton != null) {
                _continueButton.Visible = false;
            }

            if (_characterNameText != null) {
                // If we have a character name text view, show the character
                // name in it, and show the rest of the text in our main
                // text view.
                _characterNameText.Text = dialogueLine.CharacterName;
                _lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
            }
            else {
                // We don't have a character name text view. Should we show
                // the character name in the main text view?
                if (_showCharacterNameInLineView) {
                    // Yep! Show the entire text.
                    _lineText.Text = dialogueLine.Text.Text;
                }
                else {
                    // Nope! Show just the text without the character name.
                    _lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
                }
            }

            if (_useTypewriterEffect) {
                // If we're using the typewriter effect, hide all of the
                // text before we begin any possible fade (so we don't fade
                // in on visible text).
                _lineText.VisibleCharacters = 0;
            }
            else {
                // Ensure that the max visible characters is effectively
                // unlimited.
                _lineText.VisibleCharacters = -1;
            }

            // If we're using the fade effect, start it, and wait for it to
            // finish.
            if (_useFadeEffect) {
                await Effects.FadeAlpha(this, 0, 1, fadeInTime);
                /*
                yield return StartCoroutine(Effects.FadeAlpha(this, 0, 1, fadeInTime, currentStopToken));
                if (currentStopToken.WasInterrupted) {
                    // The fade effect was interrupted. Stop this entire
                    // coroutine.
                    yield break;
                }
                */
            }

            Color c;

            // If we're using the typewriter effect, start it, and wait for
            // it to finish.
            if (_useTypewriterEffect) {
                // setting the canvas all back to its defaults because if we didn't also fade we don't have anything visible
                c = this.Modulate;
                c.A = 1;
                this.Modulate = c;
                this.MouseFilter = MouseFilterEnum.Stop;

                cancellationToken = new CancellationTokenSource();
                await Effects.Typewriter(_lineText, _typewriterEffectSpeed, () =>EmitSignal("onCharacterTyped"), cancellationToken);
            }

            // All of our text should now be visible.
            if (cancellationToken != null) {
                cancellationToken.Dispose();
            }
            cancellationToken = null;

            _lineText.VisibleCharacters = -1;

            // Our view should at be at full opacity.
            c = this.Modulate;
            c.A = 1.0f;
            this.Modulate = c;
            this.MouseFilter = MouseFilterEnum.Stop;

            // Show the continue button, if we have one.
            if (_continueButton != null) {
                // GD.Print("Displaying continue button");
                _continueButton.Visible = true;
            }

            // If we have a hold time, wait that amount of time, and then
            // continue.
            if (_holdTime > 0) {
                await Task.Delay((int)(_holdTime * 1000));
            }

            if (_autoAdvance == false) {
                // The line is now fully visible, and we've been asked to not
                // auto-advance to the next line. Stop here, and don't call the
                // completion handler - we'll wait for a call to
                // UserRequestedViewAdvancement, which will interrupt this
                // coroutine.
                return;
            }

            // Our presentation is complete; call the completion handler.
            onDialogueLineFinished();
        }

        /// <inheritdoc/>
        public override void UserRequestedViewAdvancement() {
            // We received a request to advance the view. If we're in the middle of
            // an animation, skip to the end of it. If we're not current in an
            // animation, interrupt the line so we can skip to the next one.

            // we have no line, so the user just mashed randomly
            if (_currentLine == null) {
                return;
            }

            // GD.Print("UserRequestedViewAdvancement");

            // we may want to change this later so the interrupted
            // animation coroutine is what actually interrupts
            // for now this is fine.
            // Is an animation running that we can stop?
            if (cancellationToken != null && cancellationToken.IsCancellationRequested == false) {
            // Stop the current animation, and skip to the end of whatever
                // started it.
                cancellationToken.Cancel();
                return;
            }

            // No animation is now running. Signal that we want to
            // interrupt the line instead.
            requestInterrupt?.Invoke();
        }

        /// <summary>
        /// Called when the <see cref="continueButton"/> is clicked.
        /// </summary>
        public void OnContinueClicked() {
            // When the Continue button is clicked, we'll do the same thing as
            // if we'd received a signal from any other part of the game (for
            // example, if a DialogueAdvanceInput had signalled us.)
            UserRequestedViewAdvancement();
        }
    }
}