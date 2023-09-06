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

    /// <summary>
    /// This class is responsible for the localization popup editor property.
    /// </summary>
    public partial class LocalizationPopupEditorProperty : EditorProperty {
        private MenuButton menuBar = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationPopupEditorProperty"/> class.
        /// </summary>
        public LocalizationPopupEditorProperty() {
            foreach (var c in Cultures.GetCultures()) {
                menuBar.GetPopup().AddItem($"{c.DisplayName}:({c.Name})");
            }
            AddChild(menuBar);

            menuBar.GetPopup().IdPressed += OnIdPressed;

            UpdateProperty();
        }

        /// <inheritdoc/>
        public override void _UpdateProperty() {
            // GD.Print("UpdateProperty");
            string nullText = "<null>";

            GodotObject obj = this.GetEditedObject();

            if (obj == null) {
                this.menuBar.Text = nullText;
                return;
            }

            var prop = obj.Get(this.GetEditedProperty()).AsGodotObject();

            if (prop == null) {
                Localization loc = new ();
                this.GetEditedObject().Set(this.GetEditedProperty(), loc);
                return;
            }

            if (prop is not Localization localization) {
                this.menuBar.Text = nullText;
                return;
            }

            var culture = Cultures.GetCulture(localization.LocaleCode);
            menuBar.Text = $"{culture.DisplayName}:({culture.Name})";
        }

        private void OnIdPressed(long id) {
            int index = (int)id;

            string localeCode = menuBar.GetPopup().GetItemText(index);
            menuBar.Text = localeCode;

            localeCode = localeCode.Split(':')[1];
            localeCode = localeCode.Trim('(');
            localeCode = localeCode.Trim(')');

            this.GetEditedObject().Get(this.GetEditedProperty()).AsGodotObject().Set("_localeCode", localeCode);
        }
    }
}
#endif