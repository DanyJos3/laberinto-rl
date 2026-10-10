using System.Collections.Generic;
using UnityEngine;

public class MonteCarlo : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AgentController agentController;

    [Header("Hiperparámetros")]
    [SerializeField] private float gamma = 0.99f;
    [SerializeField] private float epsilon = 1f;
    [SerializeField] private float minimumEpsilon = 0.05f;
    [SerializeField] private float epsilonDecay = 0.995f;

    private const int NumberOfActions = 4;

    private float[,] qTable;
    private int[,] visitCounts;
    private float initialEpsilon;
    private readonly List<int> validActions =
        new List<int>(NumberOfActions);

    private struct EpisodeStep
    {
        public int state;
        public int action;
        public float reward;

        public EpisodeStep(int state, int action, float reward)
        {
            this.state = state;
            this.action = action;
            this.reward = reward;
        }
    }

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

        visitCounts = new int[
            gridManager.TotalStates + 1,
            NumberOfActions
        ];
    }

    public EpisodeResult TrainEpisode(int maxSteps)
    {
        agentController.ResetAgent();

        List<EpisodeStep> episode = new List<EpisodeStep>();
        float totalReward = 0f;
        int steps = 0;
        bool reachedGoal = false;

        for (int i = 0; i < maxSteps; i++)
        {
            int state = agentController.CurrentState;
            int action = SelectAction(state);

            StepResult result =
                agentController.ExecuteAction((ActionType)action);

            episode.Add(
                new EpisodeStep(state, action, result.reward)
            );

            totalReward += result.reward;
            steps++;

            if (result.isTerminal)
            {
                reachedGoal = true;
                break;
            }
        }

        UpdateQValues(episode);
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

    private void UpdateQValues(List<EpisodeStep> episode)
    {
        float returnG = 0f;
        HashSet<long> updatedPairs = new HashSet<long>();

        for (int i = episode.Count - 1; i >= 0; i--)
        {
            EpisodeStep step = episode[i];

            returnG = step.reward + gamma * returnG;

            long key =
                ((long)step.state * NumberOfActions) + step.action;

            if (!updatedPairs.Add(key))
            {
                continue;
            }

            visitCounts[step.state, step.action]++;

            qTable[step.state, step.action] +=
                (returnG - qTable[step.state, step.action]) /
                visitCounts[step.state, step.action];
        }
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

    private void DecayEpsilon()
    {
        epsilon = Mathf.Max(
            minimumEpsilon,
            epsilon * epsilonDecay
        );
    }
}
