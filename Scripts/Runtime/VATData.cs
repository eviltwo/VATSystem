using UnityEngine;

namespace VATSystem
{
    public class VATData : ScriptableObject
    {
        public Texture2D VertexTexture;

        public Texture2D NormalTexture;

        public int VertexCount;

        public int KeyframeCount;
        
        public float Duration;

        public bool Loop;
    }
}
