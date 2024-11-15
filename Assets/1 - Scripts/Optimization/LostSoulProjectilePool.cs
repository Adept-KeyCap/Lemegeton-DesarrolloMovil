using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LostSoulProjectilePool : MonoBehaviour
{
    public static LostSoulProjectilePool Instance { get; private set; }
    [SerializeField] private GameObject pooledProjectile;
    public int poolSize = 10;
    private List<GameObject> projectilePool;

    private void Awake()
    {
        //Singleton for simplicity
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        projectilePool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(pooledProjectile);
            projectile.SetActive(false);
            projectilePool.Add(projectile);
        }
    }

    public GameObject GetPooledProjectile()
    {
        foreach (GameObject projectile in projectilePool)
        {
            if (!projectile.activeInHierarchy)
            {
                return projectile;
            }
        }

        //Just to make sure that if there arent enough bullets active, it would create another one (most likely dont needed, but i have 0 clue how fast you can shoot when the game progresses)
        GameObject newProjectile = Instantiate(pooledProjectile);
        newProjectile.SetActive(false);
        projectilePool.Add(newProjectile);
        return newProjectile;
    }
}
