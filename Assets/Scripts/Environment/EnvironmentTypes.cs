public enum ActionType
{
    Up = 0,
    Right = 1,
    Down = 2,
    Left = 3
}

public struct StepResult
{
    public int previousState;
    public int action;
    public int nextState;
    public float reward;
    public bool isTerminal;

    public StepResult(
        int previousState,
        int action,
        int nextState,
        float reward,
        bool isTerminal
    )
    {
        this.previousState = previousState;
        this.action = action;
        this.nextState = nextState;
        this.reward = reward;
        this.isTerminal = isTerminal;
    }
}