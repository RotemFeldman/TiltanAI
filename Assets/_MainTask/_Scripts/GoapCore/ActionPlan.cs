using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GOAP
{
    public interface IGoapPlanner {
        ActionPlan Plan(GoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null);
    }
    
    public class GoapPlanner : IGoapPlanner {
        public ActionPlan Plan(GoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null) {
            // Order goals by priority, descending
            List<AgentGoal> orderedGoals = goals
                .Where(g => g.DesiredEffects.Any(b => !b.Evaluate()))
                .OrderByDescending(g => g == mostRecentGoal ? g.Priority - 0.01 : g.Priority)
                .ToList();
            
            // Try to solve each goal in order
            foreach (var goal in orderedGoals) {
                Node goalNode = new Node(null, null, goal.DesiredEffects, 0);
                
                // If we can find a path to the goal, return the plan
                if (FindPath(goalNode, agent.actions)) {
                    // If the goalNode has no leaves and no action to perform try a different goal
                    if (goalNode.IsLeafDead) continue;
                    
                    Stack<AgentAction> actionStack = new Stack<AgentAction>();
                    while (goalNode.Leaves.Count > 0) {
                        var cheapestLeaf = goalNode.Leaves.OrderBy(leaf => leaf.Cost).First();
                        goalNode = cheapestLeaf;
                        actionStack.Push(cheapestLeaf.Action);
                    }
                    
                    return new ActionPlan(goal, actionStack, goalNode.Cost);
                }
            }
            
            Debug.LogWarning("No plan found");
            return null;
        }
    
        // TODO: Consider a more powerful search algorithm like A* or D*
        bool FindPath(Node parent, HashSet<AgentAction> actions) {
            // Order actions by cost, ascending
            var orderedActions = actions.OrderBy(a => a.Cost);
            
            foreach (var action in orderedActions) {
                var requiredEffects = new HashSet<AgentBelief>(parent.RequiredEffects);
                
                // Remove any effects that evaluate to true, there is no action to take
                requiredEffects.RemoveWhere(b => b.Evaluate());
                
                // If there are no required effects to fulfill, we have a plan
                if (requiredEffects.Count == 0) {
                    return true;
                }

                // Check if this action can help with any required effects
                if (action.Effects.Any(requiredEffects.Contains)) {
                    var newRequiredEffects = new HashSet<AgentBelief>(requiredEffects);
                    
                    // IMPORTANT: Remove effects that this action claims to produce
                    // even if they can't actually be satisfied (for fallback actions)
                    newRequiredEffects.ExceptWith(action.Effects);
                    newRequiredEffects.UnionWith(action.Preconditions);
                    
                    var newAvailableActions = new HashSet<AgentAction>(actions);
                    newAvailableActions.Remove(action);
                    
                    var newNode = new Node(parent, action, newRequiredEffects, parent.Cost + action.Cost);
                    
                    // Check if all preconditions are satisfied for this action
                    bool canPerformAction = action.Preconditions.All(p => p.Evaluate());
                    
                    // If this action can be performed and solves our requirements, accept it
                    if (canPerformAction && newRequiredEffects.Count == 0) {
                        parent.Leaves.Add(newNode);
                        return true;
                    }
                    
                    // Otherwise, explore deeper if possible
                    if (FindPath(newNode, newAvailableActions)) {
                        parent.Leaves.Add(newNode);
                    }
                }
            }
            
            return parent.Leaves.Count > 0;
        }
    }
    
    public class Node {
        public Node Parent { get; }
        public AgentAction Action { get; }
        public HashSet<AgentBelief> RequiredEffects { get; }
        public List<Node> Leaves { get; }
        public float Cost { get; }
        
        public bool IsLeafDead => Leaves.Count == 0 && Action == null;
        
        public Node(Node parent, AgentAction action, HashSet<AgentBelief> effects, float cost) {
            Parent = parent;
            Action = action;
            RequiredEffects = new HashSet<AgentBelief>(effects);
            Leaves = new List<Node>();
            Cost = cost;
        }
    }
    
    public class ActionPlan {
        public AgentGoal AgentGoal { get; }
        public Stack<AgentAction> Actions { get; }
        public float TotalCost { get; set; }
        
        public ActionPlan(AgentGoal goal, Stack<AgentAction> actions, float totalCost) {
            AgentGoal = goal;
            Actions = actions;
            TotalCost = totalCost;
        }
    }
}