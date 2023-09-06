using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotYarn {

    /// <summary>
    /// Contains coroutine methods that apply visual effects. This class is used
    /// by <see cref="LineView"/> to handle animating the presentation of lines.
    /// </summary>
    public static class Effects {
        /// <summary>
        /// An async <see cref="Task"/> that fades a <see cref="Control"/>'s <see cref="CanvasItem.Modulate"/>
        /// from <paramref name="from"/> to <paramref name="to"/> over the
        /// course of <paramref name="fadeTime"/> seconds
        /// </summary>
        /// <param name="from">The opacity value to start fading from, ranging
        /// from 0 to 1.</param>
        /// <param name="to">The opacity value to end fading at, ranging from 0
        /// to 1.</param>
        /// <param name="fadeTime">The amount of time in seconds to fade the opacity.</param>
        /// <param name="stopToken">A <see cref="CancellationTokenSource"/> that
        /// can be used to interrupt the <see cref="Task"/></param>
        public static async Task FadeAlpha(Control control, float from, float to, float fadeTime, CancellationTokenSource stopToken = null) {
            // GD.Print("[Effects.FadeAlpha] Start");

            Color c = control.Modulate;
            c.A = from;

            double start = Godot.Time.GetUnixTimeFromSystem();
            double end = start + fadeTime;

            while(Godot.Time.GetUnixTimeFromSystem() < end) {
                if (stopToken?.IsCancellationRequested ?? false) {
                    break;
                }

                double t = Mathf.InverseLerp(start, end, Godot.Time.GetUnixTimeFromSystem());
                float a = Mathf.Lerp(from, to, (float)t);

                c.A = a;
                control.Modulate = c;

                await control.ToSignal(control.GetTree(), "process_frame");
            }

            c.A = to;
            control.Modulate = c;

            // If our destination alpha is zero, disable interactibility,
            // because the canvas group is now invisible.
            if (to <= 0) {
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
            else {
                // Otherwise, enable interactibility, because it's visible.
                control.MouseFilter = Control.MouseFilterEnum.Stop;
            }

            // GD.Print("[Effects.FadeAlpha] Complete");
        }

        /// <summary>
        /// An <see cref="async"/> <see cref="Task"/> that gradually reveals the text in a <see
        /// cref="RichTextLabel"/> object over time.
        /// </summary>
        /// <remarks>
        /// <para>This method works by adjusting the value of the <paramref name="text"/> parameter's <see cref="RichTextLabel.VisibleCharacters"/> property. This means that word wrapping will not change half-way through the presentation of a word.</para>
        /// <para style="note">Depending on the value of <paramref name="lettersPerSecond"/>, <paramref name="onCharacterTyped"/> may be called multiple times per frame.</para>
        /// <para>Due to an internal implementation detail of <see cref="RichTextLabel"/>, this method will always take at least one frame to execute, regardless of the length of the <paramref name="text"/> parameter's text.</para>
        /// </remarks>
        /// <param name="text">A <see cref="RichTextLabel"/> object to reveal the text
        /// of.</param>
        /// <param name="lettersPerSecond">The number of letters that should be
        /// revealed per second.</param>
        /// <param name="onCharacterTyped">An <see cref="Action"/> that should be called for each character that was revealed.</param>
        /// <param name="stopToken">A <see cref="CancellationTokenSource"/> that
        /// can be used to interrupt the <see cref="Task"/></param>
        public static async Task Typewriter(RichTextLabel text, float lettersPerSecond, Action onCharacterTyped, CancellationTokenSource stopToken = null) {
            // GD.Print("[Effects.Typewriter] Start");

            // Start with everything invisible
            text.VisibleCharacters = 0;

            // Wait a single frame to let the text component process its
            // content, otherwise text.textInfo.characterCount won't be
            // accurate
            await text.ToSignal(text.GetTree(), "process_frame");

            // How many visible characters are present in the text?
            var characterCount = text.GetTotalCharacterCount();

            // Early out if letter speed is zero, text length is zero
            if (lettersPerSecond <= 0 || characterCount == 0) {
                // Show everything and return
                text.VisibleCharacters = -1;
                return;
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
                if (stopToken?.IsCancellationRequested ?? false) {
                    text.VisibleCharacters = -1;
                    // GD.Print("[Effects.Typewriter] Interrupt");
                    break;
                }

                // We need to show as many letters as we have accumulated
                // time for.
                while (accumulator >= secondsPerLetter) {
                    text.VisibleCharacters += 1;
                    onCharacterTyped?.Invoke();
                    accumulator -= secondsPerLetter;
                }

                accumulator += text.GetProcessDeltaTime();

                await text.ToSignal(text.GetTree(), "process_frame");
            }

            // We either finished displaying everything, or were
            // interrupted. Either way, display everything now.
            text.VisibleCharacters = characterCount;
            // GD.Print("[Effects.Typewriter] Complete");
        }
    }
}