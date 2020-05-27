using UnityEngine;
using System;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class Player : CreatureBase {
	private const float c_moveTrimStartTime = 0.3f;
	private const float c_moveTrimDuration = 0.5f;
	private const float c_climbDistance = 1f;
	private const float c_playerHeight = 1.5f;
	
	private enum State {
		Walking,
		Attacking,
		ChargingSplit,
		LandingSplit,
		Falling,
		Climbing
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
	
	[Header( "Links" )]
	[SerializeField] private float m_splitReach = 1.5f;
	[SerializeField] private AttackIndicator m_attackUI;
	[SerializeField] private SliceIndicator m_splitUI;
	[SerializeField] private GameObject m_walkTargetUI;
	
	[Header( "Audio" )]
	[SerializeField] private AudioSource m_source;
	[SerializeField] private AudioClip[] m_splitSounds;
	[SerializeField] private AudioClip[] m_attackSounds;
	
	[Header( "Animations" )]
	[SerializeField] private Skeletool m_skeletool;
	[SerializeField] private AnimationCurve m_curve;
	[SerializeField] private AnimationCurve m_splitCurve;
	[SerializeField] private AnimationCurve m_splitCurve2;
	
	private State m_state;
	private float m_stateTimer;
	private Monster m_target;
	private bool m_spawned = false;
	
	private bool m_hadClick = false;
	private float m_clickStartTime;
	private Vector3? m_motionTarget;
	
	private bool m_wasInContact = false;
	private Vector3 m_climbPosition;
	private Vector3 m_climbStart;
	
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
	
	void OnDrawGizmos() {
		var index = 0;
		var previous = Vector3.zero;
		
		foreach( var position in m_positionsHistory ) {
			if( index != 0 ) {
				Draw.RayFromTo( previous, position, Palette.darkYellow );
			}
			
			index += 1;
			previous = position;
		}
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
		// fall determination
		if( m_state == State.Walking ) {
			if( m_isInContact != m_wasInContact ) {
				var ray = new Ray( transform.position, Vector3.down );
				var castResult = _Cast( ray );
				
				if( !m_isInContact && !castResult.IcebergPoint.HasValue ) {
					_SetState( State.Falling );
				}
			}
			
			m_wasInContact = m_isInContact;
		}
		
		// climbing motion
		if( m_state == State.Climbing ) {
			_ClimbMove();
		}
		
		// TODO: plug in animations
		
		if( m_motionTarget.HasValue ) {
			var positionDiff = (m_motionTarget.Value - transform.position).WithY( 0f );
			if( positionDiff.magnitude < 0.1f ) {
				m_motionTarget = null;
				_SetMoveDirection( Vector3.zero );
			}
		}
		
		base._Move();
	}
	
	protected override void _ChangeIceberg( Iceberg newIceberg ) {
		newIceberg.TransferDrift( Iceberg );
		
		base._ChangeIceberg( newIceberg );
	}
	
	private void _ProcessActions( CastResult castResult ) {
		var attackKeyPressed = _primaryKeyPressed;
		var splitKeyPressed = _secondaryKeyPressed;
		
		if( m_state == State.Walking ) {
			if( splitKeyPressed ) {
				_SetState( State.ChargingSplit );
			}
			
			// TODO: will have to rework that, since it doesn't play well with Diablo-movement
			/*
			if( attackKeyPressed ) {
				_SetState( State.Attacking );
			}
			*/
		}
		
		if( (m_state == State.ChargingSplit) && !splitKeyPressed ) {
			m_state = State.Walking;
		}
		
		if( Time.time > m_stateTimer ) {
			if( m_state == State.ChargingSplit ) {
				if( splitKeyPressed ) {
					_SetState( State.LandingSplit );
				}
			}
			else if( m_state == State.LandingSplit ) {
				// Note: Split ended
				m_state = State.Walking;
			}
			else if( m_state == State.Attacking ) {
				// Note: Attack ended
				_AttackLanded();
				m_state = State.Walking;
			}
			else if( m_state == State.Climbing ) {
				m_state = State.Walking;
			}
		}
		
		if( m_state == State.Falling ) {
			var verticalDiff = (m_climbPosition - transform.position).y;
			if( verticalDiff >= c_playerHeight ) {
				_SetState( State.Climbing );
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
	
	private void _SetState( State state ) {
		var stateTime = 0f;
		switch( state ) {
			case State.Attacking:
				stateTime = TimingManager.AttackTime;
				m_skeletool.Enqueue( "Attack", m_curve, TimingManager.AttackTime );
				break;
			case State.ChargingSplit:
				stateTime = TimingManager.ChargeTime;
				m_skeletool.Enqueue( "Split1", m_splitCurve, TimingManager.ChargeTime );
				break;
			case State.LandingSplit:
				stateTime = TimingManager.SplitTime;
				_StartSplit();
				break;
			case State.Falling:
				_StartFalling();
				break;
			case State.Climbing:
				stateTime = TimingManager.ClimbTime;
				m_climbStart = transform.position;
				break;
		}
		
		m_state = state;
		m_stateTimer = Time.time + stateTime;
		
		if( state != State.Walking ) {
			m_motionTarget = null;
			_SetMoveDirection( Vector3.zero );
		}
	}
	
	private void _AttackLanded() {
		if( m_target != null ) {
			m_target.GetKilled();
			Kills += 1;
		}
		
		var clip = m_attackSounds[Random.Range( 0, m_attackSounds.Length )];
		m_source.PlayOneShot( clip );
		CameraShake.MakeAShake( false );
	}
	
	private void _StartSplit() {
		var clip = m_splitSounds[Random.Range( 0, m_splitSounds.Length )];
		m_source.PlayOneShot( clip );
		
		CameraShake.MakeAShake( true );
		
		m_skeletool.Enqueue( "Split2", m_curve, TimingManager.SplitTime /2 );
		m_skeletool.GoIdle( m_splitCurve2, TimingManager.SplitTime /2 );
		
		var cut = _GetCut();
		if( Iceberg != null ) {
			Iceberg.Split( cut.Item1, cut.Item2 );
		}
	}
	
	private void _StartFalling() {
		var flatBackNormal = (-m_lastHit.normal).WithY( 0f ).normalized;
		m_climbPosition = transform.position + flatBackNormal *c_climbDistance;
		
		var ray = new Ray( m_climbPosition + Vector3.up *3, Vector3.down );
		var castResult = _Cast( ray );
		
		if( castResult.IcebergPoint.HasValue ) {
			m_climbPosition = castResult.IcebergPoint.Value;
		}
	}
	
	private void _ClimbMove() {
		var diff = (m_climbPosition - m_climbStart);
		var verticalComponent = diff.ProjectedOn( Vector3.up );
		var flatComponent = diff.Flat( Vector3.up );
		var totalTravelLength = verticalComponent.magnitude + flatComponent.magnitude;
		
		var breakPoint = verticalComponent.magnitude /totalTravelLength;
		
		var timeFactor = 1f - (m_stateTimer - Time.time) /TimingManager.ClimbTime;
		if( timeFactor < breakPoint ) {
			var localFactor = Mathf.InverseLerp( 0f, breakPoint, timeFactor );
			transform.position = Vector3.Lerp( m_climbStart, m_climbStart + verticalComponent, localFactor );
			
			Debug.LogError( "Climbing vertical, local factor: "+localFactor );
			Draw.RayFromToCross( m_climbStart, m_climbStart + verticalComponent, Palette.red, 0.5f );
		}
		else {
			var localFactor = Mathf.InverseLerp( breakPoint, 1f, timeFactor );
			transform.position = Vector3.Lerp( m_climbStart + verticalComponent, m_climbPosition, localFactor );
			
			Debug.LogError( "Climbing horizontal, local factor: "+localFactor );
			Draw.RayFromToCross( m_climbStart + verticalComponent, m_climbPosition, Palette.blue, 0.5f );
		}
	}
	
	private void _SyncUI() {
		m_attackUI.gameObject.SetActive( false );
		
		var isSplitting = (m_state == State.ChargingSplit) || (m_state == State.LandingSplit);
		var cut = _GetCut();
		m_splitUI.gameObject.SetActive( isSplitting );
		if( m_state == State.ChargingSplit ) {
			m_splitUI.Position( cut.Item1, cut.Item2 );
		}
		
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
	private CastResult _Cast( Ray ray, float distance = 100f ) {
		var result = new CastResult();
		
		var plane = new Plane( Vector3.up, transform.position );
		result.PlanePoint = plane.Cast( ray );
		
		var castMask = (1 << Game.c_layerIceberg) + (1 << Game.c_layerCreature);
		var hits = Physics.RaycastAll( ray, distance, castMask );
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
	
	private ValueTuple<Vector3, Vector3> _GetCut() {
		var plane = new Plane( Vector3.up, transform.position );
		var ray = Camera.main.ScreenPointToRay( Input.mousePosition );
		var point = plane.Cast( ray );
		var diff = point - transform.position;
		var direction = (diff).normalized.XZ().X0Y();
		
		var newMag = Mathf.Clamp( direction.magnitude, 0.5f, m_splitReach );
		direction = direction.normalized *newMag;
		point = transform.position + direction;
		return new ValueTuple<Vector3, Vector3>( point, direction );
	}
#endregion
}
