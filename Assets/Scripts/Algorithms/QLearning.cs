using System.Collections.Generic;
using UnityEngine;

public class QLearning : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AgentController agentController;

    [Header("Hiperparámetros")]
    [SerializeField] private float alpha = 0.1f;
    [SerializeField] private float gamma = 0.99f;
    [SerializeField] private float epsilon = 1f;
    [SerializeField] private float minimumEpsilon = 0.05f;
    [SerializeField] private float epsilonDecay = 0.995f;

    private const int NumberOfActions = 4;

    private float[,] qTable;
    private float initialEpsilon;
    private readonly List<int> validActions =
        new List<int>(NumberOfActions);

    private void Awake()
    {
        initialEpsilon = epsilon;
        Initialize();
    }

    public void Initialize()
    {
        if (gridManager == null || agentController == null)
        {
            return;
        }

        qTable = new float[
            gridManager.TotalStates + 1,
            NumberOfActions
        ];
    }

    public EpisodeResult TrainEpisode(int maxSteps)
    {
        agentController.ResetAgent();

        float totalReward = 0f;
        int steps = 0;
        bool reachedGoal = false;

        for (int i = 0; i < maxSteps; i++)
        {
            int state = agentController.CurrentState;
            int action = SelectAction(state);

            StepResult result =
                agentController.ExecuteAction((ActionType)action);

            float target =
                result.reward +
                gamma * (
                    result.isTerminal
                        ? 0f
                        : GetMaximumQValue(result.nextState)
                );

            qTable[state, action] +=
                alpha * (target - qTable[state, action]);

            totalReward += result.reward;
            steps++;

            if (result.isTerminal)
            {
                reachedGoal = true;
                break;
            }
        }

        DecayEpsilon();

        return new EpisodeResult(
            totalReward,
            steps,
            reachedGoal
        );
    }

    public int GetGreedyAction(
        int state,
        bool randomTieBreak = true
    )
    {
        gridManager.GetValidActions(state, validActions);

        if (validActions.Count == 0)
        {
            return 0;
        }

        float bestValue = float.NegativeInfinity;
        List<int> bestActions = new List<int>();

        foreach (int action in validActions)
        {
            float value = qTable[state, action];

            if (value > bestValue)
            {
                bestValue = value;
                bestActions.Clear();
                bestActions.Add(action);
            }
            else if (Mathf.Approximately(value, bestValue))
            {
                bestActions.Add(action);
            }
        }

        if (!randomTieBreak)
        {
            return bestActions[0];
        }

        return bestActions[
            Random.Range(0, bestActions.Count)
        ];
    }

    public float GetQValue(int state, int action)
    {
        return qTable[state, action];
    }

    public void ResetLearning()
    {
        Initialize();
        epsilon = initialEpsilon;
    }

    private int SelectAction(int state)
    {
        gridManager.GetValidActions(state, validActions);

        if (validActions.Count == 0)
        {
            return 0;
        }

        if (Random.value < epsilon)
        {
            return validActions[
                Random.Range(0, validActions.Count)
            ];
        }

        return GetGreedyAction(state);
    }

    private float GetMaximumQValue(int state)
    {
        gridManager.GetValidActions(state, validActions);

        if (validActions.Count == 0)
        {
            return 0f;
        }

        float maxValue = qTable[state, validActions[0]];

        foreach (int action in validActions)
        {
            if (qTable[state, action] > maxValue)
            {
                maxValue = qTable[state, action];
            }
        }

        return maxValue;
    }

    private void DecayEpsilon()
    {
        epsilon = Mathf.Max(
            minimumEpsilon,
            epsilon * epsilonDecay
        );
    }
}
