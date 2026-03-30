using UnityEngine;
using VATSystem.MaterialModules;

namespace VATSystem
{
    public class VATPlayer : MonoBehaviour
    {
        private static readonly int VertTexProperty = Shader.PropertyToID("_VAT_VertexTexture");
        private static readonly int NormalTexProperty = Shader.PropertyToID("_VAT_NormalTexture");
        private static readonly int TextureWidthProperty = Shader.PropertyToID("_VAT_TextureWidth");
        private static readonly int TextureHeightProperty = Shader.PropertyToID("_VAT_TextureHeight");
        private static readonly int VertexCountProperty = Shader.PropertyToID("_VAT_VertexCount");
        private static readonly int KeyframeCountProperty = Shader.PropertyToID("_VAT_KeyframeCount");
        private static readonly int DurationProperty = Shader.PropertyToID("_VAT_Duration");
        private static readonly int LoopProperty = Shader.PropertyToID("_VAT_Loop");
        private static readonly int SpeedProperty = Shader.PropertyToID("_VAT_Speed");
        private static readonly int StartTimeProperty = Shader.PropertyToID("_VAT_StartTime");

        [SerializeField]
        private MeshRenderer _meshRenderer;

        public MeshRenderer MeshRenderer
        {
            get => _meshRenderer;
            set
            {
                _meshRenderer = value;
                _materialPropertySetter?.Dispose();
                _materialPropertySetter = null;
            }
        }

        [SerializeField]
        private VATData _data;

        public VATData Data
        {
            get => _data;
            set
            {
                _data = value;
                StopOrPlay(_data);
            }
        }

        public bool PlayOnStart;

        private VATData _playingData;

        private IMaterialPropertySetter _materialPropertySetter;

        private void Reset()
        {
            MeshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        private void OnDestroy()
        {
            _materialPropertySetter?.Dispose();
            _materialPropertySetter = null;
        }

        private void Start()
        {
            if (PlayOnStart && _data != null)
            {
                Play(_data);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            StopOrPlay(_data);
        }

#if UNITY_EDITOR
        public void Refresh()
        {
            StopOrPlay(_data);
        }
#endif

        public void Play(VATData vatData, float speed = 1, VATPlaybackMode playbackMode = VATPlaybackMode.UseData)
        {
            if (MeshRenderer == null) return;
            if (vatData == null) return;

            _data = vatData;
            _playingData = vatData;

            CreateMaterialPropertySetterIfNeeded();
            var loop = playbackMode == VATPlaybackMode.UseData ? _playingData.Loop : playbackMode == VATPlaybackMode.Loop;
            _materialPropertySetter.SetTexture(VertTexProperty, _data.VertexTexture);
            _materialPropertySetter.SetTexture(NormalTexProperty, _data.NormalTexture);
            _materialPropertySetter.SetFloat(TextureWidthProperty, _data.VertexTexture.width);
            _materialPropertySetter.SetFloat(TextureHeightProperty, _data.VertexTexture.height);
            _materialPropertySetter.SetFloat(VertexCountProperty, _playingData.VertexCount);
            _materialPropertySetter.SetFloat(KeyframeCountProperty, _playingData.KeyframeCount);
            _materialPropertySetter.SetFloat(DurationProperty, _playingData.Duration);
            _materialPropertySetter.SetFloat(LoopProperty, loop ? 1 : 0);
            _materialPropertySetter.SetFloat(SpeedProperty, speed);
            _materialPropertySetter.SetFloat(StartTimeProperty, Time.time);
            _materialPropertySetter.Apply();
        }

        public void Stop()
        {
            if (MeshRenderer == null) return;

            _playingData = null;

            CreateMaterialPropertySetterIfNeeded();
            _materialPropertySetter.SetFloat(SpeedProperty, 0);
            _materialPropertySetter.Apply();
        }

        public void CreateMaterialPropertySetterIfNeeded()
        {
            if (_materialPropertySetter == null)
            {
                if (Application.isPlaying)
                {
                    _materialPropertySetter = new MaterialInstancePropertySetter(MeshRenderer);
                }
                else
                {
                    _materialPropertySetter = new MaterialPropertyBlockSetter(MeshRenderer);
                }
            }
        }

        private void StopOrPlay(VATData data)
        {
            if (data == null)
            {
                Stop();
            }
            else
            {
                Play(data);
            }
        }
    }
}
