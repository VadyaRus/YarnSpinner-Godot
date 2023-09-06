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

namespace Yarn.GodotYarn {
    using System;
    using Godot;

    /// <summary>
    /// This class is responsible for the yarn spinner variable storage.
    /// </summary>
    public abstract partial class VariableStorageBehaviour : Godot.Node, Yarn.IVariableStorage {
        /// <summary>
        /// This method is called when the dialogue system wants to get the value of a variable.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <returns>True if the variable exists, otherwise false.</returns>
        /// <param name="result">The value of the variable, or null if it doesn't exist.</param>
        /// <typeparam name="T">The type of the variable.</typeparam>
        public abstract bool TryGetValue<T>(string variableName, out T result);

        /// <summary>
        /// This method is called when the dialogue system wants to set a variable's value.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="stringValue">The new value of the variable.</param>
        public abstract void SetValue(string variableName, string stringValue);

        /// <summary>
        /// This method is called when the dialogue system wants to set a variable's value.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="floatValue">The new value of the variable.</param>
        public abstract void SetValue(string variableName, float floatValue);

        /// <summary>
        /// This method is called when the dialogue system wants to set a variable's value.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="boolValue">The new value of the variable.</param>
        public abstract void SetValue(string variableName, bool boolValue);

        /// <summary>
        /// This method is called to clear all variables from memory.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// This method is called when the dialogue system wants to know if a variable exists.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <returns>True if the variable exists, otherwise false.</returns>
        public abstract bool Contains(string variableName);
    }
}