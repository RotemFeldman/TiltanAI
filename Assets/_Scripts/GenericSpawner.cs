using UnityEngine;
using System.Collections;
using System.Linq;

public class GenericSpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    public SpawnableItemSO[] spawnableItems;
    public float spawnInterval = 5f;
    public Transform[] spawnPoints;
    
    [Header("Spawn Settings")]
    public bool useWeightedSpawning = true;
    public bool spawnOnStart = true;
    
    private int totalWeight;
    
    private void Start()
    {
        if (spawnableItems == null || spawnableItems.Length == 0)
        {
            Debug.LogError("No spawnable items assigned!");
            return;
        }

        CalculateTotalWeight();
        
        if (spawnOnStart)
        {
            StartCoroutine(SpawnRoutine());
        }
    }
    
    private void CalculateTotalWeight()
    {
        totalWeight = spawnableItems.Sum(item => item.spawnWeight);
    }
    
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (useWeightedSpawning)
            {
                SpawnWeighted();
            }
            else
            {
                SpawnRandom();
            }
        }
    }
    
    private void SpawnWeighted()
    {
        int randomWeight = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (var item in spawnableItems)
        {
            currentWeight += item.spawnWeight;
            if (randomWeight < currentWeight && CountExistingItems(item) < item.maxActiveItems)
            {
                SpawnItem(item);
                break;
            }
        }
    }
    
    private void SpawnRandom()
    {
        var availableItems = spawnableItems
            .Where(item => CountExistingItems(item) < item.maxActiveItems)
            .ToArray();
            
        if (availableItems.Length > 0)
        {
            SpawnItem(availableItems[Random.Range(0, availableItems.Length)]);
        }
    }
    
    private int CountExistingItems(SpawnableItemSO item)
    {
        if (!string.IsNullOrEmpty(item.tag))
        {
            return GameObject.FindGameObjectsWithTag(item.tag).Length;
        }
        
        return GameObject.FindObjectsOfType<GameObject>()
            .Count(obj => obj.name == item.prefab.name || obj.name == item.prefab.name + "(Clone)");
    }
    
    private void SpawnItem(SpawnableItemSO item)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned!");
            return;
        }
        
        if (item.prefab == null)
        {
            Debug.LogError($"Prefab is not assigned in {item.name}!");
            return;
        }
        
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        Quaternion rotation = item.randomizeRotation ? 
            Quaternion.Euler(item.GetRandomRotation()) : 
            spawnPoint.rotation;
            
        Instantiate(item.prefab, spawnPoint.position, rotation);
    }
    
    public void StartSpawning()
    {
        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }
    
    public void StopSpawning()
    {
        StopAllCoroutines();
    }
}