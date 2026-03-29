using System;
using UnityEditor;
using UnityEngine;

namespace VATSystem
{
    public class VATBaker : MonoBehaviour
    {
        public SkinnedMeshRenderer SkinnedMeshRenderer;

        public Animator Animator;

        public AnimationClip[] AnimationClips;

        [Min(1)]
        public int FramesPerSecond = 30;

        public bool BakeNormal = true;

        public string Prefix;

#if UNITY_EDITOR
        public DefaultAsset SaveLocation;
#endif

        public ComputeShader ComputeShader;

        public Texture2D ResultDebug;

        private void Reset()
        {
            SkinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            Animator = GetComponentInChildren<Animator>();
            if (Animator != null)
            {
                AnimationClips = Animator.runtimeAnimatorController.animationClips;
            }

            if (SkinnedMeshRenderer != null && SkinnedMeshRenderer.sharedMesh != null)
            {
                Prefix = SkinnedMeshRenderer.sharedMesh.name;
            }
        }
    }
}
