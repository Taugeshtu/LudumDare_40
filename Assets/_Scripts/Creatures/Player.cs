using UnityEngine;
using System.Collections.Generic;

public class Player : CreatureBase {
	private enum State {
		Walking,
		Attacking,
		ChargingSplit,
		LandingSplit
		
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
	
	private State m_state;
	private float m_stateTimer;
	private bool m_canSplit = true;
	private Monster m_target;
	private bool m_spawned = false;
	
	protected override int _layerMask {
		get { return (1 << 8) + (1 << 9); }	// Note: because Player can actually walk on monsters
	}
	
	public int Kills;
	
#region Implementation
	void Update() {
		if( !m_spawned ) {
			return;
		}
		
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
				m_skeletool.Enqueue( "Split1", m_curve, TimingManager.ChargeTime );
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
		
		m_skeletool.Enqueue( "Split2", m_curve, TimingManager.SplitTime );
		
		var point = Vector3.zero;
		var direction = Vector3.zero;
		_GetCut( out point, out direction );
		
		if( m_iceberg != null ) {
			m_iceberg.Split( point, direction );
		}
	}
	
	private void _ProcessMovement() {
		var xAxis = Input.GetAxis( "Horizontal" );
		var yAxis = Input.GetAxis( "Vertical" );
		var moveDirection = (new Vector2( xAxis, yAxis )).X0Y();
		if( moveDirection.magnitude > 1f ) {
			moveDirection = moveDirection.normalized;
		}
		
		if( m_state != State.Walking ) {
			moveDirection = Vector3.zero;
		}
		
		_SetMoveDirection( moveDirection );
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
	
	protected override void _Ignite() {
		base._Ignite();
		
		var allChildren = GetComponentsInChildren<Transform>();
		foreach( var child in allChildren ) {
			child.gameObject.layer = 10;
		}
	}
	
	protected override void _Move() {
		base._Move();
		
		// TODO: plug in animations
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
	
	
#region Temporary
#endregion
}
