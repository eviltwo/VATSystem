using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VATSystem.Editor
{
    [CustomEditor(typeof(VATBaker))]
    [CanEditMultipleObjects]
    public class VATBakerEditor : UnityEditor.Editor
    {
        private readonly List<VATBaker> _vatBakers = new();
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUILayout.Space();

            _vatBakers.Clear();
            foreach (var t in targets)
            {
                _vatBakers.Add(t as VATBaker);
            }

            var canBake = _vatBakers.All(CanBake);
            using (new EditorGUI.DisabledGroupScope(!canBake))
            {
                if (GUILayout.Button("Bake"))
                {
                    foreach (var t in targets)
                    {
                        Bake(t as VATBaker);
                    }
                    
                    AssetDatabase.Refresh();
                }
            }
        }

        private bool CanBake(VATBaker vatBaker)
        {
            if (vatBaker.SkinnedMeshRenderer == null) return false;
            if (vatBaker.AnimationClips.Length == 0) return false;
            if (vatBaker.SaveLocation == null) return false;
            if (string.IsNullOrEmpty(vatBaker.Prefix)) return false;
            return true;
        }

        private static void Bake(VATBaker vatBaker)
        {
            var baker = new BakerInternal(vatBaker);

            foreach (var clip in vatBaker.AnimationClips)
            {
                baker.Bake(clip);
            }
        }

        private static int CalculateQuadSideLength(int vertCount, int frameCount)
        {
            var totalPixel = vertCount * frameCount;
            var minSideLength = Mathf.CeilToInt(Mathf.Sqrt(totalPixel));
            return Mathf.NextPowerOfTwo(minSideLength);
        }

        private class BakerInternal
        {
            private readonly VATBaker _baker;
            private readonly GameObject _gameObject;
            private readonly int _vertCount;
            
            private readonly Mesh _mesh = new();
            private readonly List<MeshInfo> _totalVertices = new();
            private readonly List<Vector3> _frameVertices = new();
            private readonly List<MeshInfo> _totalNormals = new();
            private readonly List<Vector3> _frameNormals = new();
            
            public BakerInternal(VATBaker baker)
            {
                _baker = baker;
                _gameObject = baker.Animator == null ? baker.gameObject : baker.Animator.gameObject;
                _vertCount = baker.SkinnedMeshRenderer.sharedMesh.vertexCount;
            }

            public void Bake(AnimationClip clip)
            {
                var duration = clip.length;
                var dt = 1f / _baker.FramesPerSecond;
                var frameCount = Mathf.CeilToInt(duration / dt);
                var width = CalculateQuadSideLength(_vertCount, frameCount);
                Debug.Log($"{clip.name}: {duration:F2} s, {frameCount} f, {_vertCount} v, {width} px");

                _totalVertices.Clear();
                for (var f = 0; f < frameCount; f++)
                {
                    clip.SampleAnimation(_gameObject, dt * f);
                    _baker.SkinnedMeshRenderer.BakeMesh(_mesh);
                    _frameVertices.Clear();
                    _mesh.GetVertices(_frameVertices);
                    foreach (var v in _frameVertices)
                    {
                        _totalVertices.Add(new MeshInfo{data = v});
                    }
                }

                var tex = Render(_totalVertices, _baker.ComputeShader, width);
                _baker.ResultDebug = tex;
                tex.name = $"{_baker.Prefix}_{clip.name}";
                SaveTexture(tex, AssetDatabase.GetAssetPath(_baker.SaveLocation));
            }
            
            private static readonly int WidthProperty = Shader.PropertyToID("Width");
            private static readonly int HeightProperty = Shader.PropertyToID("Height");
            private static readonly int DataProperty = Shader.PropertyToID("MeshData");
            private static readonly int TextureProperty = Shader.PropertyToID("Result");

            private static Texture2D Render(List<MeshInfo> data, ComputeShader shader, int width)
            {
                var height = Mathf.CeilToInt((float)data.Count / width);
                var tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false, false);
                var desc = new RenderTextureDescriptor(width, height)
                {
                    enableRandomWrite = true,
                    graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                    sRGB = false
                };
                var rt = RenderTexture.GetTemporary(desc);
                rt.enableRandomWrite = true;
                
                var buffer = new ComputeBuffer(data.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshInfo)));
                buffer.SetData(data);
                var kernel = shader.FindKernel("CSMain");
                shader.SetInt(WidthProperty, width);
                shader.SetInt(HeightProperty, height);
                shader.SetBuffer(kernel, DataProperty, buffer);
                shader.SetTexture(kernel, TextureProperty, rt);
                shader.GetKernelThreadGroupSizes(kernel, out var threadsX, out var threadsY, out _);
                shader.Dispatch(kernel, Mathf.CeilToInt((float)width / threadsX), Mathf.CeilToInt((float)height / threadsY), 1);
                buffer.Release();
                
                var before = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                tex.Apply(false, false);
                RenderTexture.active = before;
                RenderTexture.ReleaseTemporary(rt);
                
                return tex;
            }

            private struct MeshInfo
            {
                public Vector4 data;
            }

            private void SaveTexture(Texture2D tex, string folderPath)
            {
                var filePath = Path.Combine(folderPath, tex.name + ".asset");
                AssetDatabase.CreateAsset(tex, filePath);
            }
        }
    }
}
