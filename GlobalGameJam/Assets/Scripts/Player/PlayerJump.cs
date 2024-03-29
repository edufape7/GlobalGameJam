using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))] [RequireComponent(typeof(PlayerGroundedManager))]
public class PlayerJump : MonoBehaviour
{
    [Header("Ground Jump")] 
    [SerializeField] private float _maxGroundJumpHeight;
    [SerializeField] private float _minGroundJumpHeight;
    [SerializeField] private float _timeToReachGroundJumpApexInSeconds;
    
    [Header("Double Jump")]
    [SerializeField] private float _maxDoubleJumpHeight;
    [SerializeField] private float _minDoubleJumpHeight;

    [Header("Balancing")] 
    [SerializeField] private float _coyoteTimeInFrames;
    [SerializeField] private int _inputGraceTimeInFrames;

    private Rigidbody2D _rb2d;
    private PlayerGroundedManager _playerGroundedManager;

    private float _groundJumpVelocity;
    private float _doubleJumpVelocity;
    private float _terminatedGroundJumpVelocity;
    private float _terminatedDoubleJumpVelocity;

    private Coroutine _coyoteTimeCoroutine;
    private bool _coyoteTimeExpired;
    private bool _hasCoyoteTime;

    private bool _groundJumpTerminated;
    private bool _doubleJumpTerminated;
    private bool _canGroundJump;
    private bool _canDoubleJump;

    private void Start()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        _playerGroundedManager = GetComponent<PlayerGroundedManager>();

        var gravityScale = 2 * _maxGroundJumpHeight / (_timeToReachGroundJumpApexInSeconds * _timeToReachGroundJumpApexInSeconds);
        _rb2d.gravityScale = gravityScale;
        
        _groundJumpVelocity = Mathf.Sqrt(2 * gravityScale * _maxGroundJumpHeight);
        _terminatedGroundJumpVelocity = Mathf.Sqrt(_groundJumpVelocity * _groundJumpVelocity + 2 * -gravityScale * (_maxGroundJumpHeight - _minGroundJumpHeight));

        _doubleJumpVelocity = Mathf.Sqrt(2 * gravityScale * _maxDoubleJumpHeight);
        _terminatedDoubleJumpVelocity = Mathf.Sqrt(_doubleJumpVelocity * _doubleJumpVelocity + 2 * -gravityScale * (_maxDoubleJumpHeight - _minDoubleJumpHeight));
        
        Debug.Log(gravityScale);
        Debug.Log(_groundJumpVelocity);
        Debug.Log(_terminatedGroundJumpVelocity);
    }

    public void JumpViaInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (_canGroundJump)
            {
                var jumpImpulseForce = _rb2d.mass * (_groundJumpVelocity / Time.fixedDeltaTime);
                
                _rb2d.AddForce(jumpImpulseForce * transform.TransformDirection(Vector3.up));
                _hasCoyoteTime = false;
                if(_coyoteTimeCoroutine != null) StopCoroutine(_coyoteTimeCoroutine);
            }
            
            else if (_canDoubleJump)
            {
                _rb2d.velocity = new Vector2(_rb2d.velocity.x, _doubleJumpVelocity);
                _canDoubleJump = false;
            }
            
            else StartCoroutine(InputGraceTime());
        }
        
        else if (context.canceled)
        {
            if (!_groundJumpTerminated)
            {
                var velocityY = Mathf.Min(_rb2d.velocity.y, _terminatedGroundJumpVelocity);
                _rb2d.velocity = new Vector2(_rb2d.velocity.x, velocityY);
                _groundJumpTerminated = true;
            }
            
            else if (!_doubleJumpTerminated)
            {
                var velocityY = Mathf.Min(_rb2d.velocity.y, _terminatedDoubleJumpVelocity);
                _rb2d.velocity = new Vector2(_rb2d.velocity.x, velocityY);
                _doubleJumpTerminated = true;
            }
        }
    }

    private IEnumerator InputGraceTime()
    {
        for (var i = 0; i < _inputGraceTimeInFrames; i++)
        {
            if (_playerGroundedManager.IsGrounded)
            {
                JumpViaGraceTime();
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private void JumpViaGraceTime()
    {
        _rb2d.velocity = new Vector2(_rb2d.velocity.x, _groundJumpVelocity);
    }

    private void FixedUpdate()
    {
        if (_playerGroundedManager.IsGrounded)
        {
            _groundJumpTerminated = false;
            _doubleJumpTerminated = false;
            
            _canGroundJump = true;
            _canDoubleJump = true;
            
            _coyoteTimeExpired = false;
            _hasCoyoteTime = true;
        }
        
        else
        {
            if (_coyoteTimeCoroutine == null && !_coyoteTimeExpired && _hasCoyoteTime)
            {
                _coyoteTimeCoroutine = StartCoroutine(CoyoteTime());
            }

            else
            {
                _canGroundJump = false;
            }
        }
    }

    private IEnumerator CoyoteTime()
    {
        for (var i = 0; i < _coyoteTimeInFrames; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        _canGroundJump = false;
        _coyoteTimeExpired = true;
    }
}
