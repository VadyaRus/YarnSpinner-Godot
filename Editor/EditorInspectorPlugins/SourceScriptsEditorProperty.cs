#if TOOLS
using Godot;
using Godot.Collections;

namespace Yarn.GodotYarn.Editor {
    using Node = Godot.Node;

    public partial class SourceScriptsEditorProperty : EditorProperty {
        private VBoxContainer _verticalBox;

        public SourceScriptsEditorProperty() {
            _verticalBox = new VBoxContainer();
            this.AddChild(_verticalBox);
        }

        private void AddResource() {
            GodotObject obj = this.GetEditedObject();

            if (obj == null) {
                return;
            }

            var prop = obj.Get(this.GetEditedProperty()).AsGodotObjectArray<Resource>();

            if (prop == null) {
                return;
            }

            // You can implement your own logic here to add a new resource to the array
            // For demonstration purposes, I'm just adding a placeholder resource
            Resource[] newProp = new Resource[prop.Length];
            for (int i = 0; i < prop.Length; ++i) {
                newProp[i] = prop[i];
            }

            prop = newProp;

            obj.Set(this.GetEditedProperty(), prop);

            // Emit the signal to notify the editor that the property has changed
            // this.EmitChanged(this.GetEditedProperty(), newProp);
            UpdateProperty();
        }

        public override void _UpdateProperty() {
            GodotObject obj = this.GetEditedObject();

            if (obj == null) {
                return;
            }

            var prop = obj.Get(this.GetEditedProperty()).AsGodotObjectArray<Resource>();

            if (prop == null) {
                return;
            }

            for (int i = 0; i < _verticalBox.GetChildCount(); ++i) {
                _verticalBox.GetChild(i).QueueFree();
            }

            for (int i = 0; i < prop.Length; ++i) {
                GD.Print(prop[i].ResourceName);

                Label button = new Label();
                button.Text = prop[i].ResourceName;

                _verticalBox.AddChild(button);
            }

            // Create a button to add a new resource to the array
            Button _addButton = new Button();
            _addButton.Text = "+";
            _addButton.Pressed += AddResource;

            _verticalBox.AddChild(_addButton);
        }
    }
}

#endif