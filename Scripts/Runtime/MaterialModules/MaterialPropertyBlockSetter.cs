using UnityEngine;

namespace VATSystem.MaterialModules
{
    public class MaterialPropertyBlockSetter : IMaterialPropertySetter
    {
        private readonly Renderer _renderer;

        private readonly MaterialPropertyBlock _mpb;

        public MaterialPropertyBlockSetter(Renderer renderer)
        {
            _renderer = renderer;
            _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
        }

        public void Dispose()
        {
            _renderer.SetPropertyBlock(null);
        }

        public void SetTexture(int nameID, Texture texture)
        {
            _mpb.SetTexture(nameID, texture);
        }

        public void SetFloat(int nameID, float value)
        {
            _mpb.SetFloat(nameID, value);
        }

        public void Apply()
        {
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
