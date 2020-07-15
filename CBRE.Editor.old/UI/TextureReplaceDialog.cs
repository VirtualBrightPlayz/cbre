using CBRE.Common;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Editor.Actions.MapObjects.Selection;
using CBRE.Editor.Documents;
using CBRE.Providers.Texture;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace CBRE.Editor.UI
{
    public partial class TextureReplaceDialog : Form
    {
        private Document _document;

        public TextureReplaceDialog(Document document)
        {
            _document = document;
            InitializeComponent();
            BindTextureControls(Find, FindImage, FindBrowse, FindInfo);
            BindTextureControls(Replace, ReplaceImage, ReplaceBrowse, ReplaceInfo);

            ReplaceSelection.Checked = true;
            ActionExact.Checked = true;

            if (document.Selection.IsEmpty())
            {
                ReplaceSelection.Enabled = false;
                ReplaceVisible.Checked = true;
            }

            if (_document.TextureCollection.SelectedTexture != null)
            {
                var tex = _document.TextureCollection.SelectedTexture;
                Find.Text = tex.Name;
            }
        }

        private IEnumerable<MapObject> GetObjects()
        {
            if (ReplaceSelection.Checked) return _document.Selection.GetSelectedObjects();
            if (ReplaceVisible.Checked) return _document.Map.WorldSpawn.Find(x => !x.IsVisgroupHidden);
            return _document.Map.WorldSpawn.FindAll();
        }

        private bool MatchTextureName(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return false;

            var match = Find.Text;
            if (!ActionExact.Checked)
            {
                return name.ToLowerInvariant().Contains(match.ToLowerInvariant());
            }
            return String.Equals(name, match, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<Tuple<string, TextureItem, ITexture>> GetReplacements(IEnumerable<string> names)
        {
            var list = new List<Tuple<string, TextureItem, ITexture>>();
            var substitute = ActionSubstitute.Checked;
            var find = Find.Text.ToLowerInvariant();
            var replace = Replace.Text.ToLowerInvariant();

            foreach (var name in names.Select(x => x.ToLowerInvariant()).Distinct())
            {
                var n = substitute ? name.Replace(find, replace) : replace;

                var item = _document.TextureCollection.GetItem(n);
                if (item == null) continue;

                list.Add(Tuple.Create(name, item, item.GetTexture()));
            }
            return list;
        }

        public IAction GetAction()
        {
            var faces = GetObjects().OfType<Solid>().SelectMany(x => x.Faces).Where(x => MatchTextureName(x.Texture.Name)).ToList();
            if (ActionSelect.Checked)
            {
                return new ChangeSelection(faces.Select(x => x.Parent).Distinct(), _document.Selection.GetSelectedObjects());
            }
            var rescale = RescaleTextures.Checked;
            var replacements = GetReplacements(faces.Select(x => x.Texture.Name));
            Action<Document, Face> action = (doc, face) =>
                                                {
                                                    var repl = replacements.FirstOrDefault(x => x.Item1 == face.Texture.Name.ToLowerInvariant());
                                                    if (repl == null) return;
                                                    if (rescale)
                                                    {
                                                        var item = _document.TextureCollection.GetItem(face.Texture.Name);
                                                        if (item != null)
                                                        {
                                                            face.Texture.XScale *= item.Width / (decimal)repl.Item2.Width;
                                                            face.Texture.YScale *= item.Height / (decimal)repl.Item2.Height;
                                                        }
                                                    }
                                                    face.Texture.Name = repl.Item2.Name;
                                                    face.Texture.Texture = repl.Item3;
                                                    face.CalculateTextureCoordinates(true);
                                                };
            return new EditFace(faces, action, true);
        }

        private void BindTextureControls(TextBox box, PictureBox image, Button browse, Label info)
        {
            box.TextChanged += (sender, e) => UpdateTexture(box.Text, image, info);
            browse.Click += (sender, e) => BrowseTexture(box);
            UpdateTexture(box.Text, image, info);
        }

        private void BrowseTexture(TextBox box)
        {
            using (var tb = new TextureBrowser())
            {
                tb.SetTextureList(_document.TextureCollection.GetAllBrowsableItems());
                tb.ShowDialog();
                if (tb.SelectedTexture != null)
                {
                    box.Text = tb.SelectedTexture.Name;
                }
            }
        }

        private void UpdateTexture(string text, PictureBox image, Label info)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                image.Image = null;
                info.Text = "No Image";
                return;
            }

            var item = _document.TextureCollection.GetItem(text)
                       ?? new TextureItem(null, text, TextureFlags.Missing, 64, 64);

            using (var tp = _document.TextureCollection.GetStreamSource(128, 128))
            {
                var bmp = tp.GetImage(item);
                image.SizeMode = bmp.Bitmap.Width > image.Width || bmp.Bitmap.Height > image.Height
                                     ? PictureBoxSizeMode.Zoom
                                     : PictureBoxSizeMode.CenterImage;
                image.Image = bmp.Bitmap;
            }

            var format = item.Flags.HasFlag(TextureFlags.Missing) ? "Invalid texture" : "{0} x {1}";
            info.Text = string.Format(format, item.Width, item.Height);
        }
    }
}
