using System;
using UnityEngine;

namespace VATSystem.MaterialModules
{
    public interface IMaterialPropertySetter : IDisposable
    {
        void SetTexture(int nameID, Texture texture);

        void SetFloat(int nameID, float value);

        void Apply();
    }
}
