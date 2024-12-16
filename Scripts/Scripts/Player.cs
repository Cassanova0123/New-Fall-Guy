using UnityEngine;
using System.Collections.Generic;
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 7f;
    public float rotationSpeed = 10f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    public Transform groundCheck;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 moveDirection;
    private bool isSprinting;
    private bool isJumping;
    private Animator animator;



    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        // Pour v�rifier la surface
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        //Les inputs de contr�le du joueur 
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Pour sprinter
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        //Pour contr�ler le saut
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (animator != null) animator.SetTrigger("Jump");
        }


    }
     void FixedUpdate()
     {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 movement = moveDirection * currentSpeed;

        // Preserve vertical velocity (for jumping/falling)
        movement.y = rb.linearVelocity.y;

        rb.linearVelocity = movement;

        // Update animations if you have an animator
        if (animator != null)
        {
            animator.SetBool("IsMoving", moveDirection.magnitude > 0);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsSprinting", isSprinting);
        }
    }

}
