using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;

    [Header("Recompensas")]
    [SerializeField] private float stepReward = -1f;
    [SerializeField] private float wallReward = -3f;
    [SerializeField] private float goalReward = 100f;
    [SerializeField] private float pitReward = -100f;

    [Header("Visualización")]
    [SerializeField] private float zOffset = -0.5f;

    private readonly HashSet<int> visitedStates = new HashSet<int>();

    public int CurrentState { get; private set; }
    public int Collisions { get; private set; }
    public int RepeatedVisits { get; private set; }

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogError("AgentController: falta GridManager.");
            enabled = false;
}
    }

    private void Start()
    {
        ResetAgent();
    }

    public void ResetAgent()
    {
        if (gridManager == null)
        {
            return;
        }

        MoveToState(gridManager.StartState);

        Collisions = 0;
        RepeatedVisits = 0;
        visitedStates.Clear();
        visitedStates.Add(CurrentState);
    }

    public void MoveToState(int state)
    {
        if (gridManager == null || !gridManager.IsTraversable(state))
        {
            return;
        }

        CurrentState = state;
        transform.position =
            gridManager.GetPosition(state) +
            new Vector3(0f, 0f, zOffset);
    }

    public StepResult ExecuteAction(ActionType action)
    {
        int previousState = CurrentState;

        if (
            !gridManager.TryGetNextState(
                previousState,
                (int)action,
                out int nextState
            )
        )
        {
            Collisions++;

            return new StepResult(
                previousState,
                (int)action,
                previousState,
                wallReward,
                false
            );
        }

        MoveToState(nextState);

        if (!visitedStates.Add(nextState))
        {
            RepeatedVisits++;
        }

        bool isTerminal =
            gridManager.IsTerminal(nextState);

        float reward = stepReward;

        if (gridManager.IsGoal(nextState))
        {
            reward = goalReward;
        }
        else if (gridManager.IsPit(nextState))
        {
            reward = pitReward;
        }

        return new StepResult(
            previousState,
            (int)action,
            nextState,
            reward,
            isTerminal
        );
    }

    public bool IsGoalReached()
    {
        return
            gridManager != null &&
            gridManager.IsTerminal(CurrentState);
    }
}
