using UnityEngine;

public class BrainLoader : MonoBehaviour
{
    public TextAsset[] brainJsons;  // drag JSONs saved from training
    public EnemyBrain enemyPrefab;
    public Transform[] spawnPoints;

    void Start()
    {
        for (int i=0; i<spawnPoints.Length; i++)
        {
            var brain = JsonUtility.FromJson<Genome>(brainJsons[i % brainJsons.Length].text);
            var e = Instantiate(enemyPrefab, spawnPoints[i].position, Quaternion.identity);
            e.LoadGenome(brain);
        }
    }
}