using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Configuración visual")]
    [Min(0.1f)]
    [SerializeField] private float cellSize = 1f;

    [Range(0.1f, 0.99f)]
    [SerializeField] private float cellFill = 0.92f;

    [Header("Colores")]
    [SerializeField] private Color freeColor = Color.white;
    [SerializeField] private Color wallColor = Color.black;
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color goalColor = Color.cyan;

    // 0 = libre, 1 = muro, 2 = inicio, 3 = meta.
    private readonly int[,] maze =
    {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,2,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,0,1,0,1,1,1,1,1,0,1,0,1,0,1,1,1,1,0,1,1,0,1},
        {1,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1},
        {1,0,1,0,1,0,1,0,1,0,1,0,1,0,1,1,1,1,1,1,1,0,1},
        {1,0,1,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,0,1,0,1,1,1,0,0,0,1,1,1,1,1,0,1,0,1,0,1,0,1},
        {1,0,0,0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,1,0,1},
        {1,1,1,1,1,0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,1,0,1},
        {1,0,0,0,1,0,0,0,1,0,1,0,0,0,1,0,0,0,0,0,0,0,1},
        {1,0,1,0,1,1,1,1,1,0,1,0,1,0,1,0,1,0,1,1,1,0,1},
        {1,0,1,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,0,0,0,0,1},
        {1,0,1,1,1,1,1,0,1,0,1,0,1,0,1,0,1,1,1,1,1,1,1},
        {1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,1},
        {1,0,1,0,1,1,1,0,1,0,1,0,1,1,1,1,1,0,1,1,1,0,1},
        {1,0,1,0,0,0,0,0,0,0,1,0,0,0,1,0,1,0,0,0,1,0,1},
        {1,0,1,1,1,0,1,1,1,0,1,1,1,0,1,0,1,0,1,0,1,0,1},
        {1,0,0,0,1,0,1,0,0,0,0,0,0,0,1,0,0,0,1,0,1,0,1},
        {1,1,1,0,1,0,1,0,1,1,1,0,1,0,1,1,0,1,1,0,1,0,1},
        {1,0,1,0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,1,0,1,0,1},
        {1,0,1,1,1,0,1,0,1,1,1,1,1,1,1,1,1,0,1,0,1,0,1},
        {1,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,3,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
    };

    private readonly Dictionary<int, Vector3> statePositions =
        new Dictionary<int, Vector3>();

    private readonly Dictionary<int, int> cellTypes =
        new Dictionary<int, int>();

    public int Rows => maze.GetLength(0);
    public int Cols => maze.GetLength(1);
    public int TotalStates => Rows * Cols;

    public int StartState { get; private set; } = -1;
    public int GoalState { get; private set; } = -1;

    private void Awake()
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        ClearGrid();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                int state = GetState(row, col);
                int cellType = maze[row, col];

                cellTypes[state] = cellType;
                CreateCell(state, row, col, cellType);

                if (cellType == 2)
                {
                    StartState = state;
                }
                else if (cellType == 3)
                {
                    GoalState = state;
                }
            }
        }
    }

    private void ClearGrid()
    {
        statePositions.Clear();
        cellTypes.Clear();
        StartState = -1;
        GoalState = -1;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private void CreateCell(
        int state,
        int row,
        int col,
        int cellType
    )
    {
        float x = col * cellSize;
        float y = (Rows - 1 - row) * cellSize;
        float z = cellType == 1 ? -0.3f : 0f;

        GameObject cell =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        cell.name = $"S{state}";
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = new Vector3(x, y, z);
        cell.transform.localScale = new Vector3(
            cellSize * cellFill,
            cellSize * cellFill,
            cellType == 1 ? 0.7f : 0.1f
        );

        statePositions[state] = cell.transform.position;

        Renderer renderer = cell.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = CreateMaterial(cellType);
        }
    }

    private Material CreateMaterial(int cellType)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("GridManager: no se encontró un shader.");
            return null;
        }

        Material material = new Material(shader);
        Color color = GetCellColor(cellType);

        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    private Color GetCellColor(int cellType)
    {
        switch (cellType)
        {
            case 0:
                return freeColor;
            case 1:
                return wallColor;
            case 2:
                return startColor;
            case 3:
                return goalColor;
            default:
                return Color.magenta;
        }
    }

    public int GetState(int row, int col)
    {
        if (!IsInsideGrid(row, col))
        {
            return -1;
        }

        return row * Cols + col + 1;
    }

    public void GetRowAndColumn(
        int state,
        out int row,
        out int col
    )
    {
        if (!IsValidState(state))
        {
            row = -1;
            col = -1;
            return;
        }

        int index = state - 1;
        row = index / Cols;
        col = index % Cols;
    }

    public bool IsInsideGrid(int row, int col)
    {
        return row >= 0 &&
               row < Rows &&
               col >= 0 &&
               col < Cols;
    }

    public bool IsValidState(int state)
    {
        return state >= 1 && state <= TotalStates;
    }

    public bool IsWall(int state)
    {
        return GetCellType(state) == 1;
    }

    public bool IsTraversable(int state)
    {
        return IsValidState(state) && !IsWall(state);
    }

    public bool IsTerminal(int state)
    {
        return state == GoalState;
    }

    public int GetCellType(int state)
    {
        return cellTypes.TryGetValue(state, out int cellType)
            ? cellType
            : -1;
    }

    public Vector3 GetPosition(int state)
    {
        return statePositions.TryGetValue(state, out Vector3 position)
            ? position
            : Vector3.zero;
    }

    public bool TryGetNextState(
        int state,
        int action,
        out int nextState
    )
    {
        nextState = -1;

        if (!IsValidState(state))
        {
            return false;
        }

        GetRowAndColumn(state, out int row, out int col);

        switch ((ActionType)action)
        {
            case ActionType.Up:
                row--;
                break;
            case ActionType.Right:
                col++;
                break;
            case ActionType.Down:
                row++;
                break;
            case ActionType.Left:
                col--;
                break;
            default:
                return false;
        }

        int candidateState = GetState(row, col);

        if (!IsTraversable(candidateState))
        {
            return false;
        }

        nextState = candidateState;
        return true;
    }

    public void GetValidActions(
        int state,
        List<int> validActions
    )
    {
        validActions.Clear();

        for (int action = 0; action < 4; action++)
        {
            if (TryGetNextState(state, action, out _))
            {
                validActions.Add(action);
            }
        }
    }
}
