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

    [Header("Exploración")]
    [Tooltip("Probabilidad de que un episodio de entrenamiento " +
             "comience en una celda aleatoria (exploring starts). " +
             "Sin esto, Monte Carlo casi nunca encuentra la meta.")]
    [Range(0f, 1f)]
    [SerializeField] private float randomStartProbability = 0.5f;

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

        if (Random.value < randomStartProbability)
        {
            MoveToRandomState();
        }

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

    private void MoveToRandomState()
    {
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int state = Random.Range(1, gridManager.TotalStates + 1);

            if (
                gridManager.IsTraversable(state) &&
                !gridManager.IsTerminal(state)
            )
            {
                agentController.MoveToState(state);
                return;
            }
        }
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
        // Retorno G_t de cada paso, calculado de atrás hacia adelante.
        float[] returns = new float[episode.Count];
        float returnG = 0f;

        for (int i = episode.Count - 1; i >= 0; i--)
        {
            returnG = episode[i].reward + gamma * returnG;
            returns[i] = returnG;
        }

        // Primera visita: se recorre hacia adelante para que cada par
        // (estado, acción) se actualice con el retorno de su PRIMERA
        // aparición y no con el de la última.
        HashSet<long> updatedPairs = new HashSet<long>();

        for (int i = 0; i < episode.Count; i++)
        {
            EpisodeStep step = episode[i];

            long key =
                ((long)step.state * NumberOfActions) + step.action;

            if (!updatedPairs.Add(key))
            {
                continue;
            }

            visitCounts[step.state, step.action]++;

            qTable[step.state, step.action] +=
                (returns[i] - qTable[step.state, step.action]) /
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
