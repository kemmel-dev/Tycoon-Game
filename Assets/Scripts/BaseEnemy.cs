using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BaseEnemy : MonoBehaviour
{
    public int health;

    [SerializeField] private Transform target;
    [SerializeField] private bool activateTarget;

    private NavMeshAgent _navMeshAgent;

    private Vector3 _startPos;

    // Start is called before the first frame update
    private void Awake()
    {
        health = GameManager.Instance.EnemyBaseHealth;
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _startPos = transform.localPosition;
    }

    private void Update()
    {
        target = EnemySpawner.Instance.currentTarget;
        if (activateTarget)
        {
            _navMeshAgent.isStopped = false;
            SetTarget(target);
        }
        else
        {
            _navMeshAgent.isStopped = true;
        }

        CheckHealth();
    }


    private void CheckHealth()
    {
        if (health > 0) return;
        transform.localPosition = _startPos;
        EnemySpawner.Instance.KillEnemy(gameObject);
        health = GameManager.Instance.EnemyBaseHealth;
    }

    private void SetTarget(Transform targetObj)
    {
        _navMeshAgent.SetDestination(targetObj.position);
    }
}