using UnityEngine;
using System.Collections.Generic;

public class Player : CreatureBase {
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
	
	private Vector3? m_motionTarget;
	
	protected override int _layerMask {
		get { return (1 << Game.c_layerIceberg) + (1 << Game.c_layerCreature); }	// Note: because Player can actually walk on monsters
	}
	
	public int Kills;
	
#region Implementation
	void Update() {
		if( !m_spawned ) {
			return;
		}
		
		// new logic:
		// 1. Get iceberg hit
		// 2. If the hit is
		
		var castResult = _Cast();
		var attackKeyPressed = (Input.GetAxis( "Fire1" ) > 0.1f);
		
		if( m_state == State.Walking ) {
			_ProcessMovement( castResult, attackKeyPressed );
		}
		
		/*
		_ProcessKeys();
		_ProcessMovement();
		
		var point = Vector3.zero;
		var direction = Vector3.zero;
		_GetCut( out point, out direction );
		
		m_attackUI.gameObject.SetActive( false );
		m_splitUI.gameObject.SetActive( false );
		if( m_state == State.Walking ) {
			m_target = _SeekTarget();
			if( m_target != null ) {
				point = m_target.transform.position;
				direction = m_target.transform.forward;
				
				m_attackUI.gameObject.SetActive( true );
				m_attackUI.Position( point, direction );
			}
			else {
				m_splitUI.gameObject.SetActive( m_canSplit );
				m_splitUI.Position( point, direction );
			}
		}
		else if( m_state == State.ChargingSplit ) {
			m_splitUI.gameObject.SetActive( m_canSplit );
		}
		*/
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
	
	private CastResult _Cast() {
		var result = new CastResult();
		
		var ray = Camera.main.ScreenPointToRay( Input.mousePosition );
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
	
	private void _ProcessMovement( CastResult castResult, bool attackPressed ) {
		var arrowMove = _GetArrowMove();
		
		if( arrowMove.sqrMagnitude.EpsilonEquals( 0f ) ) {
			if( attackPressed ) {
				m_motionTarget = castResult.WalkTarget;
			}
			
			if( m_motionTarget.HasValue ) {
				var moveDirection = (m_motionTarget.Value - transform.position).WithY( 0f );
				_SetMoveDirection( moveDirection.normalized );
			}
		}
		else {
			m_motionTarget = null;
			_SetMoveDirection( arrowMove );
		}
	}
#endregion
	
	
#region Temporary
	private void _ProcessKeys() {
		var attackKeyPressed = (Input.GetAxis( "Fire1" ) > 0.1f);
		var splitKeyPressed = (Input.GetAxis( "Fire2" ) > 0.1f);
		
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
			
			if( attackKeyPressed ) {
				m_state = State.Attacking;
				m_stateTimer = Time.time + TimingManager.AttackTime;
				_OnAttack();
			}
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
	
	private Monster _SeekTarget() {
		var point = Vector3.zero;
		var direction = Vector3.zero;
		_GetCut( out point, out direction );
		
		Monster found = null;
		var minDistance = float.MaxValue;
		foreach( var monster in m_iceberg.Monsters ) {
			if( monster.IsAlive ) {
				var diffToMonster = (monster.transform.position - point);
				if( diffToMonster.magnitude < minDistance ) {
					found = monster;
					minDistance = diffToMonster.magnitude;
				}
			}
		}
		
		if( minDistance > 2f ) {
			found = null;
		}
		return found;
	}
#endregion
}
