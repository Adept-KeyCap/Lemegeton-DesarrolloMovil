using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum asmoState
{
    Dead,
    ShootingFast,
    Still,
    BulletHell,
    Striking
    //Wander
};

public class AsmodeusScript : MonoBehaviour
{
    Rigidbody2D rb;
    //Animator animator;
    //AudioSource audioSource;
    private ScoreBoard scoreBoard;
    private ChronometerManager chrono;
    private bool dead = false;

    private bool isCoroutineRunning = false;
    private bool isStrikingCrRunning = false;
    private bool isBulletHellCrRunning = false;

    [SerializeField] private ParticleSystem VFX_TpFrom;
    [SerializeField] private ParticleSystem VFX_TpTo;
    [SerializeField] private ParticleSystem VFX_Tired;

    public Vector3 defaultStats; // hp, attack, speed

    #region MOVE RELATED
    public float moveSpeed;
    Vector2 position;

    [SerializeField] private GameObject spawnPoint;
    #endregion

    #region ATTACK RELATED
    public float firerate;
    public float bulletHellRate;
    float timer;
    float hellTimer;
    public GameObject acidPrefab;
    public Transform firePoint;
    public float shotDamage;
    [SerializeField] private float shootingForce;
    [SerializeField] private float hellForce;
    [SerializeField] private int shootsDone = 0;
    [SerializeField] private GameObject sword1, sword2;
    #endregion

    #region AI RELATED
    GameObject player;
    public asmoState currState = asmoState.ShootingFast;
    [SerializeField] private float strikingSpeed = 1f; // Speed of movement
    [SerializeField] private float perlinScale = 1f; // Scale of the noise
    private Vector2 startPos;
    private int nextState = 0;
    [SerializeField] private int maxShots;
    [SerializeField] private float timeStriking;
    [SerializeField] private float timeStill;
    [SerializeField] private float timeBulletHell;
    [SerializeField] private float rotationOffset = 87f; // Rotation offset

    Vector3 randomDir;
    #endregion

    #region idk RELATED
    public float health;
    private BossHealthBar _healthBar;
    public bool endCanva = false;
    [SerializeField] private ParticleSystem deathVFX;
    //GameObject door;
    //LevelManagement LM;
    #endregion

    #region DROPS RELATED
    //public GameObject drop1Prefab;
    //public GameObject drop2Prefab;
    #endregion

    #region  AUDIO
    [SerializeField] private AudioSource SFXTakeDamage, SFXDie, SFXShot, SFXLaughter, SFXScream, SFXRun;

    // Adjust these variables to set the pitch range
    public float minPitch = 0.6f;
    public float maxPitch = 1.6f;
    //int randPitch; 
    //public AudioClip acidClip;
    #endregion

    public SpriteRenderer asmoSprite;

    void Start()
    {
        deathVFX.gameObject.SetActive(false);
        scoreBoard = UnityEngine.Object.FindObjectOfType<ScoreBoard>();
        chrono = UnityEngine.Object.FindObjectOfType<ChronometerManager>();

        player = GameObject.Find("_Player");
        //door = GameObject.Find("Door");
        rb = GetComponent<Rigidbody2D>();
        timer = firerate;
        hellTimer = bulletHellRate;

        startPos = transform.position;

        sword1.SetActive(false);
        sword2.SetActive(false);

        _healthBar = FindObjectOfType<BossHealthBar>();
        _healthBar.SetMaxHealth(health);
        //LM = door.GetComponent<LevelManagement>();
        //animator = GetComponent<Animator>();
        //audioSource = GetComponent<AudioSource>();

        SFXShot.pitch = 1;
    }

    void FixedUpdate()
    {
        timer -= Time.fixedDeltaTime;
        hellTimer -= Time.fixedDeltaTime;

        //randPitch = Random.Range(-2, 2);

        Vector2 Look = player.GetComponent<Rigidbody2D>().position - (Vector2)firePoint.position;
        Look.Normalize();

        switch (currState)
        {
            //case (asmoState.Wander):
            //    Wander();
            //    break;
            case (asmoState.ShootingFast):
                if (timer < 0)
                {
                    Shoot();
                    timer = firerate;
                }
                ShootingFast();
                break;
            case (asmoState.BulletHell):
                if (hellTimer < 0)
                {
                    BulletHell();
                    hellTimer = bulletHellRate;                   
                }

                if (!isBulletHellCrRunning)
                {
                    StartCoroutine(BulletHellCr(timeBulletHell));
                }
                break;
            case (asmoState.Striking):
                Striking();

                if (!isStrikingCrRunning)
                {
                    StartCoroutine(StrikingCr(timeStriking));
                }
                
                break;
            case (asmoState.Still):
                if (!isCoroutineRunning)
                {
                    StartCoroutine(WaitStill(timeStill)); 
                } 
                break;
            case (asmoState.Dead):
                StartCoroutine(WaitAndDie(0.7f));
                dead = true;
                break;
        }

        if(shootsDone > maxShots)
        {
            currState = asmoState.Still;
            shootsDone = 0;
        }

        if(currState != asmoState.Still)
        {
            StopCoroutine(WaitStill(1));
        }
        _healthBar.SetHealth(health);
    }



    void ShootingFast()
    {

        SFXRun.Play();


        Vector2 position = rb.position;
        Vector2 moveDir = rb.position - player.GetComponent<Rigidbody2D>().position;
        moveDir.Normalize();

        position.x = position.x + moveSpeed * moveDir.x * Time.fixedDeltaTime;
        position.y = position.y + moveSpeed * moveDir.y * Time.fixedDeltaTime;

        rb.MovePosition(position);

    }

    void BulletHell()
    {
        rb.velocity = Vector2.zero;

        int numProjectiles = 16; // Number of projectiles
        float angleStep = 360f / numProjectiles; // Angle between each projectile

        for (int i = 0; i < numProjectiles; i++)
        {
            // Calculate direction to shoot the projectile
            float angleInDegrees = rotationOffset + angleStep * i;
            Vector2 direction = RotateVector2(Vector2.right, angleInDegrees);

            // Instantiate the projectile and get the SoulBullet component
            GameObject acidObject = Instantiate(acidPrefab, firePoint.position, Quaternion.identity);
            SoulBullet sBullet = acidObject.GetComponent<SoulBullet>();

            // Shoot the projectile in the calculated direction
            sBullet.shoot(direction, hellForce, shotDamage);
        }

        //float randPitch = Random.Range(-1f, 1f);
        //SFXShot.pitch = randPitch;

        //switch (SFXShot.pitch)
        //{
        //    case -1f:
        //        SFXShot.pitch = 0.2f;
        //        SFXShot.Play();
        //        break;
        //    case 0.2f:
        //        SFXShot.pitch = 1f;
        //        SFXShot.Play();
        //        break;
        //    case 1f:
        //        SFXShot.pitch = -1f;
        //        SFXShot.Play();
        //        break;
        //    default:
        //        break;
        //}
        SFXShot.Play();

        //PlayWithRandomPitch(SFXShot);

        transform.position = spawnPoint.transform.position;

        // Update the rotation offset for the next time the projectiles are generated
        rotationOffset += angleStep + Random.Range(1, 360); // Change this value to control how much the base direction changes each time
    }

    Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        float tx = v.x;
        float ty = v.y;
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }




    void Striking()
    {
        sword1.SetActive(true);
        sword2.SetActive(true);

        float perlinX = (Mathf.PerlinNoise(Time.time * strikingSpeed, 0) - 0.5f) * perlinScale * 2;
        float perlinY = (Mathf.PerlinNoise(0, Time.time * strikingSpeed) - 0.5f) * perlinScale * 2;

        transform.position = startPos + new Vector2(perlinX, perlinY);
    }

    void Shoot()
    {
        Vector2 aimDirection = player.GetComponent<Rigidbody2D>().position - (Vector2)firePoint.position;

        aimDirection.Normalize();

        GameObject acidObject = Instantiate(acidPrefab, firePoint.position, Quaternion.identity);
        SoulBullet sBullet = acidObject.GetComponent<SoulBullet>();

        //float randPitch = Random.Range(-1f, 1f);
        //SFXShot.pitch = randPitch;
        //SFXShot.Play();

        PlayWithRandomPitch(SFXShot);

        sBullet.shoot(aimDirection, shootingForce, shotDamage);

        shootsDone++;
        //AcidShotController asController = acidObject.GetComponent<AcidShotController>();
        //asController.shoot(aimDirection, 10);
        //audioSource.PlayOneShot(acidClip);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check for a match with the specified name on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "BossWall" && currState != asmoState.Striking)
        {
            StartCoroutine(VFXForWallCollison());
        }
    }
        //private bool isPlayerInRange(float range)
        //{

        //    return Vector3.Distance(transform.position, player.transform.position) <= range;
        //}

    public void takeDamage(float damage)
    {
        health -= damage;

        if (health > 0)
        {
            //float randPitch = Random.Range(-1f,1f);
            //SFXTakeDamage.pitch = randPitch;
            //SFXTakeDamage.Play();

            PlayWithRandomPitch(SFXTakeDamage);

            asmoSprite.DOColor(new Color(0.4622642f,0.4622642f,0.4622642f), 0.2f).OnComplete(() =>
            {
                 asmoSprite.DOColor(Color.white, 0.1f).OnComplete(() =>
                 {
                     DOTween.KillAll(gameObject);
                 });
            });

        }
        else
        {
            SFXDie.Play();
            currState = asmoState.Dead;
        }
    }

    IEnumerator WaitStill(float seconds)
    {
        if (!dead)
        {
            VFX_Tired.Play();
            isCoroutineRunning = true;
            rb.velocity = Vector2.zero;

            yield return new WaitForSeconds(seconds);

            nextState = Random.Range(1, 4);
            ChangeState(nextState);
            VFX_Tired.Stop();
            isCoroutineRunning = false;
        }
        else
        {
            yield return new WaitForSeconds(0);
        }
    }

    IEnumerator StrikingCr(float seconds)
    {
        if (!dead)
        {
            isStrikingCrRunning = true;
            
            SFXScream.loop = true;
            SFXScream.Play();
            yield return new WaitForSeconds(seconds);

            currState = asmoState.Still;
            isStrikingCrRunning = false;
            sword1.SetActive(false);
            sword2.SetActive(false);
            SFXScream.loop = false;
        }
        else
        {
            yield return new WaitForSeconds(0);
        }
    }

    IEnumerator BulletHellCr(float seconds)
    {
        if (!dead)
        {
            isBulletHellCrRunning = true;
            SFXLaughter.Play();
            yield return new WaitForSeconds(seconds);

            currState = asmoState.Still;
            isBulletHellCrRunning = false;
        }
        else
        {
            yield return new WaitForSeconds(0);
        }
    }

    void ChangeState(int nextState)
    {
        switch (nextState)
        {
            case 1:
                currState = asmoState.ShootingFast;
                break;
            case 2:
                currState = asmoState.BulletHell;
                break;
            case 3:
                currState = asmoState.Striking;
                break;
            default:
                break;
        }        
    }


    IEnumerator WaitAndDie(float seconds)
    {
        if (!dead)
        {
            if (scoreBoard != null && chrono != null)
            {
                scoreBoard.GetPoints(100000);
            }

            Debug.Log("Se murio definitivamente");
            //GetComponent<BoxCollider2D>().size = new Vector2(0, 0);
            GetComponent<SpriteRenderer>().enabled = false;
            acidPrefab = null;
            deathVFX.gameObject.SetActive(true);
            
            yield return new WaitForSeconds(seconds);
            endCanva = true;
            yield return new WaitForSeconds(seconds);
            Destroy(gameObject);
        }
        else
        {
            yield return new WaitForSeconds(0);

        }
    }

    private IEnumerator VFXForWallCollison()
    {
        VFX_TpFrom.Play();
        VFX_TpTo.Play();
        yield return new WaitForSeconds(VFX_TpTo.main.duration);

        VFX_TpFrom.Stop();
        VFX_TpTo.Stop();
        gameObject.transform.position = spawnPoint.transform.position;
    }

    void PlayWithRandomPitch(AudioSource audioSource)
    {
        // Set a random pitch within the specified range
        float randomPitch = Random.Range(minPitch, maxPitch);

        // Set the AudioSource pitch
        audioSource.pitch = randomPitch;

        // Play the audio
        audioSource.Play();
    }
}
