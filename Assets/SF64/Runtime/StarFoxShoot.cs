using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moloch.Testing.SF64
{
    public class StarFoxShoot : MonoBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private StarFoxController controller;
        [SerializeField] private Transform spawnLeft;
        [SerializeField] private Transform spawnRight;

        [SerializeField] private float rate = 10f;
        [SerializeField] private float life = 2f;
        [SerializeField] private float speed = 100f;
        [SerializeField] private LayerMask layerMask;
        private float _next;
        private RaycastHit[] _results = new RaycastHit[4];

        private void Update()
        {
            if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed && Time.time > _next)
            {
                _next = Time.time + (1f / rate);
                var left = Instantiate(projectilePrefab, spawnLeft.position, spawnLeft.rotation);
                var right = Instantiate(projectilePrefab, spawnRight.position, spawnRight.rotation);
                Vector3 velocityLeft = controller.ShipVelocity + spawnLeft.forward * speed;
                Vector3 velocityRight = controller.ShipVelocity + spawnRight.forward * speed;
                DriveProjectileAsync(left, velocityLeft, life, gameObject.GetCancellationTokenOnDestroy()).Forget();
                DriveProjectileAsync(right, velocityRight, life, gameObject.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        private async UniTask DriveProjectileAsync
            (GameObject projectileInstance, Vector3 velocity, float projectileLife, CancellationToken ct)
        {
            Transform trs = projectileInstance.transform;
            float timeout = projectileLife + Time.time;
            while (timeout > Time.time)
            {
                if (ct.IsCancellationRequested)
                    break;
                var deltaPos = velocity * Time.deltaTime;
                trs.position += deltaPos;
                trs.forward = velocity;
                int collisionCount = Physics.RaycastNonAlloc(
                    trs.position,
                    trs.forward,
                    _results,
                    deltaPos.magnitude,
                    layerMask,
                    QueryTriggerInteraction.Collide
                );

                for (int i = 0; i < collisionCount; i++)
                {
                    if (_results[i].collider.TryGetComponent(out StarFoxDamageable foxDamageable))
                    {
                        foxDamageable.Hit();
                    }
                }

                await UniTask.NextFrame(PlayerLoopTiming.Update, ct);
            }

            Destroy(projectileInstance);
        }
    }
}