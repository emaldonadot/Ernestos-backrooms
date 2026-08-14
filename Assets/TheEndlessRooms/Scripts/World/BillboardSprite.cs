using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Rotates around the Y axis only to face the active camera, so a flat sprite quad
    /// (e.g. the Attendant's monster art) always reads as a consistent, upright silhouette
    /// instead of tilting into the floor/ceiling or facing away from the player.
    /// </summary>
    public sealed class BillboardSprite : MonoBehaviour
    {
        private Transform _cameraTransform;

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return;
                }

                _cameraTransform = mainCamera.transform;
            }

            Vector3 direction = transform.position - _cameraTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
