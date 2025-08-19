using UnityEngine;

namespace Utils
{
    public class LookAtCamera : MonoBehaviour
    {
        private static Camera _mainCamera;

        private void Awake()
        {
            if (_mainCamera) return;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!_mainCamera) return;
            
            transform.LookAt(_mainCamera.transform.position, Vector3.up);
                
            transform.Rotate(0, 180, 0);
        }
    }
}