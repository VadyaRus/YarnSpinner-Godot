using System;
using System.Collections;
using System.Reflection;
using Godot;

namespace Yarn.GodotYarn {
    public partial class DialogueRunner : Control {
        /// <summary>
        /// Represents the result of attempting to locate and call a command.
        /// </summary>
        /// <seealso cref="DispatchCommandToGameObject(Command, Action)"/>
        /// <seealso cref="DispatchCommandToRegisteredHandlers(Command, Action)"/>
        internal enum CommandDispatchResult {
            /// <summary>
            /// The command was located and successfully called.
            /// </summary>
            Success,

            /// <summary>
            /// The command was located, but failed to be called.
            /// </summary>
            Failed,

            /// <summary>
            /// The command could not be found.
            /// </summary>
            NotFound,
        }

#region CommandsAndFunctions
        /// <summary>
        /// Adds a command handler. Dialogue will pause execution after the
        /// command is called.
        /// </summary>
        /// <remarks>
        /// <para>When this command handler has been added, it can be called
        /// from your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;commandName param1 param2&gt;&gt;
        /// </code>
        ///
        /// <para>If <paramref name="handler"/> is a method that returns a <see
        /// cref="Coroutine"/>, when the command is run, the <see
        /// cref="DialogueRunner"/> will wait for the returned coroutine to stop
        /// before delivering any more content.</para>
        /// </remarks>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="handler">The <see cref="CommandHandler"/> that will be
        /// invoked when the command is called.</param>
        public void AddCommandHandler(string commandName, Delegate handler) {
            if (commandHandlers.ContainsKey(commandName)) {
                GD.PushError($"Cannot add a command handler for {commandName}: one already exists");
                return;
            }
            commandHandlers.Add(commandName, handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        /// <param name="method">The method that will be invoked when the
        /// command is called.</param>
        public void AddCommandHandler(string commandName, MethodInfo method) => AddCommandHandler(commandName, method);

        /*
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, System.Func<Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, System.Func<T1, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        */

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, System.Func<IEnumerator> handler) => AddCommandHandler(commandName, handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, System.Func<T1, IEnumerator> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, IEnumerator> handler) => AddCommandHandler(commandName, handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, IEnumerator> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, IEnumerator> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, IEnumerator> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, IEnumerator> handler) => AddCommandHandler(commandName, handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, System.Action handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, System.Action<T1> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, System.Action<T1, T2> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, System.Action<T1, T2, T3> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Action<T1, T2, T3, T4> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Action<T1, T2, T3, T4, T5> handler) => AddCommandHandler(commandName, handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Action<T1, T2, T3, T4, T5, T6> handler) => AddCommandHandler(commandName, handler);


        /// <summary>
        /// Removes a command handler.
        /// </summary>
        /// <param name="commandName">The name of the command to
        /// remove.</param>
        public void RemoveCommandHandler(string commandName) {
            commandHandlers.Remove(commandName);
        }

        /// <summary>
        /// Add a new function that returns a value, so that it can be
        /// called from Yarn scripts.
        /// </summary>
        /// <remarks>
        /// <para>When this function has been registered, it can be called from
        /// your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;if myFunction(1, 2) == true&gt;&gt;
        ///     myFunction returned true!
        /// &lt;&lt;endif&gt;&gt;
        /// </code>
        ///
        /// <para>The <c>call</c> command can also be used to invoke the function:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;call myFunction(1, 2)&gt;&gt;
        /// </code>
        /// </remarks>
        /// <param name="implementation">The <see cref="Delegate"/> that
        /// should be invoked when this function is called.</param>
        /// <seealso cref="Library"/>
        public void AddFunction(string name, Delegate implementation) {
            if (Dialogue.Library.FunctionExists(name)) {
                GD.PushError($"Cannot add function {name}: one already exists");
                return;
            }

            Dialogue.Library.RegisterFunction(name, implementation);
        }

        /// <inheritdoc cref="AddFunction(string, Delegate)" />
        /// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
        public void AddFunction<TResult>(string name, System.Func<TResult> implementation) => AddFunction(name, implementation);

        /// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})" />
        /// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
        public void AddFunction<TResult, T1>(string name, System.Func<TResult, T1> implementation) => AddFunction(name, implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1}(string, Func{TResult,T1})" />
        /// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2>(string name, System.Func<TResult, T1, T2> implementation) => AddFunction(name, implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2}(string, Func{TResult,T1,T2})" />
        /// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3>(string name, System.Func<TResult, T1, T2, T3> implementation) => AddFunction(name, implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3}(string, Func{TResult,T1,T2,T3})" />
        /// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4>(string name, System.Func<TResult, T1, T2, T3, T4> implementation) => AddFunction(name, implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4}(string, Func{TResult,T1,T2,T3,T4})" />
        /// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4, T5>(string name, System.Func<TResult, T1, T2, T3, T4, T5> implementation) => AddFunction(name, implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4,T5}(string, Func{TResult,T1,T2,T3,T4,T5})" />
        /// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4, T5, T6>(string name, System.Func<TResult, T1, T2, T3, T4, T5, T6> implementation) => AddFunction(name, implementation);

        /// <summary>
        /// Remove a registered function.
        /// </summary>
        /// <remarks>
        /// After a function has been removed, it cannot be called from
        /// Yarn scripts.
        /// </remarks>
        /// <param name="name">The name of the function to remove.</param>
        /// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
        public void RemoveFunction(string name) => Dialogue.Library.DeregisterFunction(name);
#endregion

    }
}