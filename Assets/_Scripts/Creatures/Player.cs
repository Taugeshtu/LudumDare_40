using UnityEngine;
using System.Collections.Generic;

public class Player : CreatureBase {
	private const float c_moveTrimStartTime = 0.3f;
	private const float c_moveTrimDuration = 0.5f;
	
	private enum State {
		Walking,
		Attacking,
		ChargingSplit,
		LandingSplit
	}
	
	private struct CastResult {
		public Vector3 PlanePoint;
		public Vector3? IcebergPoint;
		public Monster Monster;
		
		public Vector3 WalkTarget {
			get {
				if( Monster != null ) { return Monster.Position; }
				if( IcebergPoint.HasValue ) { return IcebergPoint.Value; }
				return PlanePoint;
			}
		}
	}
	
	[Header( "Ice-split" )]
	[SerializeField] private float m_splitReach = 1.5f;
	[SerializeField] private AttackIndicator m_attackUI;
	[SerializeField] private SliceIndicator m_splitUI;
	[SerializeField] private GameObject m_walkTargetUI;
	
	[SerializeField] private AudioSource m_source;
	[SerializeField] private AudioClip[] m_splitSounds;
	[SerializeField] private AudioClip[] m_attackSounds;
	
	[SerializeField] private Skeletool m_skeletool;
	[SerializeField] private AnimationCurve m_curve;
	[SerializeField] private AnimationCurve m_splitCurve;
	[SerializeField] private AnimationCurve m_splitCurve2;
	
	private State m_state;
	private float m_stateTimer;
	private bool m_canSplit = true;
	private Monster m_target;
	private bool m_spawned = false;
	
	private bool m_hadClick = false;
	private float m_clickStartTime;
	private Vector3? m_motionTarget;
	
	protected override int _layerMask {
		get { return (1 << Game.c_layerIceberg) + (1 << Game.c_layerCreature); }	// Note: because Player can actually walk on monsters
	}
	
	private bool _primaryKeyPressed {
		get { return (Input.GetAxis( "Fire1" ) > 0.1f); }
	}
	private bool _secondaryKeyPressed {
		get { return (Input.GetAxis( "Fire2" ) > 0.1f); }
	}
	
	public int Kills;
	
#region Implementation
	void Update() {
		if( !m_spawned ) {
			return;
		}
		
		var ray = Camera.main.ScreenPointToRay( Input.mousePosition );
		var castResult = _Cast( ray );
		
		_ProcessActions( castResult );
		_ProcessMovement( castResult );
		
		_SyncUI();
	}
#endregion
	
	
#region Public
	public void Spawn() {
		m_spawned = true;
		Kills = 0;
		
		transform.position = Vector3.up *5;
	}
	
	public void Despawn() {
		m_spawned = false;
	}
#endregion
	
	
#region Private
	protected override void _Ignite() {
		base._Ignite();
		
		var allChildren = GetComponentsInChildren<Transform>();
		foreach( var child in allChildren ) {
			child.gameObject.layer = Game.c_layerPlayer;
		}
	}
	
	protected override void _Move() {
		base._Move();
		
		// TODO: plug in animations
		
		if( m_motionTarget.HasValue ) {
			var positionDiff = (m_motionTarget.Value - transform.position).WithY( 0f );
			if( positionDiff.magnitude < 0.1f ) {
				m_motionTarget = null;
				_SetMoveDirection( Vector3.zero );
			}
		}
	}
	
	private void _ProcessActions( CastResult castResult ) {
		var attackKeyPressed = _primaryKeyPressed;
		var splitKeyPressed = _secondaryKeyPressed;
		
		if( m_canSplit == false ) {
			if( !splitKeyPressed ) {
				m_canSplit = true;
			}
		}
		
		if( m_state == State.Walking ) {
			if( splitKeyPressed && m_canSplit ) {
				m_state = State.ChargingSplit;
				m_stateTimer = Time.time + TimingManager.ChargeTime;
				m_skeletool.Enqueue( "Split1", m_splitCurve, TimingManager.ChargeTime );
			}
			
			// TODO: will have to rework that, since it doesn't play well with Diablo-movement
			/*
			if( attackKeyPressed ) {
				m_state = State.Attacking;
				m_stateTimer = Time.time + TimingManager.AttackTime;
				_OnAttack();
			}
			*/
		}
		
		if( Time.time > m_stateTimer ) {
			if( m_state == State.ChargingSplit ) {
				if( splitKeyPressed ) {
					m_canSplit = false;
					m_state = State.LandingSplit;
					m_stateTimer = Time.time + TimingManager.SplitTime;
					_OnSplitLanding();
				}
				else {
					m_state = State.Walking;
				}
			}
			else if( m_state == State.LandingSplit ) {
				// Note: Split ended
				m_state = State.Walking;
			}
			else if( m_state == State.Attacking ) {
				// Note: Attack ended
				_OnAttackLanding();
				m_state = State.Walking;
			}
		}
	}
	
	private void _ProcessMovement( CastResult castResult ) {
		if( m_state != State.Walking ) { return; }
		
		_SetMoveDirection( Vector3.zero );
		
		if( _primaryKeyPressed != m_hadClick ) {
			m_clickStartTime = Time.time;
		}
		m_hadClick = _primaryKeyPressed;
		
		var mouseMoveTime = Time.time - m_clickStartTime;
		var arrowMove = _GetArrowMove();
		if( arrowMove.sqrMagnitude.EpsilonEquals( 0f ) ) {
			if( _primaryKeyPressed ) {
				// Updating motion target
				m_motionTarget = castResult.WalkTarget;
				
				if( mouseMoveTime > c_moveTrimStartTime ) {
					var factor = Mathf.InverseLerp( c_moveTrimStartTime, c_moveTrimStartTime + c_moveTrimDuration, mouseMoveTime );
					var targetDiff = (castResult.WalkTarget - transform.position);
					var constrainedDiff = Vector3.Lerp( targetDiff, targetDiff.normalized, factor );
					m_motionTarget = constrainedDiff + transform.position;
				}
			}
			
			if( m_motionTarget.HasValue ) {
				_SetMoveDirection( (m_motionTarget.Value - transform.position).WithY( 0f ).normalized );
			}
		}
		else {
			m_motionTarget = null;
			_SetMoveDirection( arrowMove );
		}
	}
	
	private void _OnAttack() {
		m_skeletool.Enqueue( "Attack", m_curve, TimingManager.AttackTime );
	}
	
	private void _OnAttackLanding() {
		if( m_target != null ) {
			m_target.GetKilled();
			Kills += 1;
		}
		
		var clip = m_attackSounds[Random.Range( 0, m_attackSounds.Length )];
		m_source.PlayOneShot( clip );
		CameraShake.MakeAShake( false );
	}
	
	private void _OnSplitLanding() {
		// TODO: play animation!
		var clip = m_splitSounds[Random.Range( 0, m_splitSounds.Length )];
		m_source.PlayOneShot( clip );
		
		CameraShake.MakeAShake( true );
		
		m_skeletool.Enqueue( "Split2", m_curve, TimingManager.SplitTime /2 );
		m_skeletool.GoIdle( m_splitCurve2, TimingManager.SplitTime /2 );
		
		var point = Vector3.zero;
		var direction = Vector3.zero;
		_GetCut( out point, out direction );
		
		if( m_iceberg != null ) {
			m_iceberg.Split( point, direction );
		}
	}
	
	private void _SyncUI() {
		var isSplitting = (m_state == State.ChargingSplit) || (m_state == State.LandingSplit);
		
		m_attackUI.gameObject.SetActive( false );
		m_splitUI.gameObject.SetActive( isSplitting );
		
		m_walkTargetUI.SetActive( m_motionTarget.HasValue );
		if( m_motionTarget.HasValue ) {
			var ray = new Ray( m_motionTarget.Value + Vector3.up *10, Vector3.down );
			var castResult = _Cast( ray );
			Draw.Cross( castResult.WalkTarget, Palette.rose );
			m_walkTargetUI.transform.position = castResult.WalkTarget;
		}
	}
#endregion
	
	
#region Utility
	private CastResult _Cast( Ray ray ) {
		var result = new CastResult();
		
		var plane = new Plane( Vector3.up, transform.position );
		result.PlanePoint = plane.Cast( ray );
		
		var castMask = (1 << Game.c_layerIceberg) + (1 << Game.c_layerCreature);
		var hits = Physics.RaycastAll( ray, 100f, castMask );
		foreach( var hit in hits ) {
			var iceberg = hit.collider.GetComponent<Iceberg>();
			var monster = hit.collider.GetComponent<Monster>();
			
			if( iceberg != null ) {
				result.IcebergPoint = hit.point;
			}
			
			// TODO: collect multiple monsters, find the one closest to the point!
			if( monster != null ) {
				result.Monster = monster;
			}
		}
		
		return result;
	}
	
	private Vector3 _GetArrowMove() {
		var xAxis = Input.GetAxis( "Horizontal" );
		var yAxis = Input.GetAxis( "Vertical" );
		var moveDirection = (new Vector2( xAxis, yAxis )).X0Y();
		if( moveDirection.magnitude > 1f ) {
			moveDirection = moveDirection.normalized;
		}
		
		return moveDirection;
	}
	
	private void _GetCut( out Vector3 point, out Vector3 direction ) {
		var plane = new Plane( Vector3.up, transform.position );
		var ray = Camera.main.ScreenPointToRay( Input.mousePosition );
		point = plane.Cast( ray );
		var diff = point - transform.position;
		direction = (diff).normalized.XZ().X0Y();
		
		var newMag = Mathf.Clamp( direction.magnitude, 0.5f, m_splitReach );
		direction = direction.normalized *newMag;
		point = transform.position + direction;
	}
#endregion
}
