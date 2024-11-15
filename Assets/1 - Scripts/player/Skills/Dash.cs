using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Dash : MonoBehaviour
{
    [SerializeField] private ParticleSystem smokePartSystem; 
    public Button dashButton;
    [SerializeField] private Image dashButtonBG;
    [SerializeField] private GameObject playerComponent;
    [SerializeField] private PlayerManager realPlayer;
    [SerializeField] private float dash_durarion;
    [SerializeField] private AudioSource SFXDash;
    private float original_speed;
    [SerializeField] private float dash_Speed;
    [SerializeField] private float dash_CD;
    private float dash_CDTimer;
    private bool usingDash = false;

    bool isOnCd = false;

    PlayerInput_map _Input;
    private CapsuleCollider2D capCollider;
    private SpriteRenderer playerSprite;

    public bool GetUsingDash()
    {
        return usingDash;
    }

    public float GetOriginalSpeed()
    {
        return original_speed;
    }

    public void Awake()
    {
        _Input = new PlayerInput_map();
        capCollider = GetComponent<CapsuleCollider2D>();
        playerSprite = GetComponent<SpriteRenderer>();
        dash_CDTimer = 0;
    }

    public void OnEnable()
    {
        _Input.Enable();
        _Input.Player.Dash.performed += OnDash;
    }

    private void OnDisable()
    {
        _Input.Disable();
        _Input.Player.Dash.performed -= OnDash;
    }

    private void Use_Dash()
    {
        if (isOnCd == true)
            return;
        usingDash = true;
        SFXDash.Play();
        dashButtonBG.fillAmount = 0f;
        smokePartSystem.Play();

        playerSprite.color = new Color(1, 1, 1, 0.2f);
        playerComponent.SetActive(true);
        playerComponent.transform.parent = null;

        original_speed = realPlayer.speed;

        if(realPlayer.speed < original_speed * dash_Speed)
        {
            realPlayer.speed = original_speed * dash_Speed;
        }

        isOnCd = true;

        StartCoroutine(DashCD(dash_CD));
    }

    private void Update()
    {
        if(isOnCd == true)
        {
            dash_CDTimer = dash_CDTimer + Time.deltaTime;
            dashButtonBG.fillAmount = (dash_CDTimer / dash_CD);
        }
    }

    private void RestoreParent()
    {
        playerComponent.transform.parent = gameObject.transform;
        playerComponent.transform.position = gameObject.transform.position;
        realPlayer.speed = original_speed;
        playerSprite.color = Color.white;
        playerComponent.SetActive(false);
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if(context.performed) { StartCoroutine(WaitAndExecute_Dash(dash_durarion)); }
    }

    public void OnDashMobile()
    {
        StartCoroutine(WaitAndExecute_Dash(dash_durarion));
    }

    private IEnumerator WaitAndExecute_Dash(float waitTime)
    {
        Use_Dash();
        capCollider.enabled = false;
        // Wait for the specified time
        yield return new WaitForSeconds(waitTime);

        // After waiting, execute the method
        RestoreParent();
        capCollider.enabled = true;
    }

    private IEnumerator DashCD(float waitTime)
    {       
        dashButton.interactable = false;
        
        // Wait for the specified time
        yield return new WaitForSeconds(waitTime);
        usingDash = false;
        // After waiting, execute the method
        isOnCd = false;
        dashButton.interactable = true;
        dashButtonBG.fillAmount = 1;
        dash_CDTimer = 0;
    }
}
