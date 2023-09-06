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