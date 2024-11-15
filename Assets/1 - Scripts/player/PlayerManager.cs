using DG.Tweening;
using Fungus;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Stats")]
    public float speed = 3;
    public float maxHealth = 10;
    public float health;
    public float maxDefense = 5;
    public float defense;
    public float attack = 3;
    public float shotSpeed = 1;
    public float shotRange = 10;
    public int shotDamage = 5;
    public float criticalRateUp = 0.6f;
    public float criticalDamage = 0.25f;
    public bool isInvincible = false;
    public float invincibleTimer;
    public float timeInvincible = 2.0f;

    [Header("Movement config.")]
    public PlayerTouchMovement _playerTouchMovementScript;
    public GameObject joystickCanva;
    public Vector2 _Movement;
    public Vector2 _DampedSpeed;
    public Rigidbody2D _Rigidbody;
    public Animator animator;
    
    [Header("Items and Statue attributes")]
    public Stack<Item> itemsHeld = new Stack<Item>();
    private bool itemNearBy;
    private Item _Item;
    private bool statueNearBy;
    private GameObject _Statue;
    private Item lastNearByItem;
    [SerializeField] private GameObject selecctionMark;
    [SerializeField] private Vector2 selectionOffset;

    [Header("UI")]
    [SerializeField] private itemsNotification _Notification;
    [SerializeField] private GameObject attackBtn;
    [SerializeField] private GameObject interactionBtn;
    [SerializeField] private Image blackScreen;
    
    [Header("Bars")]
    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private DefenseBar _defenseBar;

    
    [Header("Audio")]
    [SerializeField] private AudioSource SFXHit;
    [SerializeField] private AudioSource SFXArmorHit;
    [SerializeField] private AudioSource SFXPickUp;

    [Header("PartSystem")]
    [SerializeField] private ParticleSystem takeDamageVFX;
    [SerializeField] private ParticleSystem shieldTakeDamageVFX;
    [SerializeField] private ParticleSystem PlayerDeathVFX;

    private float lastDamageTime;
    private bool isRechargingDefense;
    private float rechargeStartTime;
    private float rechargeDuration = 5.0f;
    private float previousDefenseValue;
    private bool stopRechargeDefense;
    public float cdDefenseRegeneration = 10;

    public bool endCanva = false;

    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        health = maxHealth;
        defense = maxDefense;

        _healthBar.SetMaxHealth(maxHealth);
        _defenseBar.SetMaxDefense(maxDefense);

        _spriteRenderer = GetComponent<SpriteRenderer>();
        _Rigidbody = GetComponent<Rigidbody2D>();        

        isRechargingDefense = false;
        isInvincible = false;

        InvokeRepeating("TransparencyCheck", 1f, 3f);
    }
    
    private void Update()
    {
        if (isInvincible)
        {

            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0)
            {
                isInvincible = false;

            }
        }

        // Verifica si han pasado cdDefenseRegeneration segundos desde el último daño y la recarga no está en curso
        if (Time.time - lastDamageTime >= cdDefenseRegeneration && !isRechargingDefense)
        {
            previousDefenseValue = defense;
            isRechargingDefense = true;
            rechargeStartTime = Time.time;
        }

        // Restablece gradualmente la defensa durante la recarga
        if (isRechargingDefense)
        {
            float elapsedTime = Time.time - rechargeStartTime;

            if (elapsedTime < rechargeDuration)
            {
                // Incrementa gradualmente la defensa a su valor máximo durante la recarga
                defense = Mathf.Lerp(previousDefenseValue, maxDefense, elapsedTime / rechargeDuration);
                _defenseBar.SetDefense(defense);
                if (stopRechargeDefense)
                {
                    defense = Convert.ToUInt32(defense);
                    previousDefenseValue = defense;
                    isRechargingDefense = false;
                    stopRechargeDefense = false;
                }
            }
            else
            {
                // Finaliza la recarga cuando se alcanza la duración deseada
                isRechargingDefense = false;
                defense = maxDefense;
                _defenseBar.SetDefense(defense);
            }
        }

        _healthBar.SetHealth(health);
        _defenseBar.SetDefense(defense);
        AnimMovement();
       
    }

    private void AnimMovement()
    {
        
        if (_Rigidbody.velocity.normalized[1] >= 0.2f)
        {
            animator.SetFloat("Vertical",1);
            animator.SetFloat("Horizontal",0);
        }
        else if (_Rigidbody.velocity.normalized[1] <= -0.2f)
        {
            animator.SetFloat("Vertical",-1);
            animator.SetFloat("Horizontal",0);
        }
        else if (_Rigidbody.velocity.normalized[0] >= 0.2f)
        {
            animator.SetFloat("Horizontal",1);
            animator.SetFloat("Vertical",0);
        }
        else if (_Rigidbody.velocity.normalized[0] <= -0.2f)
        {
            animator.SetFloat("Horizontal",-1);
            animator.SetFloat("Vertical",0);
        }
        
        //Debug.Log(_Rigidbody.velocity.normalized);
        animator.SetFloat("Speed",_Rigidbody.velocity.sqrMagnitude);
    }
    
   

    

    private void OnMovement(InputAction.CallbackContext context)
    {
        _Movement = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        
        if (_playerTouchMovementScript.joystickActive == false)
        {
            _DampedSpeed = Vector2.SmoothDamp(_DampedSpeed, _Movement, ref _DampedSpeed, 0.05f);
            _Rigidbody.velocity = _DampedSpeed * speed;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible)
            return;

        isInvincible = true;
        invincibleTimer = timeInvincible;
        lastDamageTime = Time.time;

        if (isRechargingDefense)
        {
            stopRechargeDefense = true;
            defense = previousDefenseValue;
            _defenseBar.SetDefense(defense);
        }
        if (defense > 0)
        {
            SFXArmorHit.Play();
            shieldTakeDamageVFX.Play();

            defense-- ;

            float loopsTime = timeInvincible / 6;
            _spriteRenderer.DOColor(new Color(1, 1, 1, 0.3f), loopsTime).SetEase(Ease.InOutCubic).SetLoops(6).OnComplete(() =>
            {
                _spriteRenderer.DOColor(Color.white, 0.3f).SetEase(Ease.InOutCubic).OnComplete(() =>
                {
                    DOTween.Kill(gameObject);
                });
            });
        }
        else
        {
            health -= amount;

            SFXHit.Play();
            takeDamageVFX.Play();
            float loopsTime = timeInvincible / 6;
            _spriteRenderer.DOColor(new Color(1, 1, 1, 0.3f), loopsTime).SetEase(Ease.InOutCubic).SetLoops(6).OnComplete(() =>
            {
                _spriteRenderer.DOColor(Color.white, 0.3f).SetEase(Ease.InOutCubic).OnComplete(() =>
                {
                    DOTween.Kill(gameObject);
                });
            });
        }

        if (health <= 0)
        {
            Death();
        }        
    }

    public void HazardLava()
    {
        defense = 0f;
        isInvincible = true;
        invincibleTimer = timeInvincible;
        lastDamageTime = Time.time;
        if (isRechargingDefense)
        {
            stopRechargeDefense = true;
            previousDefenseValue = 0f;
            defense = previousDefenseValue;
            _defenseBar.SetDefense(defense);
        }
    }
    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Item"))
        {
            _Item = collision.GetComponent<Item>();

            itemNearBy = true;
            selecctionMark.SetActive(true);
            selecctionMark.transform.position = _Item.transform.position + (Vector3)selectionOffset;


            interactionBtn.SetActive(true);
            attackBtn.SetActive(false);

            _Notification.gameObject.SetActive(true);
            _Notification.DisplayNearByItem(_Item,false);
        }
        else if (collision != null && collision.CompareTag("Statue"))
        {
            statueNearBy = true;
            _Statue = collision.gameObject;
            if (!_Statue.GetComponent<ItemStatueSpawner>().isStatueUsed)
            {
                interactionBtn.SetActive(true);
                attackBtn.SetActive(false);
            }
            
        }
        else if (collision == null)
        {
            _Item = null;
            statueNearBy = false;
            attackBtn.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Item"))
        {
            _Item = collision.GetComponent<Item>();

            _Notification.DisplayNearByItem(_Item, true);

            itemNearBy = false;
            interactionBtn.SetActive(false);
            attackBtn.SetActive(true);

            selecctionMark.SetActive(false);
            selecctionMark.transform.position = gameObject.transform.position;
        }
        else if (collision != null && collision.CompareTag("Statue"))
        {
            statueNearBy = false;
            _Statue = null;

            interactionBtn.SetActive(false);
            attackBtn.SetActive(true);
        }
        else if(collision == null)
            _Item = null;
    }

    public void PickUp()
    {
        if (itemNearBy == true)
        {
            SFXPickUp.Play();

            speed += _Item.item_speed;

            if (health < maxHealth)
            {
                if ((_Item.item_health + health) <= maxHealth)
                {                    
                    health += _Item.item_health;
                }
                else
                {
                    health = maxHealth;
                }
            }
           
            maxHealth += _Item.item_maxHealth;
            _healthBar.SetMaxHealth(maxHealth);
            
            maxDefense += _Item.item_maxDefense;
            _defenseBar.SetMaxDefense(maxDefense);

            cdDefenseRegeneration += _Item.item_defense;
            attack += _Item.item_attack;
            shotDamage += _Item.item_shotDamage;
            shotSpeed += _Item.item_shotSpeed;
            shotRange += _Item.item_shotRange;
            
            criticalRateUp = Mathf.Clamp(criticalRateUp + _Item.item_criticalRateUp, 0f,1f);
            
            criticalDamage += _Item.item_criticalDamage;
            timeInvincible += _Item.item_timeInvincible;

            itemsHeld.Push(_Item);
        }

        if(itemNearBy == true)
        {
            Collider2D[] deleteItemsRadius = Physics2D.OverlapCircleAll(gameObject.transform.position, 10);
            if (deleteItemsRadius != null && deleteItemsRadius.Length > 0)
            {
                foreach (Collider2D collision in deleteItemsRadius)
                {
                    GameObject @object = collision.gameObject;
                    if (@object.CompareTag("Item"))
                    {
                        Destroy(@object);
                    }
                }
            }
            else
            {
                //Debug.Log("No Items to pick Up");
            }
        }
        
    }
    
    public void ActivateStatue()
    {
        if (statueNearBy == true && _Statue != null)
        {
            ItemStatueSpawner statueSpawner = _Statue.GetComponent<ItemStatueSpawner>();

            if (statueSpawner != null)
            {
                statueSpawner.DropItems();
            }
        }
    }

    public void Death()
    {
        joystickCanva.SetActive(false);
        _Rigidbody.velocity = Vector3.zero;
        _playerTouchMovementScript.enabled = false;
        PlayerDeathVFX.Play();
        blackScreen.gameObject.SetActive(true);
        _spriteRenderer.DOColor(Color.clear, 6f).SetEase(Ease.InCubic).OnComplete(() =>
        {
            blackScreen.DOColor(Color.black, 3f).OnComplete(() =>
            {
                PlayerDeathVFX.Stop();
                StartCoroutine(WaitAndChangeScene());
            });
        });

    }

    private IEnumerator WaitAndChangeScene()
    {
        DOTween.Kill(gameObject);
        yield return new WaitForSeconds(2f);

        endCanva = true;
        gameObject.SetActive(false);
        blackScreen.DOColor(Color.clear, 1.3f).OnComplete(() => { DOTween.Kill(gameObject); });
    }
    
    public float GetSpeed()
    {
        return speed;
    }
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    public float GetHealth()
    {
        return health;
    }
    public float GetMaxDefense()
    {
        return maxDefense;
    }
    public float GetDefense()
    {
        return defense;
    }
    public float GetAttack()
    {
        return attack;
    }
    public float GetShotSpeed()
    {
        return shotSpeed;
    }
    public float GetShotRange()
    {
        return shotRange;
    }
    public int GetShotDamage()
    {
        return shotDamage;
    }
    public float GetCriticalDamage()
    {
        return criticalDamage;
    }
    public float GetCriticalRateUp()
    {
        return criticalRateUp;
    }
    public float GetTimeInvincible()
    {
        return timeInvincible;
    }

    public void SetStats(float sp,float maxHth, float hth, float maxDf, float df, float atk, float shotSp, float shotRng, int shotDmg, float crtDmg, float crtRtUp, float tmInvin)
    {
        speed = sp;
        maxHealth = maxHth;
        health = hth;
        maxDefense = maxDf;
        defense = df;
        attack = atk;
        shotSpeed = shotSp;
        shotRange = shotRng;
        shotDamage = shotDmg;
        criticalDamage = crtDmg;
        criticalRateUp = crtRtUp;
        timeInvincible = tmInvin;
    }

    public void SwitchMovement(bool b)
    {
        if (b)
        {
            speed = 10;
        }
        else if (!b)
        {
            speed = 0;
        }
    }

    public void TransparencyCheck()
    {
        _spriteRenderer.color = Color.white;
    }
}
