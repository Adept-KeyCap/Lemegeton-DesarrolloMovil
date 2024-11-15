using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public Transform playerTransform; // Reference to the player's transform
    public float crosshairDistance = 2.0f; // Distance from the player where the crosshair appears
    private Rigidbody2D rb;
    private TutorialManager tutorialManager;

    public Vector2 lastAimDirection = Vector2.zero;

    [Header("Combat")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private SpriteRenderer meleeSprite;
    [SerializeField] private float combat_CD;
    [SerializeField] private float meleeRadius;
    [SerializeField] private float targetLockRadius;
    [SerializeField] public float radio = 3.0f;
    [SerializeField] public float transition = 3.0f;

    [Header("SFX Config.")]
    [SerializeField] private AudioSource SFXAttack, SFXCrit;
    //[SerializeField] private AudioSource SFXBelhorHit;
    //[SerializeField] private AudioSource SFXAndrasHit;
    private float combat_CDTimer;

    [Header("Other")]
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private ParticleSystem slashVFX;
    [SerializeField] private ParticleSystem critSlashVFX;

    private float meleeCriticalDamage;
    private float meleeCritialRateUp;
    private float meleeDamage;
    private Vector2 currentAimDirection;

    bool isOnCd = false;

    //[SerializeField] private FloatingJoystick Joystick;

    [Header("Button Config.")]
    [SerializeField] private Button button;
    [SerializeField] private Image buttonFill;



    private void Start()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (UnityEngine.Object.FindAnyObjectByType<TutorialManager>() != null)
        {
            tutorialManager = UnityEngine.Object.FindAnyObjectByType<TutorialManager>();
        }
    }

    private void FixedUpdate()
    {
        if (isOnCd == true)
        {
            combat_CDTimer = combat_CDTimer + Time.deltaTime;
            buttonFill.fillAmount = combat_CDTimer / combat_CD;
        }

        meleeDamage = _playerManager.attack;
        meleeCriticalDamage = _playerManager.criticalDamage;
        meleeCritialRateUp = _playerManager.criticalRateUp;
        Vector2 closestEnemyDir = GetDirectionToClosestEnemy();

        // Ensure the playerTransform is set (you can also assign it manually in the Inspector)
        if (playerTransform == null)
        {
            Debug.LogError("Player transform is not set for the Crosshair.");
            return;
        }

        // Use the player's velocity directly for movement
        Vector2 playerVelocity = rb.velocity.normalized*1.2f;

        if (closestEnemyDir != Vector2.zero)
        {
            meleeSprite.DOColor(Color.red, 1f).OnComplete(() => {

                DOTween.Kill(gameObject);

            });
            gameObject.GetComponent<SpriteRenderer>().color = Color.red;
            // Apply smoothing only when enemies are nearby
            float smoothFactor = 3.0f; // You can adjust this value as needed

            // Smoothing for position
            Vector2 playerPosition = playerTransform.position;
            Vector2 targetPosition = playerPosition + closestEnemyDir.normalized * crosshairDistance;
            transform.position = Vector2.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * smoothFactor);

            // Smoothing for rotation
            float angle = Mathf.Atan2(closestEnemyDir.y, closestEnemyDir.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle - 90);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * smoothFactor);
            slashVFX.gameObject.transform.rotation = transform.rotation;
            critSlashVFX.gameObject.transform.rotation = transform.rotation;

            currentAimDirection = closestEnemyDir;
        }
        else
        {
            // No enemies nearby, use player's velocity for movement
            meleeSprite.DOColor(Color.white, 1f).OnComplete(() => {

                DOTween.Kill(gameObject);
                
            });
            currentAimDirection = playerVelocity;

            if(currentAimDirection != lastAimDirection && currentAimDirection != Vector2.zero)
            {
                lastAimDirection = currentAimDirection;

                // Position the crosshair object based on the player's aim (no smoothing)
                Vector2 playerPosition = playerTransform.position;
                Vector2 crosshairPosition = playerPosition + playerVelocity * crosshairDistance;
                transform.position = crosshairPosition;

                // Rotate the crosshair object based on the player's aim (no smoothing)
                float angle = Mathf.Atan2(playerVelocity.y, playerVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle - 90);
                slashVFX.gameObject.transform.rotation = transform.rotation;
                critSlashVFX.gameObject.transform.rotation = transform.rotation;

            }

        }

    }


    public void Melee()
    {
        if (isOnCd == true)
            return;

        StartCoroutine(AnimationPlaceholder(0.2f));


        buttonFill.fillAmount = 0;
        float randomValue = Random.Range(0f, 1f);
    
        // Comprueba si se produce un ataque crítico.
        if (randomValue < meleeCritialRateUp)//If critical damage
        {
            SFXCrit.Play();
            critSlashVFX.Play();

            meleeDamage = CalculateCriticalDamage(_playerManager.attack,_playerManager.criticalDamage);
            Debug.Log("¡Ataque crítico! Daño total: " + meleeDamage);
        }
        else //If there's no critical damage
        {
            SFXAttack.Play();
            slashVFX.Play();

            meleeDamage = _playerManager.attack;
            Debug.Log("Ataque normal. Daño total: " + meleeDamage);
        }
        Collider2D[] radius = Physics2D.OverlapCircleAll(gameObject.transform.position, meleeRadius);

        foreach (Collider2D collision in radius)
        {
            if (collision.CompareTag("Enemy"))
            {
                //SFXBelhorHit.Play();
                collision.transform.GetComponent<IncubusScript>().takeDamage(meleeDamage);
                if(collision.transform.GetComponent<IncubusScript>().Health <= 0 && tutorialManager != null)
                {
                    tutorialManager.currentEnemies++;
                }
            }

            if (collision.CompareTag("EnemySoul"))
            {
                if(collision.name == "Asmodeus")
                {
                    collision.gameObject.GetComponent<AsmodeusScript>().takeDamage(meleeDamage);
                }
                //SFXBelhorHit.Play();
                collision.gameObject.GetComponent<LostSoulScript>().takeDamage(meleeDamage);
                if (collision.transform.GetComponent<LostSoulScript>().health <= 0 && tutorialManager != null)
                {
                    tutorialManager.currentEnemies++;
                }
            }

            if (collision.CompareTag("EnemyAndras"))
            {
                //SFXAndrasHit.Play();
                collision.gameObject.GetComponent<AndrasScript>().takeDamage(meleeDamage);
                if (collision.transform.GetComponent<AndrasScript>().health <= 0 && tutorialManager != null)
                {
                    tutorialManager.currentEnemies++;
                }
            }
        }

        isOnCd = true;

        StartCoroutine(CombatCD(combat_CD));
    }
    private float CalculateCriticalDamage(float baseAttack, float criticalDmg)
    {
        float criticalDamage = baseAttack*1.5f + (baseAttack * criticalDmg);
        return criticalDamage;
    }
    public Vector2 GetDirectionToClosestEnemy()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerManager.gameObject.transform.position, targetLockRadius, enemyLayers);
        Vector2 closestEnemyDirection = Vector2.zero;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D collider in colliders)
        {

            GameObject enemy = collider.gameObject;
            Vector2 directionToEnemy = enemy.transform.position - transform.position;
            float distanceToEnemy = directionToEnemy.magnitude;

            // Add a minimum distance threshold to avoid rapid switching
            if (distanceToEnemy < closestDistance && distanceToEnemy > 0.1f)
            {
                closestDistance = distanceToEnemy;
                closestEnemyDirection = directionToEnemy.normalized;
            }

        }

        return closestEnemyDirection;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(gameObject.transform.position, meleeRadius);
        Gizmos.DrawWireSphere(_playerManager.gameObject.transform.position, targetLockRadius);
    }

    private IEnumerator AnimationPlaceholder(float waitTime)
    {
        //meleeSprite.enabled = true;

        // Wait for the specified time
        yield return new WaitForSeconds(waitTime);

        //meleeSprite.enabled = false;
    }

    private IEnumerator CombatCD(float waitTime)
    {
        button.interactable = false;

        // Wait for the specified time
        yield return new WaitForSeconds(waitTime);

        // After waiting, execute the method
        isOnCd = false;
        button.interactable = true;
        buttonFill.fillAmount = 1;
        combat_CDTimer = 0;
    }

    public void UpdateLastAimDirection(Vector2 newDirection)
    {
        lastAimDirection = newDirection;
        //Debug.Log("Updating lasAimPos");
    }

}
