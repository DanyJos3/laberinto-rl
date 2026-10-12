using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EpisodeManager : MonoBehaviour
{
    public enum AlgorithmType
    {
        MonteCarlo,
        Sarsa,
        QLearning
    }

    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AgentController agentController;
    [SerializeField] private MonteCarlo monteCarlo;
    [SerializeField] private Sarsa sarsa;
    [SerializeField] private QLearning qLearning;

    [Header("Entrenamiento")]
    [SerializeField] private AlgorithmType selectedAlgorithm =
        AlgorithmType.QLearning;
    [SerializeField] private int totalEpisodes = 5000;
    [SerializeField] private int maxStepsPerEpisode = 2000;
    [SerializeField] private bool trainOnStart;
    [SerializeField] private bool showPolicyAfterTraining = true;

    [Header("Evaluación")]
    [SerializeField] private int evaluationEpisodes = 100;
    [SerializeField] private int evaluationMaxSteps = 500;
    [Range(0f, 1f)]
    [SerializeField] private float evaluationEpsilon = 0.05f;

    [Header("Demostración")]
    [SerializeField] private float movementDelay = 0.15f;
    [SerializeField] private int maxDemonstrationSteps = 500;

    [Header("Estado")]
    [SerializeField] private int currentEpisode;
    [SerializeField] private int successfulEpisodes;
    [SerializeField] private float currentReward;
    [SerializeField] private int currentSteps;

    private int trainingPits;

    private Coroutine trainingCoroutine;
    private Coroutine demonstrationCoroutine;

    private readonly List<float> rewardHistory =
        new List<float>();

    private readonly List<int> stepHistory =
        new List<int>();

    public IReadOnlyList<float> RewardHistory => rewardHistory;
    public IReadOnlyList<int> StepHistory => stepHistory;

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        agentController.ResetAgent();

        if (trainOnStart)
        {
            StartTraining();
        }
    }

    public void StartTraining()
    {
        if (trainingCoroutine != null)
        {
            return;
        }

        StopDemonstration();
        ResetStatistics();
        ResetSelectedAlgorithm();

        trainingCoroutine =
            StartCoroutine(TrainingRoutine());
    }

    public void StopTraining()
    {
        if (trainingCoroutine != null)
        {
            StopCoroutine(trainingCoroutine);
            trainingCoroutine = null;
        }
    }

    public void StartPolicyDemonstration()
    {
        if (demonstrationCoroutine != null)
        {
            return;
        }

        StopTraining();
        demonstrationCoroutine =
            StartCoroutine(DemonstrationRoutine());
    }

    public void StopDemonstration()
    {
        if (demonstrationCoroutine != null)
        {
            StopCoroutine(demonstrationCoroutine);
            demonstrationCoroutine = null;
        }
    }

    public void SetAlgorithm(int algorithmIndex)
    {
        if (
            System.Enum.IsDefined(
                typeof(AlgorithmType),
                algorithmIndex
            )
        )
        {
            selectedAlgorithm =
                (AlgorithmType)algorithmIndex;
        }
    }

    private IEnumerator TrainingRoutine()
    {
        for (int episode = 1; episode <= totalEpisodes; episode++)
        {
            currentEpisode = episode;

            EpisodeResult result =
                RunSelectedAlgorithm();

            currentReward = result.totalReward;
            currentSteps = result.steps;

            rewardHistory.Add(result.totalReward);
            stepHistory.Add(result.steps);

            if (result.reachedGoal)
            {
                successfulEpisodes++;
            }
            else if (gridManager.IsPit(agentController.CurrentState))
            {
                trainingPits++;
            }

            if (episode % 100 == 0)
            {
                yield return null;
            }
        }

        trainingCoroutine = null;

        Debug.Log(
            $"Entrenamiento finalizado | " +
            $"Algoritmo: {selectedAlgorithm} | " +
            $"Éxitos: {successfulEpisodes}/{totalEpisodes} | " +
            $"Fosos: {trainingPits} | " +
            $"Tiempo agotado: {totalEpisodes - successfulEpisodes - trainingPits}"
        );

        EvaluatePolicy();

        if (showPolicyAfterTraining)
        {
            StartPolicyDemonstration();
        }
    }

    private void EvaluatePolicy()
    {
        int goals = 0;
        int pits = 0;
        float sumReward = 0f;
        float sumSteps = 0f;
        float sumCollisions = 0f;
        float sumRepeated = 0f;

        for (int episode = 1; episode <= evaluationEpisodes; episode++)
        {
            agentController.ResetAgent();

            float reward = 0f;
            int steps = 0;

            while (
                steps < evaluationMaxSteps &&
                !agentController.IsGoalReached()
            )
            {
                int state = agentController.CurrentState;

                int action =
                    Random.value < evaluationEpsilon
                        ? Random.Range(0, 4)
                        : GetDeterministicGreedyAction(state);

                reward += agentController
                    .ExecuteAction((ActionType)action).reward;
                steps++;
            }

            bool reachedGoal =
                gridManager.IsGoal(agentController.CurrentState);
            bool fellInPit =
                gridManager.IsPit(agentController.CurrentState);

            if (reachedGoal) goals++;
            if (fellInPit) pits++;

            sumReward += reward;
            sumSteps += steps;
            sumCollisions += agentController.Collisions;
            sumRepeated += agentController.RepeatedVisits;
        }

        float n = evaluationEpisodes;

        Debug.Log(
            $"RESUMEN {selectedAlgorithm} | Episodios: {evaluationEpisodes} | " +
            $"Éxito: {100f * goals / n:F1}% | Fosos: {pits} | " +
            $"Recompensa media: {sumReward / n:F1} | " +
            $"Pasos medios: {sumSteps / n:F1} | " +
            $"Colisiones medias: {sumCollisions / n:F2} | " +
            $"Visitas repetidas medias: {sumRepeated / n:F1}"
        );

        agentController.ResetAgent();
    }

    private IEnumerator DemonstrationRoutine()
    {
        agentController.ResetAgent();

        for (int step = 0; step < maxDemonstrationSteps; step++)
        {
            if (agentController.IsGoalReached())
            {
                break;
            }

            int state = agentController.CurrentState;
            int action = GetDeterministicGreedyAction(state);

            agentController.ExecuteAction((ActionType)action);

            yield return new WaitForSeconds(movementDelay);
        }

        demonstrationCoroutine = null;
    }

    private EpisodeResult RunSelectedAlgorithm()
    {
        switch (selectedAlgorithm)
        {
            case AlgorithmType.MonteCarlo:
                return monteCarlo.TrainEpisode(maxStepsPerEpisode);

            case AlgorithmType.Sarsa:
                return sarsa.TrainEpisode(maxStepsPerEpisode);

            default:
                return qLearning.TrainEpisode(maxStepsPerEpisode);
        }
    }

    private int GetDeterministicGreedyAction(int state)
    {
        switch (selectedAlgorithm)
        {
            case AlgorithmType.MonteCarlo:
                return monteCarlo.GetGreedyAction(state, false);

            case AlgorithmType.Sarsa:
                return sarsa.GetGreedyAction(state, false);

            default:
                return qLearning.GetGreedyAction(state, false);
        }
    }

    private void ResetSelectedAlgorithm()
    {
        switch (selectedAlgorithm)
        {
            case AlgorithmType.MonteCarlo:
                monteCarlo.ResetLearning();
                break;

            case AlgorithmType.Sarsa:
                sarsa.ResetLearning();
                break;

            case AlgorithmType.QLearning:
                qLearning.ResetLearning();
                break;
        }
    }

    private void ResetStatistics()
    {
        currentEpisode = 0;
        successfulEpisodes = 0;
        trainingPits = 0;
        currentReward = 0f;
        currentSteps = 0;
        rewardHistory.Clear();
        stepHistory.Clear();
    }

    private bool ValidateReferences()
    {
        return
            gridManager != null &&
            agentController != null &&
            monteCarlo != null &&
            sarsa != null &&
            qLearning != null;
    }

    private void OnDisable()
    {
        StopTraining();
        StopDemonstration();
    }
}

[System.Serializable]
public struct EpisodeResult
{
    public float totalReward;
    public int steps;
    public bool reachedGoal;

    public EpisodeResult(
        float totalReward,
        int steps,
        bool reachedGoal
    )
    {
        this.totalReward = totalReward;
        this.steps = steps;
        this.reachedGoal = reachedGoal;
    }
}
