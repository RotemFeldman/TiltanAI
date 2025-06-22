using UnityEngine;
using System.Collections;

public class HealthSpawner : MonoBehaviour
{
    public GameObject healthPrefab;
    public float spawnInterval = 5f;
    public Transform[] spawnPoints;
    public int maxHealthItems = 3;
    
    private float nextSpawnTime;
    
    private void Start()
    {
        // Start the spawning coroutine
        StartCoroutine(SpawnHealthRoutine());
    }
    
    private IEnumerator SpawnHealthRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            // Only spawn if we have fewer than maxHealthItems
            if (CountExistingHealthItems() < maxHealthItems)
            {
                SpawnHealth();
            }
        }
    }
    
    private int CountExistingHealthItems()
    {
        // Find all health prefabs in the scene
        // This assumes the prefab has a specific tag or component that identifies it
        // Option 1: If the health prefab has a specific tag
        if (healthPrefab.CompareTag("MedKit"))
        {
            return GameObject.FindGameObjectsWithTag("MedKit").Length;
        }
        
        // Option 2: If the health prefab has a specific component
        // Assuming it has a component called "HealthItem"
        // Replace "HealthItem" with the actual component name on your prefab
        var healthItems = FindObjectsOfType<HealthItem>();
        if (healthItems != null)
        {
            return healthItems.Length;
        }
        
        // If we can't determine the count by component or tag, count by name
        // This is less efficient but works as a fallback
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int count = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == healthPrefab.name || obj.name == healthPrefab.name + "(Clone)")
            {
                count++;
            }
        }
        
        return count;
    }
    
    private void SpawnHealth()
    {
        // Check if we have spawn points and a prefab
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned to HealthSpawner!");
            return;
        }
        
        if (healthPrefab == null)
        {
            Debug.LogError("Health prefab is not assigned to HealthSpawner!");
            return;
        }
        
        // Select a random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // Instantiate the health prefab at the spawn point position and rotation
        Instantiate(healthPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}