using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Room : MonoBehaviour
{
    public UnityEvent NoRemainingEnemies;

    private EdgeCollider2D edgeCollider;
    private GameObject dungeonManager;
    private Dungeon _dungeonManager;
    private HashSet<GameObject> enemiesHashSet = new HashSet<GameObject>();

    private ScoreBoard scoreBoard;
    private ChronometerManager chrono;

    // The first Collider is the one who detects the player and trapts it in the Room
    [Header("Colliders Config")]
    [SerializeField] private Vector2 InnerColliderSize;
    [SerializeField] private BoxCollider2D mainBoxCollider;
    [SerializeField] private GameObject combatOverlay;

    [Header("Enemies Config")]
    // pls especify how many enemies prefabs there are
    [SerializeField] private int numberOfEnemiesPrefabs;
    [SerializeField] private GameObject enemyPrefab1, enemyPrefab2, enemyPrefab3;
    //private bool inCombat = false;
    private bool enemiesBuffed = false;
    private bool playerInCombat = false;
    private bool roomUsed = false;

    void Awake()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        mainBoxCollider = GetComponent<BoxCollider2D>();

        mainBoxCollider.enabled = true;
        edgeCollider.enabled = false;
    }

    private void Start()
    {
        scoreBoard = UnityEngine.Object.FindObjectOfType<ScoreBoard>();
        chrono = UnityEngine.Object.FindObjectOfType<ChronometerManager>();
        roomUsed = false;
        dungeonManager = GameObject.Find("Dungeon Manager");
        _dungeonManager = dungeonManager.GetComponent<Dungeon>();
        combatOverlay.SetActive(false);
    }

    private void Update()
    {
        if (playerInCombat)
        {
            IsPlayerInside();
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        var @object = other.gameObject;
        if (@object.CompareTag("Player") && playerInCombat == false)
        {
            playerInCombat = true;
        }
        else if (@object.CompareTag("Enemy") || @object.CompareTag("EnemySoul") || @object.CompareTag("EnemyAndras"))
        {
            if (!enemiesHashSet.Contains(@object))
            {
                enemiesHashSet.Add(@object);
            }
        }
        else if (!@object.CompareTag("Dungeon Manager"))
        {
            //Debug.Log("Entity: "+ @object.name + " Not Recognized");
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        var @object = other.gameObject;
        if (@object.CompareTag("Player"))
        {
            playerInCombat = false;
        }
        else if (@object.CompareTag("Enemy") || @object.CompareTag("EnemySoul") || @object.CompareTag("EnemyAndras"))
        {
            if (enemiesHashSet.Contains(@object))
            {
                enemiesHashSet.Remove(@object);
                Destroy(@object);
                if (enemiesHashSet.Count == 0)
                {
                    playerInCombat = false;
                    NoRemainingEnemies.Invoke();
                    //inCombat = false;
                    enemiesBuffed = false;
                    scoreBoard.GetPoints(1000 * chrono.difficultyLvl);
                    Destroy(gameObject);
                }
            }
        }
        else if (!@object.CompareTag("Room"))
        {
            //Debug.Log("Entity: " + @object.name + " Not Recognized");
        }

    }

    private Vector2 RandomEnemySpawnPos()
    {
        float newBoundary_x = UnityEngine.Random.Range(mainBoxCollider.bounds.min.x + 5, mainBoxCollider.bounds.max.x -5);
        float newBoundary_y = UnityEngine.Random.Range(mainBoxCollider.bounds.min.y + 5, mainBoxCollider.bounds.max.y -5);

        Vector2 enemyPos = new Vector2(newBoundary_x, newBoundary_y);
        return enemyPos;
    }

    private void SpawnEnemies(int addEnemyCount)
    {
        for (int i = 0; i < addEnemyCount; i++)
        {
            int enemyToSpawn = UnityEngine.Random.Range(0, (numberOfEnemiesPrefabs));
            if (enemyToSpawn == 0)
            {
                GameObject incubusOb = Instantiate(enemyPrefab1, RandomEnemySpawnPos(), Quaternion.identity);
                incubusOb.GetComponent<IncubusScript>().Health = _dungeonManager.incubus_StatsMultiplier.x;
                incubusOb.GetComponent<IncubusScript>().ContactDamage = _dungeonManager.incubus_StatsMultiplier.y;
                incubusOb.GetComponent<IncubusScript>().MoveSpeed = _dungeonManager.incubus_StatsMultiplier.z;
            }
            else if (enemyToSpawn == 1)
            {
                GameObject lostSoul = Instantiate(enemyPrefab2, RandomEnemySpawnPos(), Quaternion.identity);
                lostSoul.GetComponent<LostSoulScript>().health = _dungeonManager.LostSoul_StatsMultiplier.x;
                lostSoul.GetComponent<LostSoulScript>().shotDamage = _dungeonManager.LostSoul_StatsMultiplier.y;
                lostSoul.GetComponent<LostSoulScript>().moveSpeed = _dungeonManager.LostSoul_StatsMultiplier.z;
            }
            else if (enemyToSpawn == 2)
            {
                GameObject andras = Instantiate(enemyPrefab3, RandomEnemySpawnPos(), Quaternion.identity);
                andras.GetComponent<AndrasScript>().health = _dungeonManager.andras_StatsMultiplier.x;
                andras.GetComponent<AndrasScript>().shotDamage = _dungeonManager.andras_StatsMultiplier.y;
                andras.GetComponent<AndrasScript>().moveSpeed = _dungeonManager.andras_StatsMultiplier.z;
                andras.GetComponent<AndrasScript>().firerate = 6.5f;
            }
        }
    }

    private void IsPlayerInside()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(mainBoxCollider.bounds.center, InnerColliderSize, 0);

        foreach (Collider2D obj in colliders)
        {
            if(obj.gameObject.CompareTag("Player") && !roomUsed)
            {
                playerInCombat = true;
                edgeCollider.enabled = true;
                StartCoroutine(WaitAndTrapPlayer(2, enemyPrefab1, enemyPrefab2, enemyPrefab3));
                roomUsed = true;
            }
        }
    }

    IEnumerator WaitAndTrapPlayer(float waitTime, GameObject prefab1, GameObject prefab2, GameObject prefab3)
    {
        combatOverlay.SetActive(true);

        yield return new WaitForSeconds(waitTime);

        if (enemiesBuffed == false)
        {
            enemiesBuffed = true;
            SpawnEnemies(2 + _dungeonManager.additionalEnemyCount);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(mainBoxCollider.bounds.center , InnerColliderSize);

    }
}
