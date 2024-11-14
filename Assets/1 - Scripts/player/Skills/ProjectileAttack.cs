using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.EventSystems.EventTrigger;

public class ProjectileAttack : MonoBehaviour
{

    [Header("Radial Function Parameters")]
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject targetObj;
    [SerializeField] private ProjectileJoystick projectileJoysick;
    [SerializeField] public float radio = 5.0f;

    [Header("Projectile Atributes")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    public float proyectile_CD;

    bool isOnCd = false;

    public UnityEvent ShootOnRelease;
    [SerializeField] private AudioSource SFXProyectile;

    ProjectilePool projectilePool;
    PlayerInput_map _Input;
    SpriteRenderer _crossHair;
    Vector2 _Position;
    PlayerManager playerManager;

    private void Awake()
    {
        if (ProjectilePool.Instance == null)
        {
            Debug.LogError("No ProjectilePool founded");
        }
        _Input = new PlayerInput_map();
        _crossHair = targetObj.GetComponent<SpriteRenderer>();
        playerManager = player.GetComponent<PlayerManager>();
    }

    private void Update()
    {
        if (isOnCd == true)
        {
            targetObj.transform.position = player.transform.position;
            return;
        }

        _Position = projectileJoysick.JoystickInput;
        ChangeTargetPos(targetObj);

        if(projectileJoysick._isDragging == true)
        {
            _crossHair.gameObject.SetActive(true);
        }
        else
        {
            _crossHair.gameObject.SetActive(false);
        }
    }

    public Vector2 PositionInCircle(Vector2 direction)
    {
        float x = player.transform.position.x + radio * direction.x;
        float y = player.transform.position.y + radio * direction.y;
        return new Vector2(x, y);
    }

    private void OnEnable()
    {
        _Input.Enable();
        _Input.Player.ProjectileAttack.performed += OnProjectileAttack;
    }

    private void OnDisable()
    {
        _Input.Disable();
        _Input.Player.ProjectileAttack.performed -= OnProjectileAttack;
    }

    private void OnProjectileAttack(InputAction.CallbackContext obj)
    {

    }

    private void ChangeTargetPos(GameObject target)
    {
        Vector2 newPosInCircle = PositionInCircle(projectileJoysick.JoystickInput);
        //Debug.Log("New Position: " + newPosInCircle);
        Vector3 newPos = new Vector3(newPosInCircle.x, newPosInCircle.y);
        target.transform.position = newPos;
    }

    public void LaunchProjectile()
    {
        if (isOnCd == true)
            return;

        Vector2 targetPosition = targetObj.transform.position;
        // Calculate the direction from the fire point to the target
        Vector3 direction = new Vector3(targetPosition.x, targetPosition.y, 0) - firePoint.position;

        // Calculate the rotation needed for the projectile to look at the target
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);

        // Retrieve the projectile from the pool
        GameObject newProjectile = ProjectilePool.Instance.GetPooledProjectile();
        if (newProjectile != null)
        {
            newProjectile.transform.position = firePoint.position;
            newProjectile.transform.rotation = rotation;
            newProjectile.SetActive(true);

            Projectile projectileComponent = newProjectile.GetComponent<Projectile>();
            projectileComponent.range = playerManager.shotRange;
            projectileComponent.speed = playerManager.shotSpeed;
            Rigidbody2D rb = newProjectile.GetComponent<Rigidbody2D>();

            SFXProyectile.Play();

            // Optionally, you can add force or other behaviors to the projectile here
            if (rb != null)
            {
                // Apply a force to propel the projectile in the direction it is facing
                rb.velocity = Vector2.zero;
                rb.AddForce(newProjectile.transform.up * player.GetComponent<PlayerManager>().shotSpeed, ForceMode2D.Impulse);

                isOnCd = true;

                StartCoroutine(proyectileCD(proyectile_CD));
            }
            else
            {
                Debug.LogError("The projectile prefab must have a Rigidbody2D component.");
            }
        }

        else
        {
            Debug.Log("There wasn't any projectile on the pool");
        }

    }

    private IEnumerator proyectileCD(float waitTime)
    {
        // Wait for the specified time
        yield return new WaitForSeconds(waitTime);

        // After waiting, execute the method
        isOnCd = false;
    }
}
