using UnityEngine;

[CreateAssetMenu(fileName = "New Spawnable Item", menuName = "Spawning/Spawnable Item")]
public class SpawnableItemSO : ScriptableObject
{
    [Header("Prefab Settings")]
    public GameObject prefab;
    public string tag;
    
    [Header("Spawn Settings")]
    [Min(1)]
    public int maxActiveItems = 3;
    [Tooltip("Weight affects the probability of this item being chosen when spawning")]
    [Range(1, 100)]
    public int spawnWeight = 1;
    
    [Header("Optional Settings")]
    [Tooltip("If true, the item will rotate randomly when spawned")]
    public bool randomizeRotation;
    [Tooltip("Random rotation will be applied only on these axes")]
    public bool rotateX, rotateY, rotateZ;
    
    public Vector3 GetRandomRotation()
    {
        return new Vector3(
            rotateX ? Random.Range(0f, 360f) : 0f,
            rotateY ? Random.Range(0f, 360f) : 0f,
            rotateZ ? Random.Range(0f, 360f) : 0f
        );
    }
}