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
using System.Threading.Tasks;

namespace Yarn.GodotYarn {
    public partial class DefaultActions : Godot.Node {
        private static DefaultActions _instance;

        public DefaultActions() {
            _instance = this;
        }

        #region Commands
        /// <summary>
        /// Yarn Spinner defines two built-in commands: "wait", and "stop".
        /// Stop is defined inside the Virtual Machine (the compiler traps it
        /// and makes it a special case.) Wait is defined here in Godot.
        /// </summary>
        /// <param name="duration">How long to wait.</param>
        [YarnCommand("wait")]
        public static async Task Wait(float duration) {
            GD.Print("wait...");

            while (_instance == null) {
                await Task.Yield();
            }

            double start = Godot.Time.GetUnixTimeFromSystem();
            double end = start + duration;

            while (Godot.Time.GetUnixTimeFromSystem() < end) {
                await _instance.ToSignal(_instance.GetTree(), "process_frame");
            }
        }

        #endregion

        #region Functions

        [YarnFunction("random")]
        public static float Random() {
            return RandomRange(0, 1);
        }

        [YarnFunction("random_range")]
        public static float RandomRange(float minInclusive, float maxInclusive) {
            return (float)GD.RandRange(minInclusive, maxInclusive);
        }

        /// <summary>
        /// Pick an integer in the given range.
        /// </summary>
        /// <param name="sides">Dice range.</param>
        /// <returns>A number between <c>[1, <paramref name="sides"/>]</c>.
        /// </returns>
        [YarnFunction("dice")]
        public static int Dice(int sides) {
            return (int)GD.RandRange(1, sides + 1);
        }

        [YarnFunction("round")]
        public static int Round(float num) {
            return (int)RoundPlaces(num, 0);
        }

        [YarnFunction("round_places")]
        public static float RoundPlaces(float num, int places) {
            return (float)Math.Round(num, places);
        }

        [YarnFunction("floor")]
        public static int Floor(float num) {
            return Mathf.FloorToInt(num);
        }

        [YarnFunction("ceil")]
        public static int Ceil(float num) {
            return Mathf.CeilToInt(num);
        }

        /// <summary>
        /// Increment if integer, otherwise go to next integer.
        /// </summary>
        [YarnFunction]
        public static int Inc(float num) {
            if (Decimal(num) != 0) {
                return Mathf.CeilToInt(num);
            }
            return (int)num + 1;
        }

        /// <summary>
        /// Decrement if integer, otherwise go to previous integer.
        /// </summary>
        [YarnFunction("dec")]
        public static int Dec(float num) {
            if (Decimal(num) != 0) {
                return Mathf.FloorToInt(num);
            }
            return (int)num - 1;
        }

        /// <summary>
        /// The decimal portion of the given number.
        /// </summary>
        /// <param name="num">Number to get the decimal portion of.</param>
        /// <returns><c>[0, 1)</c></returns>
        [YarnFunction("decimal")]
        public static float Decimal(float num) {
            return num - Int(num);
        }

        /// <summary>
        /// Truncates the number into an int. This is different to
        /// <see cref="floor(float)"/> because it rounds to zero rather than
        /// <see cref="Mathf.NegativeInfinity"/>.
        /// </summary>
        /// <param name="num">Number to truncate.</param>
        /// <returns>Truncated float value as int.</returns>
        [YarnFunction("int")]
        public static int Int(float num) {
            return (int)Math.Truncate(num);
        }
        #endregion
    }
}