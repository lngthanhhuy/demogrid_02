using System;
using SenCity.Core.Pet;
using UnityEngine;
using UnityEngine.AI;

namespace SenCity.Features.PetNpc
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class PetNpcController : MonoBehaviour, ISenCityPetCommands
    {
        private enum PetMode
        {
            Wandering,
            MovingToFurniture,
            Feeding,
            Playing,
            Sleeping
        }

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField, Min(0f)] private float waitAtPoint = 1.5f;
        [SerializeField, Min(0.1f)] private float sampleRadius = 4f;

        [Header("Activities")]
        [SerializeField] private Transform feedPoint;
        [SerializeField] private Transform playPoint;
        [SerializeField] private Transform sleepPoint;
        [SerializeField, Min(0.5f)] private float activityDuration = 4f;
        [SerializeField] private Animator animator;

        private NavMeshAgent agent;
        private int pointIndex;
        private float waitTimer;
        private float activityTimer;
        private PetMode mode = PetMode.Wandering;
        private string arrivalMessage;

        public event Action<string> StateChanged;

        public string CurrentStateLabel { get; private set; } = "Mochi is exploring the studio.";

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            PublishState(CurrentStateLabel);
            SetNextPatrolDestination();
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            if (animator != null)
                animator.SetFloat("Speed", agent.velocity.magnitude);

            if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.04f)
                return;

            if (mode != PetMode.Wandering)
            {
                HandleActivityArrival();
                return;
            }

            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtPoint)
                SetNextPatrolDestination();
        }

        public void GoToTarget(Transform target, string displayName)
        {
            if (target == null)
                return;

            string safeName = string.IsNullOrWhiteSpace(displayName) ? "the selected furniture" : displayName;
            MoveForActivity(
                target.position,
                PetMode.MovingToFurniture,
                $"Mochi is walking to {safeName}.",
                $"Mochi reached {safeName}.");
        }

        public void Feed()
        {
            MoveForActivity(
                feedPoint != null ? feedPoint.position : transform.position,
                PetMode.Feeding,
                "Mochi is going to the food bowl.",
                "Mochi is enjoying a snack.");
        }

        public void Play()
        {
            MoveForActivity(
                playPoint != null ? playPoint.position : transform.position,
                PetMode.Playing,
                "Mochi is going to the play area.",
                "Mochi is happily playing.");
        }

        public void Sleep()
        {
            MoveForActivity(
                sleepPoint != null ? sleepPoint.position : transform.position,
                PetMode.Sleeping,
                "Mochi is going to the cozy bed.",
                "Mochi is resting peacefully.");
        }

        private void MoveForActivity(Vector3 requestedPosition, PetMode nextMode, string movingMessage, string reachedMessage)
        {
            mode = nextMode;
            activityTimer = 0f;
            arrivalMessage = reachedMessage;
            PublishState(movingMessage);

            if (agent == null || !agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            else
                HandleActivityArrival();
        }

        private void HandleActivityArrival()
        {
            if (activityTimer <= 0f)
            {
                if (agent != null && agent.isOnNavMesh)
                    agent.isStopped = true;
                PublishState(arrivalMessage);
            }

            activityTimer += Time.deltaTime;
            if (activityTimer < activityDuration)
                return;

            mode = PetMode.Wandering;
            activityTimer = 0f;
            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = false;
            PublishState("Mochi is exploring the studio.");
            SetNextPatrolDestination();
        }

        private void SetNextPatrolDestination()
        {
            waitTimer = 0f;
            if (agent == null || !agent.isOnNavMesh || patrolPoints == null || patrolPoints.Length == 0)
                return;

            agent.isStopped = false;
            for (int attempt = 0; attempt < patrolPoints.Length; attempt++)
            {
                Transform point = patrolPoints[pointIndex++ % patrolPoints.Length];
                if (point != null && NavMesh.SamplePosition(point.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        private void PublishState(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            CurrentStateLabel = message;
            StateChanged?.Invoke(message);
        }
    }
}
