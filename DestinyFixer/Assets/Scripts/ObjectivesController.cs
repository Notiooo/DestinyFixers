using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectivesController : MonoBehaviour
{
    [SerializeField] public float minEventTimeout;
    [SerializeField] public float maxEventTimeout;
    [SerializeField] public float minEventSpawnInterval;
    [SerializeField] public float maxEventSpawnInterval;
    [SerializeField] public ObjectiveIndicator objectiveIndicator;
    [SerializeField] public GameObject player;
    [SerializeField] public int maxActiveEvents;
    [SerializeField] public int maxFailedEvents;
    public enum ObjectiveType {
        OBJECTIVE_TIGHTEN
    }
    public class ObjectiveEvent {
        public ObjectiveEvent(Objective _objective){
            objective = _objective;
        }
        public Objective objective;
        public float timeout = 20;
        public bool active = false;
        public float blinkFrequency = 0.75f;
        public bool isBlinked = false;
        public ObjectiveType type = ObjectiveType.OBJECTIVE_TIGHTEN;
    }
    private List<ObjectiveEvent> events = new List<ObjectiveEvent>();
    private List<ObjectiveEvent> activeEvents = new List<ObjectiveEvent>();
    private List<ObjectiveEvent> dormantEvents = new List<ObjectiveEvent>();

    private int failedEvents = 0;

    // Start is called before the first frame update
    void Start()
    {
        SetObjectiveMarkers();
        StartCoroutine(SpawnEvents());
    }

    void SetObjectiveMarkers()
    {
        foreach(Transform child in transform){
            dormantEvents.Add(
                new ObjectiveEvent(child.GetComponent<Objective>())
            );
        }
    }

    public IEnumerator SpawnEvents()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minEventSpawnInterval, maxEventSpawnInterval));
            if (activeEvents.Count < maxActiveEvents && dormantEvents.Count != 0)
            {
                int activateEvent = Random.Range(0, dormantEvents.Count);
                activeEvents.Add(dormantEvents[activateEvent]);
                dormantEvents.RemoveAt(activateEvent);

                int activatedEvent = activeEvents.Count - 1;
                activeEvents[activatedEvent].timeout = Random.Range(minEventTimeout, maxEventTimeout);
                activeEvents[activatedEvent].active = true;
                objectiveIndicator.AddObjective(activeEvents[activatedEvent].objective);
                StartCoroutine(RunEvent(activeEvents[activatedEvent]));
            }
        }
    }

    public IEnumerator Blinker(ObjectiveEvent objectiveEvent)
    {
        while(objectiveEvent.active) {
            yield return new WaitForSeconds(1/objectiveEvent.blinkFrequency);
            objectiveEvent.isBlinked = !objectiveEvent.isBlinked;

            objectiveIndicator.SetRedOverlay(objectiveEvent.objective, objectiveEvent.isBlinked);
        }
    }

    public IEnumerator RunEvent(ObjectiveEvent objectiveEvent)
    {
        float elapsed = 0.0f;
        StartCoroutine(Blinker(objectiveEvent));

        while (elapsed < objectiveEvent.timeout)
        {
            float t = elapsed / objectiveEvent.timeout;
            t = t * t * t * t * (3f - 2f * t);

            objectiveEvent.blinkFrequency = Mathf.Lerp(0.75f, 20f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if(objectiveEvent.active) {
            objectiveEvent.objective.EnableEmmiter();
            DeleteObjective(objectiveEvent);
            
            failedEvents ++;

            if(failedEvents >= maxFailedEvents)
            {
                GameOver();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CheckObjectivesInteraction()
    {
        if(activeEvents.Count == 0) {
            return;
        }

        List<Objective> objectivesToRemove = new List<Objective>();
        Vector3 playerPos = player.transform.position;
        for(int i = 0; i < activeEvents.Count; i++) {
            Vector3 objectivePos = activeEvents[i].objective.transform.position;

            float distance = Vector3.Distance(playerPos, objectivePos);
            if (distance < 2.0f)
            {
                DeactivateObjectiveEvent(activeEvents[i]);
                GameplayManager.Instance.pushState(GameplayState.MINIGAME);
            }
        }
    }

    public void Interact(InputAction.CallbackContext context)
    {
        CheckObjectivesInteraction();
    }

    public void DeleteObjective(ObjectiveEvent objectiveEvent)
    {
        DeactivateObjectiveEvent(objectiveEvent);
        dormantEvents.Remove(objectiveEvent);
    }

    private void DeactivateObjectiveEvent(ObjectiveEvent objectiveEvent)
    {
        objectiveEvent.active = false;
        dormantEvents.Add(objectiveEvent);
        activeEvents.Remove(objectiveEvent);
        objectiveIndicator.RemoveObjective(objectiveEvent.objective);
    }

    private void GameOver()
    {
        StopAllCoroutines();
        GameplayManager.Instance.EndGame("Your ship has been irreparably damaged. You face a lonely death on this ship. \n\n Gotta go fast next time.");
    }
}
