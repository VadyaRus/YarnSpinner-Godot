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
#if TOOLS
namespace Yarn.GodotYarn.Editor {
    using Godot;
    using Godot.Collections;
    using Node = Godot.Node;

    /// <summary>
    /// This class is responsible for the source scripts editor property.
    /// </summary>
    public partial class SourceScriptsEditorProperty : EditorProperty {
        private VBoxContainer verticalBox;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceScriptsEditorProperty"/> class.
        /// </summary>
        public SourceScriptsEditorProperty() {
            verticalBox = new VBoxContainer();
            this.AddChild(verticalBox);
        }

        /// <inheritdoc/>
        public override void _UpdateProperty() {
            GodotObject obj = this.GetEditedObject();

            if (obj == null) {
                return;
            }

            var prop = obj.Get(this.GetEditedProperty()).AsGodotObjectArray<Resource>();

            if (prop == null) {
                return;
            }

            for (int i = 0; i < verticalBox.GetChildCount(); ++i) {
                verticalBox.GetChild(i).QueueFree();
            }

            for (int i = 0; i < prop.Length; ++i) {
                GD.Print(prop[i].ResourceName);

                Label button = new ();
                button.Text = prop[i].ResourceName;

                verticalBox.AddChild(button);
            }

            // Create a button to add a new resource to the array
            Button addButton = new ();
            addButton.Text = "+";
            addButton.Pressed += AddResource;

            verticalBox.AddChild(addButton);
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
    }
}

#endif