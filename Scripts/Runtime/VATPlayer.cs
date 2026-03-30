using UnityEngine;

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

        public MeshRenderer MeshRenderer;

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

        private MaterialPropertyBlock _mpb;

        private void Reset()
        {
            MeshRenderer = GetComponentInChildren<MeshRenderer>();
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

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            var loop = playbackMode == VATPlaybackMode.UseData ? _playingData.Loop : playbackMode == VATPlaybackMode.Loop;
            MeshRenderer.GetPropertyBlock(_mpb);
            _mpb.SetTexture(VertTexProperty, _playingData.VertexTexture);
            _mpb.SetTexture(NormalTexProperty, _playingData.NormalTexture);
            _mpb.SetFloat(TextureWidthProperty, _data.VertexTexture.width);
            _mpb.SetFloat(TextureHeightProperty, _data.VertexTexture.height);
            _mpb.SetFloat(VertexCountProperty, _playingData.VertexCount);
            _mpb.SetFloat(KeyframeCountProperty, _playingData.KeyframeCount);
            _mpb.SetFloat(DurationProperty, _playingData.Duration);
            _mpb.SetFloat(LoopProperty, loop ? 1 : 0);
            _mpb.SetFloat(SpeedProperty, speed);
            _mpb.SetFloat(StartTimeProperty, Time.time);
            MeshRenderer.SetPropertyBlock(_mpb);
        }

        public void Stop()
        {
            if (MeshRenderer == null) return;

            _playingData = null;

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            MeshRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(SpeedProperty, 0);
            MeshRenderer.SetPropertyBlock(_mpb);
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

    public enum VATPlaybackMode
    {
        UseData,
        Clamp,
        Loop,
    }
}
