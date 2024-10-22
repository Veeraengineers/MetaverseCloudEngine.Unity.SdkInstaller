using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;

[Preserve]
public class UnityNavMeshAgentPreserve : MonoBehaviour
{
    void Start()
    {
        try
        {
            NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.destination = new Vector3(0, 0, 0);
            navMeshAgent.SetDestination(Vector3.zero);
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.angularSpeed = 0;
            navMeshAgent.acceleration = 0;
            navMeshAgent.autoTraverseOffMeshLink = false;
            navMeshAgent.autoRepath = false;
            navMeshAgent.autoBraking = false;
            navMeshAgent.height = 0;
            navMeshAgent.baseOffset = 0;
            navMeshAgent.radius = 0;
            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            navMeshAgent.avoidancePriority = 0;
            navMeshAgent.stoppingDistance = 0;
            var r = navMeshAgent.remainingDistance;
            var p = navMeshAgent.path;
            var c = navMeshAgent.pathStatus;
            var s = navMeshAgent.isStopped;
            var w = navMeshAgent.isOnNavMesh;
            var a = navMeshAgent.hasPath;
            var b = navMeshAgent.pathPending;
            var d = navMeshAgent.updatePosition;
            var e = navMeshAgent.updateRotation;
            var f = navMeshAgent.nextPosition;
            var g = navMeshAgent.agentTypeID;
            var v = navMeshAgent.velocity;
            var steering = navMeshAgent.steeringTarget;
        }
        catch (Exception exception)
        {
        }
    }
}