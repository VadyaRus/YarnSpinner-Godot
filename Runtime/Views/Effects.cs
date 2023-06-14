using System;
using System.Collections;
using Godot;

namespace Yarn.GodotYarn {

    /// <summary>
    /// Contains coroutine methods that apply visual effects. This class is used
    /// by <see cref="LineView"/> to handle animating the presentation of lines.
    /// </summary>
    public static class Effects {
        /// <summary>
        /// A coroutine that fades a <see cref="CanvasGroup"/> object's opacity
        /// from <paramref name="from"/> to <paramref name="to"/> over the
        /// course of <see cref="fadeTime"/> seconds, and then invokes <paramref
        /// name="onComplete"/>.
        /// </summary>
        /// <param name="from">The opacity value to start fading from, ranging
        /// from 0 to 1.</param>
        /// <param name="to">The opacity value to end fading at, ranging from 0
        /// to 1.</param>
        /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that
        /// can be used to interrupt the coroutine.</param>
        public static IEnumerator FadeAlpha(Control control, float from, float to, float fadeTime, CoroutineInterruptToken stopToken = null) {
            // GD.Print("[Effects.FadeAlpha] Start");
            stopToken?.Start();

            Color c = control.Modulate;
            c.A = from;

            double timeElapsed = 0.0;

            while (timeElapsed < fadeTime) {
                if (stopToken?.WasInterrupted ?? false) {
                    yield break;
                }

                double fraction = timeElapsed / fadeTime;
                timeElapsed += control.GetProcessDeltaTime();

                float a = Mathf.Lerp(from, to, (float)fraction);

                c.A = a;
                control.Modulate = c;
                yield return null;
            }

            c.A = to;
            control.Modulate = c;

            // If our destination alpha is zero, disable interactibility,
            // because the canvas group is now invisible.
            if (to == 0) {
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
            else {
                // Otherwise, enable interactibility, because it's visible.
                control.MouseFilter = Control.MouseFilterEnum.Stop;
            }

            stopToken?.Complete();
            // GD.Print("[Effects.FadeAlpha] Complete");
        }

        /// <summary>
        /// A coroutine that gradually reveals the text in a <see
        /// cref="TextMeshProUGUI"/> object over time.
        /// </summary>
        /// <remarks>
        /// <para>This method works by adjusting the value of the <paramref name="text"/> parameter's <see cref="TextMeshProUGUI.maxVisibleCharacters"/> property. This means that word wrapping will not change half-way through the presentation of a word.</para>
        /// <para style="note">Depending on the value of <paramref name="lettersPerSecond"/>, <paramref name="onCharacterTyped"/> may be called multiple times per frame.</para>
        /// <para>Due to an internal implementation detail of TextMeshProUGUI, this method will always take at least one frame to execute, regardless of the length of the <paramref name="text"/> parameter's text.</para>
        /// </remarks>
        /// <param name="text">A TextMeshProUGUI object to reveal the text
        /// of.</param>
        /// <param name="lettersPerSecond">The number of letters that should be
        /// revealed per second.</param>
        /// <param name="onCharacterTyped">An <see cref="Action"/> that should be called for each character that was revealed.</param>
        /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that
        /// can be used to interrupt the coroutine.</param>
        public static IEnumerator Typewriter(RichTextLabel text, float lettersPerSecond, Action onCharacterTyped, CoroutineInterruptToken stopToken = null) {
            stopToken?.Start();
            // GD.Print("[Effects.Typewriter] Start");

            // Start with everything invisible
            text.VisibleCharacters = 0;

            // Wait a single frame to let the text component process its
            // content, otherwise text.textInfo.characterCount won't be
            // accurate
            yield return null;

            // How many visible characters are present in the text?
            var characterCount = text.GetTotalCharacterCount();

            // Early out if letter speed is zero, text length is zero
            if (lettersPerSecond <= 0 || characterCount == 0) {
                // Show everything and return
                text.VisibleCharacters = -1;
                stopToken?.Complete();
                yield break;
            }

            // Convert 'letters per second' into its inverse
            double secondsPerLetter = 1.0 / lettersPerSecond;

            // If lettersPerSecond is larger than the average framerate, we
            // need to show more than one letter per frame, so simply
            // adding 1 letter every secondsPerLetter won't be good enough
            // (we'd cap out at 1 letter per frame, which could be slower
            // than the user requested.)
            //
            // Instead, we'll accumulate time every frame, and display as
            // many letters in that frame as we need to in order to achieve
            // the requested speed.
            var accumulator = text.GetProcessDeltaTime();

            while (text.VisibleCharacters < characterCount) {
                if (stopToken?.WasInterrupted ?? false) {
                    text.VisibleCharacters = -1;
                    // GD.Print("[Effects.Typewriter] Interrupt");
                    yield break;
                }

                // We need to show as many letters as we have accumulated
                // time for.
                while (accumulator >= secondsPerLetter) {
                    text.VisibleCharacters += 1;
                    onCharacterTyped?.Invoke();
                    accumulator -= secondsPerLetter;
                }

                accumulator += text.GetProcessDeltaTime();

                yield return null;
            }

            // We either finished displaying everything, or were
            // interrupted. Either way, display everything now.
            text.VisibleCharacters = characterCount;

            stopToken?.Complete();
            // GD.Print("[Effects.Typewriter] Complete");
        }
    }
}