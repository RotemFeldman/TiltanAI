using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public class BeliefFactory
    {
        readonly GoapAgent agent;
        private readonly Dictionary<string, AgentBelief> beliefs;

        public BeliefFactory(GoapAgent agent, Dictionary<string, AgentBelief> beliefs)
        {
            this.agent = agent;
            this.beliefs = beliefs;
        }

        public void AddBelief(string key, Func<bool> condition)
        {
            beliefs.Add(key, new AgentBelief.Builder(key)
                .WithCondition(condition)
                .Build());
        }

        public void AddSensorBelief(string key, Sensor sensor)
        {
            beliefs.Add(key, new AgentBelief.Builder(key)
                .WithCondition(() => sensor.IsTargetInRange)
                .WithLocation(() => sensor.TargetPosition)
                .Build());
        }

        public void AddLocationBelief(string key, float distance, Transform locationCondition)
        {
            AddLocationBelief(key, distance, locationCondition.position);
        }

        public void AddLocationBelief(string key, float distance, Vector3 locationCondition)
        {
            beliefs.Add(key, new AgentBelief.Builder(key)
                .WithCondition(() => InRangeOf(locationCondition, distance))
                .WithLocation(() => locationCondition)
                .Build());
        }

        bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(agent.transform.position, pos) < range;
    }

    public class AgentBelief
    {
        public string Name { get; private set; }

        private Func<bool> condition = () => false;
        Func<Vector3> observedLocation = () => Vector3.zero;

        public Vector3 Location
        {
            get
            {
                try { return observedLocation(); }
                catch (MissingReferenceException) { return Vector3.zero; }
                catch (NullReferenceException) { return Vector3.zero; }
            }
        }

        // Hardened: no exceptions bubble up from beliefs
        public bool Evaluate()
        {
            try { return condition(); }
            catch (MissingReferenceException) { return false; }
            catch (NullReferenceException) { return false; }
        }

        public class Builder
        {
            readonly AgentBelief belief;

            public Builder(string name)
            {
                belief = new AgentBelief();
                belief.Name = name;
            }

            public Builder WithCondition(Func<bool> condition)
            {
                belief.condition = condition;
                return this;
            }

            public Builder WithLocation(Func<Vector3> location)
            {
                belief.observedLocation = location;
                return this;
            }

            public AgentBelief Build() => belief;
        }
    }
}
