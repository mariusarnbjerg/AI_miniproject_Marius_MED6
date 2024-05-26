//Based on: https://www.youtube.com/watch?v=_aeYq5BmDMg&t=118s&ab_channel=KetraGames
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] 
    private MazeCell mazeCellPrefab;

    [SerializeField]
    private int mazeWidth;

    [SerializeField]
    private int mazeDepth;

    [SerializeField, Tooltip("Algorithm generation speed. Higher value means slower speed")][Range(0f, 1f)]
    private float generationSpeed;

    private MazeCell[,] mazeGrid;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private NavMeshSurface navMeshSurface;

    [SerializeField]
    private NavMeshAgent navMeshAgent;

    [SerializeField]
    private Transform target;

    [SerializeField, Tooltip("Makes sure that the navmesh surface covers the entire maze. If the maze is a scale of 5 fx. 5x5, 5x15, 25x5 etc. this value should be 0.")]
    private float navMeshSurfaceSizeBuffer;

    private int cellsChecked; //Keeps track of how many nextCell's have equaled null


    IEnumerator Start()
    {

        navMeshSurface.gameObject.transform.localScale = new Vector3(mazeWidth / 10f + navMeshSurfaceSizeBuffer, 1, mazeDepth / 10f + navMeshSurfaceSizeBuffer);//Divide by 10 because the default size of a plane in unity is 10x10 units
        navMeshSurface.gameObject.transform.position = new Vector3(mazeWidth / 2, 0, mazeDepth / 2); //Centers the surface under the maze

        mainCamera.transform.position = new Vector3(mazeWidth / 2, 20, mazeDepth / 2); //Centers the camera over the generated maze
        mainCamera.orthographicSize = (mazeWidth + mazeDepth) / 2; //Makes sure that the camera can see the entire maze despite size of width and depth

        mazeGrid = new MazeCell[mazeWidth, mazeDepth];

        //Creates the initial grid of cells to generate the maze from by instantiating a mazeCell for each width and depth unit in the mazeGrid array
        for (int x = 0; x < mazeWidth; x++)
        {
            for(int z = 0; z < mazeDepth; z++)
            {
                mazeGrid[x,z] = Instantiate(mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
            }
        }


        yield return GenerateMaze(null, mazeGrid[0, 0]); //Sets the starting position of the algorithm by setting the x and z position of the 'currentCell'.
    }

    private IEnumerator GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        currentCell.Visit();

        ClearWalls(previousCell, currentCell);

        yield return new WaitForSeconds(generationSpeed);

        MazeCell nextCell;

        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);

            if (nextCell != null)
            {
                yield return GenerateMaze(currentCell, nextCell);
            }

        } while(nextCell != null);

        if (nextCell == null)  //The amount of times this if-statement runs, corresponds to the amount of cells in the grid 
        {
            cellsChecked++;

            //The navmesh should only be generated once all cells have been checked
            if (cellsChecked == mazeWidth * mazeDepth) //The amount of cells is calculated by mazeWidth x mazeDepth
            {
                navMeshSurface.BuildNavMesh();

                InstantiateObjectOnNavMesh(target.gameObject);
                yield return new WaitForSeconds(2);

                InstantiateObjectOnNavMesh(navMeshAgent.gameObject);
            }
        }
    }

    private MazeCell GetNextUnvisitedCell(MazeCell currentCell) //Gets a random unvisited neighbor
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);

        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        if(x + 1 < mazeWidth) //Checks if there's an unvisited cell to the right
        {
            var cellToRight = mazeGrid[x + 1,z];

            if(cellToRight.isVisited == false)
            {
                yield return cellToRight;
            }
        }

        if (x - 1 >= 0) //Checks if there's an unvisited cell to the left
        {
            var cellToLeft = mazeGrid[x - 1, z];

            if (cellToLeft.isVisited == false)
            {
                yield return cellToLeft;
            }
        }

        if (z + 1 < mazeDepth) //Checks if there's an unvisited cell to the front
        {
            var cellToFront = mazeGrid[x, z + 1];

            if (cellToFront.isVisited == false)
            {
                yield return cellToFront;
            }
        }

        if (z - 1 >= 0) //Checks if there's an unvisited cell to the back
        {
            var cellToBack = mazeGrid[x, z - 1];

            if (cellToBack.isVisited == false)
            {
                yield return cellToBack;
            }
        }


    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if(previousCell == null)
        {
            return;
        }

        if(previousCell.transform.position.x < currentCell.transform.position.x) //If the algorithm has gone from left to right
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
            return;
        }

        if(previousCell.transform.position.x > currentCell.transform.position.x) //If the algorithm has gone from right to left
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();

            return;
        }

        if(previousCell.transform.position.z < currentCell.transform.position.z) //If the algorithm has moved forward
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
            return;
        }

        if(previousCell.transform.position.z > currentCell.transform.position.z) //If the algorithm has moved backwards
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
            return;
        } 
    }

    private void InstantiateObjectOnNavMesh(GameObject gameObject)
    {
        // Generate a random point on the NavMesh
        Vector3 randomPoint = GetRandomPointOnNavMesh();

        // Instantiate the object at the random point
        Instantiate(gameObject, randomPoint, Quaternion.identity);
    }

    private Vector3 GetRandomPointOnNavMesh()
    {
        // Get the center of the NavMesh
        Vector3 center = navMeshSurface.transform.position;

        // Generate a random direction within a circle
        Vector2 randomDirection = Random.insideUnitCircle.normalized * mazeDepth;

        // Calculate the random point around the center
        Vector3 randomPoint = center + new Vector3(randomDirection.x, 0f, randomDirection.y);

        // Sample the NavMesh to find the nearest point
        NavMeshHit hit;
        NavMesh.SamplePosition(randomPoint, out hit, mazeWidth, NavMesh.AllAreas);

        return hit.position;
    }
}
