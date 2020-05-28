using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class CreatureBase : IcebergEntity {
	private static RaycastHit[] s_hits = new RaycastHit[5];
	
	[Header( "Movement" )]
	[SerializeField] protected float m_speed = 4f;
	[SerializeField] protected float m_acceleration = 0.5f;
	[SerializeField] protected float m_deacceleration = 0.9f;
	
	[Header( "Physics" )]
	[SerializeField] private float m_castRadius = 0.2f;
	[SerializeField] private float m_castDepth = 1f;
	
	[Header( "Misc" )]
	[SerializeField] private bool m_shouldDraw = false;
	[SerializeField] private int m_historyDepth = 35;
	
	private Vector3 m_lastRecordedPosition;
	protected Queue<Vector3> m_positionsHistory = new Queue<Vector3>();
	protected bool m_isInContact;
	protected RaycastHit m_lastHit;
	private Vector3 m_moveDirection;
	private Jump? m_jump;
	
	private Rigidbody m_rigid;
	protected Rigidbody _rigidbody {
		get {
			if( m_rigid == null ) { m_rigid = GetComponent<Rigidbody>(); }
			return m_rigid;
		}
	}
	
	protected abstract int _layerMask { get; }
	protected abstract bool _canJump { get; }
	
	private int _icebergMask {
		get { return (1 << Game.c_layerIceberg); }
	}
	
	public bool IsAlive { get; private set; }
	public Vector3 MoveDirection { get { return m_moveDirection; } }
	
#region Implementation
	void Awake() {
		_Ignite();
	}
	
	void FixedUpdate() {
		_ProcessContact();	// Note: not sure if putting it before Move() will break things or not
		
		if( m_jump == null ) {
			m_jump = _FindJump();
		}
		
		if( m_jump == null ) {
			_Move();
		}
		else {
			_ExecuteJump();
		}
		
		if( m_positionsHistory.Count == 0 ) {
			m_positionsHistory.Enqueue( transform.position );
			m_lastRecordedPosition = transform.position;
		}
		else {
			if( (transform.position - m_lastRecordedPosition).magnitude > 0.25f ) {
				m_positionsHistory.Enqueue( transform.position );
				m_lastRecordedPosition = transform.position;
			}
		}
		
		if( m_positionsHistory.Count > m_historyDepth ) {
			m_positionsHistory.Dequeue();
		}
		
		_UpdateTurn();
	}
#endregion
	
	
#region Public
	public virtual void GetKilled() {
		IsAlive = false;
		// TODO: particle explosion!
		// Debug.LogError( "Killed" );
	}
#endregion
	
	
#region Private
	private void _ProcessContact() {
		m_isInContact = false;
		
		var ray = new Ray( transform.position + transform.up *m_castDepth, -transform.up );
		var hitsCount = Physics.SphereCastNonAlloc( ray, m_castRadius, s_hits, m_castDepth, _layerMask );
		if( hitsCount > 0 ) {
			var closestHit = _GetClosestHit( ray, s_hits, hitsCount );
			var icebergHit = (_layerMask == _icebergMask) ? closestHit : _GetIcebergHit( ray, s_hits, hitsCount );
			
			var vertical = Vector3.Project( _rigidbody.velocity, Vector3.up );
			_rigidbody.AddForce( -vertical, ForceMode.VelocityChange );
			transform.position = transform.position.WithY( closestHit.point.y );
			
			m_isInContact = true;
			m_lastHit = closestHit;
			
			if( icebergHit.collider != null ) {
				var newIceberg = icebergHit.collider.GetComponentInParent<Iceberg>();
				if( newIceberg != Iceberg ) {
					_ChangeIceberg( newIceberg );
				}
			}
		}
		else {
			if( IsAlive ) {
				_rigidbody.AddForce( Physics.gravity, ForceMode.Acceleration );
			}
		}
	}
	
	private Jump? _FindJump() {
		if( !m_isInContact ) {
			return null;
		}
		
		Physics.queriesHitBackfaces = true;
		
		var maxJumpDistance = 5f;
		var castDirection = m_moveDirection.normalized;
		var maxJumpHeight = 1f;
		var maxDrop = 2f;
		
		var planeRay = new Ray( m_lastHit.point - Vector3.up *0.2f, castDirection );
		var hitsCount = Physics.RaycastNonAlloc( planeRay, s_hits, maxJumpDistance, _layerMask );
		
		// to determine siding just use Vector3.Dot() against hit normal. Positive == backface
		RaycastHit? backface = null;
		RaycastHit? frontface = null;
		// Note: potential problems space with not ordered hits and shit
		for( var i = 0; i < hitsCount; i++ ) {
			var hit = s_hits[i];
			if( hit.normal.Dot( castDirection ) < 0 ) {
				frontface = hit;
			}
			else {
				backface = hit;
			}
		}
		
		if( frontface.HasValue ) {
			var verticalRay = new Ray( frontface.Value.point + castDirection *0.2f + Vector3.up *maxJumpHeight, Vector3.down );
			RaycastHit verticalHit;
			if( Physics.Raycast( verticalRay, out verticalHit, maxJumpHeight + maxDrop, _layerMask ) ) {
				return new Jump( transform.position, verticalHit.point, m_speed *0.2f );
			}
		}
		
		Physics.queriesHitBackfaces = false;
		
		return null;
	}
	
	private void _ExecuteJump() {
		var jumpPosition = m_jump.Value.CalculatePosition();
		
		// Eeeehhh?.. Either jump is defined as too short, or it doesn't progress far enough.
		// Might also do with defining jump as local to transform, since drift is a bitch
		
		if( jumpPosition.HasValue ) {
			_rigidbody.isKinematic = true;
			transform.position = jumpPosition.Value;
			Debug.LogError( "Jump exec!" );
		}
		else {
			m_jump = null;
			_rigidbody.isKinematic = false;
			Debug.LogError( "Jump done!" );
		}
	}
	
	private void _UpdateTurn() {
		var totalDiff = Vector2.zero;
		var index = 0;
		var previous = Vector3.zero;
		foreach( var position in m_positionsHistory ) {
			if( index != 0 ) {
				var diff = position - previous;
				totalDiff += diff.XZ() * index;	// TODO: investigate WHYYYY is it scaled by depth?..
			}
			
			index += 1;
			previous = position;
		}
		
		if( totalDiff.magnitude > 0.01f ) {
			// TODO: slerp here!
			transform.rotation = Quaternion.LookRotation( totalDiff.X0Y(), Vector3.up );
		}
	}
	
	private void _ReachNewVelocity( Vector3 newVelocity ) {
		var current = _rigidbody.velocity.WithY( 0f );
		newVelocity *= m_speed;
		
		var isAccelerating = (newVelocity.magnitude > current.magnitude);
		var lerpFactor = isAccelerating ? m_acceleration : m_deacceleration;
		
		var lerped = Vector3.Lerp( current, newVelocity, lerpFactor );
		var delta = lerped - current;
		
		if( m_shouldDraw ) {
			Draw.RayCross( Vector3.zero, delta, Palette.red, 0.5f );
			Draw.Ray( Vector3.zero, current, Palette.yellow, 0.5f );
			Draw.Ray( Vector3.zero, newVelocity, Palette.violet, 0.5f );
		}
		
		_rigidbody.AddForce( delta, ForceMode.VelocityChange );
	}
	
	protected virtual void _Ignite() {
		var allChildren = GetComponentsInChildren<Transform>();
		foreach( var child in allChildren ) {
			child.gameObject.layer = Game.c_layerCreature;
		}
		IsAlive = true;
	}
	
	protected virtual void _Move() {
		if( !IsAlive ) {
			m_moveDirection = Vector3.zero;
		}
		_ReachNewVelocity( m_moveDirection );
	}
	
	protected void _SetMoveDirection( Vector3 direction ) {
		m_moveDirection = direction;
	}
	
	protected virtual void _ChangeIceberg( Iceberg newIceberg ) {
		Iceberg.RemoveEntity( this );
		newIceberg.AddEntity( this );
	}
#endregion
	
	
#region Utility
	private static RaycastHit _GetClosestHit( Ray ray, RaycastHit[] hits, int hitsCount ) {
		// this seems worthwhile, because most creatures are penguins, casting only over ice and hitting once:
		if( hitsCount == 1 ) { return hits[0]; }
		
		var minDistance = float.MaxValue;
		var result = new RaycastHit();
		for( var i = 0; i < hitsCount; i++ ) {
			var hit = hits[i];
			
			var distance = (ray.origin - hit.point).sqrMagnitude;
			if( distance < minDistance ) {
				minDistance = distance;
				result = hit;
			}
		}
		return result;
	}
	
	private static RaycastHit _GetIcebergHit( Ray ray, RaycastHit[] hits, int hitsCount ) {
		for( var i = 0; i < hitsCount; i++ ) {
			var hit = hits[i];
			
			if( hit.collider.gameObject.layer == Game.c_layerIceberg ) {
				return hit;
			}
		}
		return new RaycastHit();
	}
#endregion
}
