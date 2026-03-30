using UnityEngine;

namespace VATSystem.MaterialModules
{
    public class MaterialInstancePropertySetter : IMaterialPropertySetter
    {
        private readonly Renderer _renderer;
        private readonly Material _sourceMaterial;
        private readonly Material _material;

        public MaterialInstancePropertySetter(Renderer renderer)
        {
            _renderer = renderer;
            _sourceMaterial = _renderer.sharedMaterial;
            _material = _renderer.material;
            _renderer.sharedMaterial = _material;
        }

        public void Dispose()
        {
            Object.Destroy(_material);
            if (_renderer != null)
            {
                _renderer.sharedMaterial = _sourceMaterial;
            }
        }

        public void SetTexture(int nameID, Texture texture)
        {
            _material.SetTexture(nameID, texture);
        }

        public void SetFloat(int nameID, float value)
        {
            _material.SetFloat(nameID, value);
        }

        public void Apply()
        {
        }
    }
}
