using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class AgentBehaviour : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform target;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        target = GameObject.FindWithTag("Target").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(target != null)
        {
            agent.destination = target.position;
        }

        if (Vector3.Distance(agent.transform.position, target.position) < 1f)
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;

            // Reload the scene by its index
            SceneManager.LoadScene(currentScene);
        }
    }
}
