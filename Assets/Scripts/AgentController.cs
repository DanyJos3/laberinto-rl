using UnityEngine;

public class AgentController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;

    [Header("Recompensas")]
    [SerializeField] private float stepReward = -1f;
    [SerializeField] private float wallReward = -3f;
    [SerializeField] private float goalReward = 100f;

    [Header("Visualización")]
    [SerializeField] private float zOffset = -0.5f;

    public int CurrentState { get; private set; }

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
            return new StepResult(
                previousState,
                (int)action,
                previousState,
                wallReward,
                false
            );
        }

        MoveToState(nextState);

        bool isTerminal =
            gridManager.IsTerminal(nextState);

        float reward =
            isTerminal
                ? goalReward
                : stepReward;

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
