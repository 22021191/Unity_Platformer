using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("component")]
    private Rigidbody2D _rb2d;
    private Animator Anim;
    private string ChangeAnimName;
    [SerializeField] private Vector3 Offset;

    [Header("Layers Masks")]
    [SerializeField] private LayerMask _GroundLayer;
    [SerializeField] private LayerMask _WallLayerMask;
    [SerializeField] private LayerMask _GrabLayerMask;

    [Header("Movement Variables")]
    [SerializeField] private float MaxSpeed;
    [SerializeField] private float Acceleration;
    [SerializeField] private float _groundLinearDrag = 7f;
    private bool _changingDirection => (_rb2d.velocity.x > 0f && horizontalDirection < 0f) || (_rb2d.velocity.x < 0f && horizontalDirection > 0f);
    private float horizontalDirection;
    private float verticalDirection;
    private bool FaceRight=true;

    [Header("JumpVariables")]
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _airLinearDrag = 2.5f;
    [SerializeField] private float _fallMultiplier = 5f;
    [SerializeField] private float _lowJumpFallMultiplier = 3f;
    [SerializeField] private float _downMultiplier = 7f;
    [SerializeField] private bool IsJumping;
    [SerializeField] private int extraJump = 0;
    private bool CanJump => IsJumping && (_onGround||extraJump>0||_onWall);

    [Header("Dash Variables")]
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashLength = .3f;
    [SerializeField] private float _dashBufferLength = .1f;
    private float _dashBufferCounter;
    private bool _isDashing;
    private bool _canDash => _dashBufferCounter > 0f;

    [Header("Wall Movement Variables")]
    [SerializeField] private float _wallSlideModifier = 0.5f;
    [SerializeField] private float _wallJumpXVelocityHaltDelay = 0.2f;
    [SerializeField] private float _wallRaycastLength;
    [SerializeField] private bool _onWall;
    private bool _onWallRight;

    [Header("Grab Variables")]
    [SerializeField] private bool _onGrab;
    [SerializeField] private bool climb;
    [SerializeField] private float SpeedOnGrab;
    [SerializeField] bool WallGrabCheck;
    [SerializeField] float _GrabRaycastLength;
    [SerializeField] private Vector3 GrabOffset;

    [Header("Ground Collision Variables")]
    [SerializeField] private float _groundRaycastLength;
    [SerializeField] private bool _onGround;
    // Start is called before the first frame update
    void Start()
    {
        _rb2d= GetComponent<Rigidbody2D>();
        Anim=GetComponent<Animator>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            IsJumping = true;
           
        }
        else IsJumping= false;
        if (Input.GetButtonDown("Dash")) _dashBufferCounter = _dashBufferLength;
        else _dashBufferCounter -= Time.deltaTime;
        horizontalDirection = GetInput().x;
        verticalDirection = GetInput().y;
        if(verticalDirection!=0&&_onGrab) climb= true;
    //}
    //private void FixedUpdate()
    //{
        if (_canDash) { StartCoroutine(Dash()); }
        if (!_isDashing)
        {
            CheckCollision();
            MoveCharacter();
            if (_onGround)
            {
                extraJump = 1;
                ApplyGroundLinearDrag();
                _rb2d.gravityScale = 1f;
            }
            else
            {
                if (_onWall&&_rb2d.velocity.y<0f)
                {
                    WallSlide();
                }
                FallMultiplier();
                ApplyAirLinearDrag();
            }
            if (CanJump)
            {
                if (!_onGround&&_onWall) {
                    
                    if (_onWallRight && horizontalDirection > 0f || !_onWallRight && horizontalDirection < 0f)
                    {
                        StartCoroutine(NeutralWallJump());
                    }
                    else
                    {
                        WallJump();
                    }
                    
                }
                else
                {
                    Jump(Vector2.up);
                }
                
            }
            WallRun();

        }
        
        Flip();
        UpdateAnimation();
    }
    private Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
    private void MoveCharacter()
    {
        _rb2d.AddForce(new Vector2(horizontalDirection, 0f) * Acceleration);

        if (Mathf.Abs(_rb2d.velocity.x) > MaxSpeed)
            _rb2d.velocity = new Vector2(Mathf.Sign(_rb2d.velocity.x) * MaxSpeed, _rb2d.velocity.y);
    }
    private void ApplyGroundLinearDrag()
    {
        if (Mathf.Abs(horizontalDirection) < 0.4f || _changingDirection)
        {
            _rb2d.drag = _groundLinearDrag;
        }
        else
        {
            _rb2d.drag = 0f;
        }
    }
    private void ApplyAirLinearDrag()
    {
        _rb2d.drag = _airLinearDrag;
    }
    private void Jump(Vector2 dir)
    {
        if(!_onGround&&!_onWall)
        {
            extraJump--;
        }
        _rb2d.velocity = new Vector2(_rb2d.velocity.x, 0f);
        _rb2d.AddForce(dir*_jumpForce,ForceMode2D.Impulse);
    }
    private void WallJump()
    {
        Vector2 jumpDirection = _onWallRight ? Vector2.left : Vector2.right;
        Jump(Vector2.up + jumpDirection);
    }
    private void WallSlide()
    {
        _rb2d.velocity = new Vector2(_rb2d.velocity.x, -MaxSpeed * _wallSlideModifier);
    }
    private void WallRun()
    {
        if (verticalDirection == 0) return;
        if (climb)
        {
            _rb2d.velocity = new Vector2(_rb2d.velocity.x, verticalDirection*SpeedOnGrab);
        }
    }
    IEnumerator NeutralWallJump()
    {
        Vector2 jumpDirection = _onWallRight ? Vector2.left : Vector2.right;
        Jump(Vector2.up + jumpDirection);
        yield return new WaitForSeconds(_wallJumpXVelocityHaltDelay);
        _rb2d.velocity = new Vector2(0f, _rb2d.velocity.y);
    }
    private void FallMultiplier()
    {
        if (verticalDirection < 0f)
        {
            _rb2d.gravityScale = _downMultiplier;
        }
        else
        {
            if (climb)
            {
                _rb2d.gravityScale = 0;
            }
            else
            {
                if (_onGrab&&!_onGround&&_onWallRight)
                {
                    _rb2d.gravityScale = 0;
                }
                else {
                    if (_rb2d.velocity.y < 0)
                    {
                        _rb2d.gravityScale = _fallMultiplier;
                    }
                    else
                    {
                        if (_rb2d.velocity.y > 0 && !Input.GetButton("Jump"))
                        {

                            _rb2d.gravityScale = _lowJumpFallMultiplier;
                        }
                        else
                        {
                            _rb2d.gravityScale = 1f;
                        }
                    }
                }
            }
        }
    }
    
    IEnumerator Dash()
    {
        float StartDash = Time.time;
        _isDashing = true;
        _rb2d.velocity = Vector2.zero;
        _rb2d.gravityScale = 0;
        _rb2d.drag = 0;

        Vector2 Dir;
        if (FaceRight) Dir = new Vector2(1f, 0f);
        else Dir = new Vector2(-1f, 0f);
        
        while (Time.time < StartDash + _dashLength)
        {
            _rb2d.velocity = Dir.normalized * _dashSpeed;
            yield return null;
        }

        _isDashing = false;

    }

    private void Flip()
    {
        bool ok = _onWall && (FaceRight && !_onWallRight || !FaceRight && _onWallRight);
        if((FaceRight&&horizontalDirection<0)|| (!FaceRight&&horizontalDirection>0))
        {
            FaceRight=!FaceRight;
            Vector3 scale = transform.localScale;
            Offset.x *= -1;
            scale.x = -scale.x;
            transform.localScale=scale;
        }
    }

    private void CheckCollision()
    {
        //Ground Check
        _onGround = Physics2D.Raycast(transform.position +Offset, Vector2.down, _groundRaycastLength, _GroundLayer);
        //Wall Check
        _onWall = Physics2D.Raycast(transform.position+Offset, Vector2.right, _wallRaycastLength, _WallLayerMask) ||
                   Physics2D.Raycast(transform.position + Offset, Vector2.left, _wallRaycastLength, _WallLayerMask);
        _onWallRight = Physics2D.Raycast(transform.position + Offset, Vector2.right, _wallRaycastLength, _WallLayerMask);
        WallGrabCheck = Physics2D.Raycast(transform.position + GrabOffset, Vector2.right, _GrabRaycastLength, _WallLayerMask);
       
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ladder"))
        {
            _onGrab = true;
        }
        
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ladder"))
        {
            _onGrab = false;
            climb = false;
        }
    }

    private void UpdateAnimation()
    {
        if(_isDashing)
        {
            ChangeAnim("Dash");
        }
        else
        {
            if (_onGround && horizontalDirection == 0)
            {
                ChangeAnim("Idle");
            }
            else
            {
                if (_onGround && horizontalDirection != 0)
                {
                    ChangeAnim("Run");
                }
                else
                {
                    if(climb)
                    {
                        ChangeAnim("EdgeGrab");
                    }
                    else
                    {
                        if (_rb2d.velocity.y > 0)
                        {
                            ChangeAnim("Jump");
                        }
                        if (_onWall)
                        {
                            if (_onGrab) ChangeAnim("WallGrab");
                            else ChangeAnim("WallSlice");
                        }
                        else if (_rb2d.velocity.y < 0)
                        {
                            ChangeAnim("Fall");
                        }
                    }

                }
            }
        }
        
    }

    private void ChangeAnim(string AnimName)
    {
        if(ChangeAnimName!= AnimName)
        {
            Anim.ResetTrigger(AnimName);
            ChangeAnimName= AnimName;
            Anim.SetTrigger(AnimName);
        }
    }
    private void OnDrawGizmos()
    {
        
        Gizmos.color = Color.green;
        //Ground
        Gizmos.DrawLine(transform.position+Offset, transform.position+Offset + Vector3.down * _groundRaycastLength);
        //Wall
        Gizmos.DrawLine(transform.position+Offset, transform.position+Offset + Vector3.right * _wallRaycastLength);
        Gizmos.DrawLine(transform.position+Offset, transform.position+Offset + Vector3.left * _wallRaycastLength);
        Gizmos.DrawLine(transform.position + GrabOffset, transform.position + GrabOffset + Vector3.right * _GrabRaycastLength);

    }

}
