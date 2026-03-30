using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                    AssetDatabase.SaveAssets();
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
                var keyframeCount = Mathf.FloorToInt(duration / (1f / _baker.FramesPerSecond));
                var dt = duration / keyframeCount;
                var width = CalculateQuadSideLength(_vertCount, keyframeCount);

                // Collect vertex and normals each keyframe
                _totalVertices.Clear();
                _totalNormals.Clear();
                for (var f = 0; f < keyframeCount; f++)
                {
                    clip.SampleAnimation(_gameObject, dt * f);
                    _baker.SkinnedMeshRenderer.BakeMesh(_mesh);
                    _frameVertices.Clear();
                    _mesh.GetVertices(_frameVertices);
                    foreach (var v in _frameVertices)
                    {
                        _totalVertices.Add(new MeshInfo { data = v });
                    }

                    if (_baker.BakeNormal)
                    {
                        _frameNormals.Clear();
                        _mesh.GetNormals(_frameNormals);
                        foreach (var n in _frameNormals)
                        {
                            _totalNormals.Add(new MeshInfo { data = n });
                        }
                    }
                }

                // Bake texture
                var vertTex = Render(_totalVertices, _baker.ComputeShader, width);
                var vertTexName = "VertexTexture";
                vertTex.name = vertTexName;

                Texture2D nrmTex = null;
                var nrmTexName = "NormalTexture";
                if (_baker.BakeNormal)
                {
                    nrmTex = Render(_totalNormals, _baker.ComputeShader, width);
                    nrmTex.name = nrmTexName;
                }

                // Create or overwrite asset
                var saveLocationPath = AssetDatabase.GetAssetPath(_baker.SaveLocation);
                var vatDataName = $"{_baker.Prefix}_{clip.name}.asset";
                var vatDataPath = Path.Combine(saveLocationPath, vatDataName);
                var vatData = AssetDatabase.LoadMainAssetAtPath(vatDataPath) as VATData;
                if (vatData == null)
                {
                    vatData = CreateInstance<VATData>();
                    AssetDatabase.CreateAsset(vatData, vatDataPath);
                }

                var subAssets = AssetDatabase.LoadAllAssetsAtPath(vatDataPath).Where(AssetDatabase.IsSubAsset).ToArray();
                var vertTexAsset = subAssets.FirstOrDefault(v => v.name == vertTexName) as Texture2D;
                if (vertTexAsset == null)
                {
                    AssetDatabase.AddObjectToAsset(vertTex, vatData);
                    vertTexAsset = vertTex;
                }
                else
                {
                    EditorUtility.CopySerialized(vertTex, vertTexAsset);
                    EditorUtility.SetDirty(vertTexAsset);
                }

                var nrmTexAsset = subAssets.FirstOrDefault(v => v.name == nrmTexName) as Texture2D;
                if (_baker.BakeNormal)
                {
                    if (nrmTexAsset == null)
                    {
                        AssetDatabase.AddObjectToAsset(nrmTex, vatData);
                        nrmTexAsset = nrmTex;
                    }
                    else
                    {
                        EditorUtility.CopySerialized(nrmTexAsset, nrmTexAsset);
                        EditorUtility.SetDirty(nrmTexAsset);
                    }
                }

                vatData.VertexTexture = vertTexAsset;
                vatData.NormalTexture = nrmTexAsset;
                vatData.VertexCount = _vertCount;
                vatData.KeyframeCount = keyframeCount;
                vatData.Duration = duration;
                vatData.Loop = clip.wrapMode == WrapMode.Loop;
                EditorUtility.SetDirty(vatData);
                Debug.Log($"{clip.name} baked to {vatDataPath}");
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
        }
    }
}
