using CBRE.DataStructures.GameData;
using CBRE.Providers.Texture;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CBRE.Editor.UI.ObjectProperties.SmartEdit
{
    [SmartEdit(VariableType.Decal)]
    [SmartEdit(VariableType.Material)]
    [SmartEdit(VariableType.Sprite)]
    internal class SmartEditTextureBrowser : SmartEditControl
    {
        private readonly TextBox _textBox;
        public SmartEditTextureBrowser()
        {
            _textBox = new TextBox { Width = 180 };
            _textBox.TextChanged += (sender, e) => OnValueChanged();
            Controls.Add(_textBox);

            var btn = new Button { Text = "Browse...", Margin = new Padding(1), Height = 24 };
            btn.Click += OpenModelBrowser;
            Controls.Add(btn);
        }

        private void OpenModelBrowser(object sender, EventArgs e)
        {
            using (var tb = new TextureBrowser())
            {
                tb.SetTextureList(GetTextureList());
                tb.SetSelectedTextures(GetSelectedTextures());
                tb.SetFilterText(GetFilterText());
                tb.ShowDialog();
                if (tb.SelectedTexture != null)
                {
                    _textBox.Text = tb.SelectedTexture.Name;
                }
            }
        }

        private IEnumerable<TextureItem> GetSelectedTextures()
        {
            var tex = Document.TextureCollection.GetItem(_textBox.Text);
            if (tex != null) yield return tex;
        }

        private IEnumerable<TextureItem> GetTextureList()
        {
            return Document.TextureCollection.GetAllBrowsableItems();
        }

        private string GetFilterText()
        {
            //TODO: probably not going to be used
            switch (Property.VariableType)
            {
                case VariableType.Sprite:
                    return "sprites/";
                case VariableType.Decal:
                    return "decals/";
                default:
                    return null;
            }
        }

        protected override string GetName()
        {
            return OriginalName;
        }

        protected override string GetValue()
        {
            return _textBox.Text;
        }

        protected override void OnSetProperty()
        {
            _textBox.Text = PropertyValue;
        }
    }
}