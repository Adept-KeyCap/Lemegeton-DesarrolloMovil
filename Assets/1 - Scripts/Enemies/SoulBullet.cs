using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using static Cinemachine.CinemachineFreeLook;

public class SoulBullet : MonoBehaviour
{
    Rigidbody2D rb;
    float damage;
    [SerializeField] ParticleSystem CollisionVFX;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        //LostSoulScript lsScript = this.GetComponentInParent<LostSoulScript>();
        //damage = lsScript.shotDamage;
    }

    void Update()
    {
        if (transform.position.magnitude > 80.0f)
        {
            gameObject.SetActive(false);
        }
    }

    public void shoot(Vector2 direction, float force, float shotDamage)
    {
        damage = shotDamage;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("EnemySoul"))
        {
            return;
        }

        GameObject player = other.gameObject;
        PlayerManager pm = player.GetComponent<PlayerManager>();
        //Robin playerController = player.GetComponent<Robin>();

        if (pm != null)
        {
            //pm.changeHealth(-0.5f);
            pm.TakeDamage(damage);

        }

        StartCoroutine(VFXthenDestroyObj());

    }

    private IEnumerator VFXthenDestroyObj()
    {
        CollisionVFX.Play();
        rb.velocity = Vector2.zero;
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
        yield return new WaitForSeconds(CollisionVFX.main.duration);
        gameObject.GetComponent<SpriteRenderer>().enabled = true;
        gameObject.SetActive(false);

    }
}
