using UnityEngine;
using Random = UnityEngine.Random;

namespace Moloch.Testing.SF64
{
    public class StarFoxObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private Vector2 lateralMinMax;
        [SerializeField] private Vector2 verticalMinMax;
        [SerializeField] private Vector2 intervalMinMax;
        [SerializeField] private Vector2 distanceMinMax;
        [SerializeField] private Vector2 scaleMinMax;
        [SerializeField] private GameObject[] prefabs;

        private void Start()
        {
            float offsetFromStart = distanceMinMax.x;
            for (int i = 0; i < 1000; i++)
            {
                if (offsetFromStart > distanceMinMax.y)
                    break;
                offsetFromStart += Random.Range(intervalMinMax.x, intervalMinMax.y);
                var rngPrefab = prefabs[Random.Range(0, prefabs.Length)];
                var instance = Instantiate(
                    rngPrefab,
                    position: new Vector3(
                        Random.Range(lateralMinMax.x, lateralMinMax.y),
                        Random.Range(verticalMinMax.x, verticalMinMax.y),
                        offsetFromStart
                    ),
                    rotation: Quaternion.Euler(0, Random.Range(-45f, 45f), 0));
                var scale = Random.Range(scaleMinMax.x, scaleMinMax.y);
                instance.transform.localScale = new Vector3(scale, scale, scale);
                instance.name = $"{rngPrefab.name} {instance.transform.position}";
            }
        }
    }
}