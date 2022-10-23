using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace CBRE.Graphics {
    public class Effect : GraphicsResource {
        internal VertexFragmentCompilationResult shaderCompilerResult = null;
        internal Shader[] shaders = null;
        internal Pipeline _pipeline = null;
        internal List<ResourceLayout> _layouts = new List<ResourceLayout>();
        internal Dictionary<uint, ResourceSet> _sets = new Dictionary<uint, ResourceSet>();
        public Dictionary<string, EffectParameter> Parameters { get; private set; } = new Dictionary<string, EffectParameter>();
        public RgbaFloat _prevFactor;
        public BlendState _prevState;
        public RgbaFloat BlendFactor { get; set; } = RgbaFloat.White;
        public BlendState BlendState { get; set; } = BlendState.Opaque;
        private string _name;

        public Effect(string path) {
            _name = path;
            string vertexCode = File.ReadAllText($"{path}.vert");
            string fragmentCode = File.ReadAllText($"{path}.frag");

            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(vertexCode),
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragmentCode),
                "main");
            shaders = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(GlobalGraphics.GraphicsDevice.BackendType == GraphicsBackend.OpenGL, GlobalGraphics.GraphicsDevice.BackendType == GraphicsBackend.Vulkan));
            shaderCompilerResult = SpirvCompilation.CompileVertexFragment(vertexShaderDesc.ShaderBytes, fragmentShaderDesc.ShaderBytes, CrossCompileTarget.GLSL);

            for (int i = 0; i < shaderCompilerResult.Reflection.ResourceLayouts.Length; i++) {
                var layout = shaderCompilerResult.Reflection.ResourceLayouts[i];
                for (int j = 0; j < layout.Elements.Length; j++) {
                    Parameters.Add(layout.Elements[j].Name, new EffectParameter());
                }
            }
        }

        public void NewPipeline() {
            _pipeline?.Dispose();
            for (int i = 0; i < _layouts.Count; i++) {
                _layouts[i]?.Dispose();
            }
            _layouts.Clear();
            for (int i = 0; i < shaderCompilerResult.Reflection.ResourceLayouts.Length; i++) {
                var layout = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateResourceLayout(shaderCompilerResult.Reflection.ResourceLayouts[i]);
                _layouts.Add(layout);
            }
            GraphicsPipelineDescription desc = new GraphicsPipelineDescription();
            BlendAttachmentDescription blendAttachment = BlendAttachmentDescription.Disabled;
            switch (BlendState) {
                case BlendState.NonPremultiplied:
                    blendAttachment = BlendAttachmentDescription.AlphaBlend;
                    break;
                case BlendState.Additive:
                    blendAttachment = BlendAttachmentDescription.AdditiveBlend;
                    break;
            }
            desc.BlendState = new BlendStateDescription(BlendFactor, blendAttachment);
            desc.DepthStencilState = DepthStencilStateDescription.Disabled;
            desc.RasterizerState = RasterizerStateDescription.Default;
            desc.PrimitiveTopology = PrimitiveTopology.TriangleList;
            desc.ResourceLayouts = _layouts.ToArray();
            desc.ShaderSet = new ShaderSetDescription(new[] { new VertexLayoutDescription(shaderCompilerResult.Reflection.VertexElements) }, shaders);
            desc.Outputs = GlobalGraphics.ActiveFramebuffer.OutputDescription;
            _pipeline = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(desc);
            _pipeline.Name = _name;
        }

        public bool IsDirty() {
            return BlendFactor != _prevFactor || BlendState != _prevState;
        }

        public void Apply() {
            if (_pipeline == null || IsDirty()) {
                NewPipeline();
            }
            GlobalGraphics.CommandList.SetPipeline(_pipeline);
            Dictionary<uint, List<BindableResource>> resources = new Dictionary<uint, List<BindableResource>>();
            int idx = -1;
            foreach (var item in Parameters) {
                idx++;
                if (item.Value.IsDirty) {
                    if (!resources.ContainsKey((uint)idx))
                        resources.Add((uint)idx, new List<BindableResource>());
                    resources[(uint)idx].AddRange(item.Value.Bindables);
                }
            }
            foreach (var item in resources) {
                if (_sets.ContainsKey(item.Key)) {
                    _sets[item.Key]?.Dispose();
                } else {
                    _sets.Add(item.Key, null);
                }
                _sets[item.Key] = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_layouts[(int)item.Key], item.Value.ToArray()));
            }
            foreach (var item in _sets) {
                GlobalGraphics.CommandList.SetGraphicsResourceSet(item.Key, item.Value);
            }
        }

        public override void Dispose() {
        }
    }

    public enum BlendState : byte {
        Opaque,
        NonPremultiplied, // i have no idea if im using this correctly lol
        Additive,
    }
}