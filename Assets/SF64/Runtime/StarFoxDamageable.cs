using UnityEngine;

namespace Moloch.Testing.SF64
{
    public class StarFoxDamageable : MonoBehaviour
    {
        [SerializeField] private Material hitMaterial;
        [SerializeField] private int frameCount = 5;
        [SerializeField] private int maxFrameCount = 10;

        

        private Material _originalMaterial;
        private MeshRenderer _renderer;
        private int _hitFrameCount;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _originalMaterial = _renderer.sharedMaterial;
        }

        public void Hit()
        {
            _hitFrameCount = Mathf.Max(_hitFrameCount + frameCount, 0, maxFrameCount); 
            _renderer.sharedMaterial = hitMaterial;
        }

        private void Update()
        {
            _hitFrameCount--;
            if (_hitFrameCount <= 0)
            {
                
                _renderer.sharedMaterial = _originalMaterial;
            }

        }
    }
}