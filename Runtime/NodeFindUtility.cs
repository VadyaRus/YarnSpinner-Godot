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
    using Node = Godot.Node;

    public partial class NodeFindUtility : Node {
        private static NodeFindUtility _instance;

        public override void _Ready() {
            _instance = this;
        }

        public static T Find<T>() where T : Node {
            if (_instance == null) return null;

            return Find<T>(_instance.GetTree().Root, true);
        }

        public static Node Find(System.Type type) {
            if (_instance == null) return null;

            return Find(_instance.GetTree().Root, type, true);
        }

        public static Node Find(string name) {
            if (_instance == null) return null;

            return Find(_instance.GetTree().Root, name, true);
        }

        // Recursive function to find a node of the specified type
        private static Node Find(Node parent, System.Type targetType, bool includeInternal = false) {
            // Check if the current node has the desired type
            if (parent.GetType() == targetType) {
                return parent;
            }

            // Iterate through the child nodes recursively
            for (int i = 0; i < parent.GetChildCount(includeInternal); ++i) {
                Node foundNode = Find(parent.GetChild(i, includeInternal), targetType, includeInternal);
                if (foundNode != null) {
                    return foundNode;
                }
            }

            return null;
        }

        // Recursive function to find a node of type T
        private static T Find<T>(Node parent, bool includeInternal = false) where T : Node {
            // Check if the current node has the desired type
            T node = parent as T;
            if (node != null) {
                return node;
            }

            // Iterate through the child nodes recursively
            for (int i = 0; i < parent.GetChildCount(includeInternal); ++i) {
                Node child = parent.GetChild(i, includeInternal);

                node = Find<T>(child, includeInternal);

                if (node != null) {
                    return node;
                }
            }

            // Node of type T not found
            return null;
        }

        // Recursive function to find a node by name
        private static Node Find(Node parent, string name, bool includeInternal = false) {
            // Check if the current node has the desired name
            if (parent.Name == name) {
                return parent;
            }

            // Iterate through the child nodes recursively
            for (int i = 0; i < parent.GetChildCount(); i++) {
                Node child = parent.GetChild(i);
                Node foundNode = Find(child, name, includeInternal);
                if (foundNode != null) {
                    return foundNode;
                }
            }
            // Node with the specified name not found
            return null;
        }
    }
}