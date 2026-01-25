using System.Collections.Generic;
using UnityEngine;

public class MageComboNormalAttack : MonoBehaviour
{
    private Animator ani;
    public int combo = 1;
    public int comboNumber = 3;

    public float comboTiming = 2f;
    public float comboTempo = 0f;

    private float minComboInterval = 0.5f;
    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    private int attackStaminaCost = 1;

    public bool isAttacking => ani.GetBool("isAttacking");

    public Transform attackPoint;
    public float attackRange = 0.75f;
    public LayerMask enemyLayer;

    private bool attackPressed = false;

    private int currentComboCache;

    private List<Collider2D> enemiesHitThisAttack;

    private Vector2 attackDirection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
