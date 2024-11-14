using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed;
    public float size;
    public float range;

    private float distance;
    private Vector2 startPos;

    private Rigidbody2D rg;

    [SerializeField] PlayerManager player;
    [SerializeField] ParticleSystem CollisionVFX;
    private TutorialManager tutorialManager;
    public Projectile (int speed, float size, float range)
    {
        this.speed = speed;
        this.size = size;
        this.range = range;
    }

    private void Awake()
    {
        rg = GetComponent<Rigidbody2D>();
        startPos = gameObject.transform.position;
        range = player.shotRange;

        if(UnityEngine.Object.FindAnyObjectByType<TutorialManager>() != null )
        {
            tutorialManager = UnityEngine.Object.FindAnyObjectByType<TutorialManager>();
        }
    }

    private void Update()   
    {
        distance = Vector2.Distance(startPos, gameObject.transform.position);
        //Debug.Log("Distance: "+distance);
        //Debug.Log("Range: "+range);
        if (distance > range)
        {
            gameObject.SetActive(false);
            Debug.Log("Proyectile Out of Reach, therefore back to the pool");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Wall") || other.CompareTag("EnemySoul") ||other.CompareTag("EnemyAndras"))
        {
            if (other.CompareTag("Enemy"))
            {
                other.gameObject.GetComponent<IncubusScript>().takeDamage(player.shotDamage);
                if (other.transform.GetComponent<IncubusScript>().Health <= 0 && tutorialManager != null)
                {
                    tutorialManager.currentEnemies++;
                }
            }

            if (other.CompareTag("EnemySoul"))
            {
                if (other.name == "Asmodeus")
                {
                    other.gameObject.GetComponent<AsmodeusScript>().takeDamage(player.shotDamage);
                }

                other.gameObject.GetComponent<LostSoulScript>().takeDamage(player.shotDamage);
                if (other.transform.GetComponent<LostSoulScript>().health <= 0 && tutorialManager != null)
                {
                    tutorialManager.currentEnemies++;
                }
            }

            if (other.CompareTag("EnemyAndras"))
            {
                other.gameObject.GetComponent<AndrasScript>().takeDamage(player.shotDamage);
                if (other.transform.GetComponent<AndrasScript>().health <= 0 && tutorialManager != null)
                {
                    tutorialManager.currentEnemies++;
                }
            }

            StartCoroutine(VFXthenDestroyObj());
        }
    }

    private IEnumerator VFXthenDestroyObj()
    {
        CollisionVFX.Play();
        rg.velocity = Vector2.zero;
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
        yield return new WaitForSeconds(CollisionVFX.main.duration);
        gameObject.SetActive(false);
    }
}
