using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior.Demo;
using UnityEngine;

namespace UtilityAI
{
    public class EvolutionManager : MonoBehaviour
    {
        
        [SerializeField] private Agent AgentPrefab;
        [SerializeField] private Transform AgentSpawnPoint;
        [SerializeField] private Agent BgentPrefab;
        [SerializeField] private Transform BgentSpawnPoint;
        
        public float mutationRate = 0.01f;
        public int populationSize = 10;
        public float TimePerGeneration = 25f;
        
        private List<Agent> Agents;
        private List<Agent> Bgents;

        private float time;
        private bool isTransitioning = false;

        private void Awake()
        {
            
            Agents = new List<Agent>();
            Bgents = new List<Agent>();

            SpawnAgents();
        }

        private void Start()
        {
            time = TimePerGeneration;
        }

        private void Update()
        {
            if (isTransitioning) return;

            time -= Time.deltaTime;
            if (time <= 0)
            {
                StartCoroutine(TransitionToNextGeneration());
            }
        }

        private IEnumerator TransitionToNextGeneration()
        {
            isTransitioning = true;

            // Sort both groups by health (fitness) - filter out null/destroyed agents first
            Agents.RemoveAll(agent => agent == null);
            Bgents.RemoveAll(agent => agent == null);

            Agents.Sort((a, b) => b.CurrentHealth.CompareTo(a.CurrentHealth));
            Bgents.Sort((a, b) => b.CurrentHealth.CompareTo(a.CurrentHealth));

            // Store top 5 agents from each group as DNA
            var topAgentsDNA = Agents.Take(5).Select(agent => new UtilityDNA(agent)).ToList();
            var topBgentsDNA = Bgents.Take(5).Select(agent => new UtilityDNA(agent)).ToList();

            // Disable all agents first to stop their AI systems
            foreach (var agent in Agents)
            {
                if (agent != null)
                {
                    agent.gameObject.SetActive(false);
                }
            }
            foreach (var agent in Bgents)
            {
                if (agent != null)
                {
                    agent.gameObject.SetActive(false);
                }
            }

            // Wait a frame to ensure all Update loops stop
            yield return null;

            // Now destroy all current agents
            foreach (var agent in Agents)
            {
                if (agent != null)
                {
                    Destroy(agent.gameObject);
                }
            }
            foreach (var agent in Bgents)
            {
                if (agent != null)
                {
                    Destroy(agent.gameObject);
                }
            }

            Agents.Clear();
            Bgents.Clear();

            // Wait another frame to ensure destruction is complete
            yield return null;

            // Create new generation - 2 offspring from each of the top 5
            for (int i = 0; i < 5; i++)
            {
                // Create 2 offspring from each parent
                for (int j = 0; j < 2; j++)
                {
                    SpawnAgentFromDNA(AgentPrefab, AgentSpawnPoint, AgentGroup.GroupA, topAgentsDNA[i]);
                    SpawnAgentFromDNA(BgentPrefab, BgentSpawnPoint, AgentGroup.GroupB, topBgentsDNA[i]);
                }
            }

            // Reset timer and allow updates to continue
            time = TimePerGeneration;
            isTransitioning = false;
        }

        public void RegisterAgent(Agent agent)
        {
            if (agent == null) return;

            if (agent.Group == AgentGroup.GroupA)
            {
                Agents.Add(agent);
            }
            else
            {
                Bgents.Add(agent);
            }
        }
        
        private void SpawnAgents()
        {
            for (int i = 0; i < populationSize; i++)
            {
                SpawnAgent(AgentPrefab, AgentSpawnPoint, AgentGroup.GroupA);
                SpawnAgent(BgentPrefab, BgentSpawnPoint, AgentGroup.GroupB);
            }
        }

        private void SpawnAgent(Agent prefab, Transform spawnPoint, AgentGroup group, Agent templateAgent = null)
        {
            Vector3 randomPosition = spawnPoint.position + new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                0,
                UnityEngine.Random.Range(-2f, 2f)
            );

            Agent agent = Instantiate(prefab, randomPosition, Quaternion.identity);
            agent.Group = group;

            if (templateAgent != null)
            {
                float healthModifier = 1f + UnityEngine.Random.Range(-mutationRate, mutationRate);
                agent.baseStats.maxHealth = templateAgent.baseStats.maxHealth * healthModifier;
            }

            RegisterAgent(agent);
        }

        private void SpawnAgentFromDNA(Agent prefab, Transform spawnPoint, AgentGroup group, UtilityDNA parentDNA)
        {
            Vector3 randomPosition = spawnPoint.position + new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                0,
                UnityEngine.Random.Range(-2f, 2f)
            );

            Agent agent = Instantiate(prefab, randomPosition, Quaternion.identity);
            agent.Group = group;

            // Apply parent stats with mutation
            ApplyDNAToAgent(agent, parentDNA, group);

            RegisterAgent(agent);
        }

        private void ApplyDNAToAgent(Agent agent, UtilityDNA dna, AgentGroup group)
        {
            // Apply base stats from parent DNA
            agent.baseStats.maxHealth = dna.maxHealth;
            agent.baseStats.baseDamage = dna.attackDamage;
            agent.baseStats.attackSpeed = dna.attackSpeed;
            agent.baseStats.moveSpeed = dna.moveSpeed;

            // Apply group-specific mutations
            if (group == AgentGroup.GroupA)
            {
                MutateGroupA(agent);
            }
            else if (group == AgentGroup.GroupB)
            {
                MutateGroupB(agent);
            }
        }

        private void MutateGroupA(Agent agent)
        {
            // Group A mutation logic - focus on health and regeneration
            agent.baseStats.maxHealth = MutateValue(agent.baseStats.maxHealth, mutationRate);
            agent.baseStats.healthRegenRate = MutateValue(agent.baseStats.healthRegenRate, mutationRate);
            agent.baseStats.baseDamage = MutateValue(agent.baseStats.baseDamage, mutationRate * 0.5f); // Less damage mutation
            agent.baseStats.attackSpeed = MutateValue(agent.baseStats.attackSpeed, mutationRate);
            agent.baseStats.moveSpeed = MutateValue(agent.baseStats.moveSpeed, mutationRate);
        }

        private void MutateGroupB(Agent agent)
        {
            // Group B mutation logic - focus on combat and speed
            agent.baseStats.maxHealth = MutateValue(agent.baseStats.maxHealth, mutationRate * 0.5f); // Less health mutation
            agent.baseStats.healthRegenRate = MutateValue(agent.baseStats.healthRegenRate, mutationRate * 0.5f);
            agent.baseStats.baseDamage = MutateValue(agent.baseStats.baseDamage, mutationRate); // More damage mutation
            agent.baseStats.attackSpeed = MutateValue(agent.baseStats.attackSpeed, mutationRate);
            agent.baseStats.moveSpeed = MutateValue(agent.baseStats.moveSpeed, mutationRate);
        }

        private float MutateValue(float originalValue, float mutationStrength)
        {
            float mutationFactor = 1f + UnityEngine.Random.Range(-mutationStrength, mutationStrength);
            return Mathf.Max(0.1f, originalValue * mutationFactor); // Ensure minimum value
        }
    }
}