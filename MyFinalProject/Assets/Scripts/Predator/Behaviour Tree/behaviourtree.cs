using System.Collections.Generic;
using UnityEngine;


// ==================================
//      Behaviour Tree FrameWork
// ==================================


// nodes return to one of these states
public enum NodeState { Running, Success, Failure }

public abstract class BTNode
{
    protected NodeState state;

    public abstract NodeState Evalute();

    public NodeState State => state;
}


// ==================================
//          Composite Nodes
// ==================================

// sequence runs children left to right - fails when any child fail, succeeds when all children succeed.
// mirror and logic
public class BTSequence : BTNode
{
    private List<BTNode> children;

    public BTSequence(List<BTNode> children)
    {
        this.children = children;
    }

    public override NodeState Evalute()
    {
        foreach (var child in children)
        {
            NodeState result = child.Evalute();
            if (result == NodeState.Failure) { state = NodeState.Failure; return state; }
            if (result == NodeState.Running) { state = NodeState.Running; return state; }
        }
        state = NodeState.Success;
        return state;
    }
}

// selector runs children left to right - succeds when any child succeds, fails when all children fail.
// mirror or logic
public class BTSelector : BTNode
{
    private List<BTNode> children;

    public BTSelector(List<BTNode> children)
    {
        this.children = children;
    }

    public override NodeState Evalute()
    {
        foreach (var child in children)
        {
            NodeState result = child.Evalute();
            if (result == NodeState.Success) { state = NodeState.Success; return state; }
            if (result == NodeState.Running) { state = NodeState.Running; return state; }
        }
        state = NodeState.Failure;
        return state;
    }
}

// ==================================
//             Leaf Nodes
// ==================================

// action leaf - does sometjing and returns a state
public class BTAction : BTNode
{
    private System.Func<NodeState> action;

    public BTAction(System.Func<NodeState> action)
    {
        this.action = action;
    }

    public override NodeState Evalute()
    {
        state = action.Invoke();
        return state;
    }
}

// condition leaf - tests  something and returns as success or failure
public class BTCondition : BTNode
{
    private System.Func<bool> condition;

    public BTCondition(System.Func<bool> condition)
    {
        this.condition = condition;
    }

    public override NodeState Evalute()
    {
        state = condition.Invoke() ? NodeState.Success : NodeState.Failure;
        return state;
    }
}

// ==================================
//             Decorators
// ==================================

// inverter - flips success and failure , passes through running
public class BTInverter : BTNode
{
    private BTNode child;

    public BTInverter(BTNode child)
    {
        this.child = child;
    }

    public override NodeState Evalute()
    {
        NodeState result = child.Evalute();
        if (result == NodeState.Success) state = NodeState.Failure;
        else if (result == NodeState.Failure) state = NodeState.Success;
        else state = NodeState.Running;
        return state;
    }
}

// always succeed regardless of child result
public class BTAlwaysSucceed : BTNode
{
    private BTNode child;

    public BTAlwaysSucceed(BTNode child)
    {
        this.child = child;
    }
    public override NodeState Evalute()
    {
        child.Evalute();
        state = NodeState.Success;
        return state;
    }
}