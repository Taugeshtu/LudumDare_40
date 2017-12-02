using UnityEngine;
using System.Collections.Generic;

public abstract class CreatureBase : MonoBehaviour {
	[SerializeField] private ValueCurve m_pushoutScale;
	[SerializeField] private float m_castRadius = 0.2f;
	[SerializeField] private float m_castDepth = 1f;
	[SerializeField] private int m_historyDepth = 10;
	[SerializeField] protected float m_speed = 3f;
	[SerializeField] protected float m_acceleration = 6f;
	
	private Queue<Vector2> m_positionsHistory = new Queue<Vector2>();
	private bool m_shouldDraw = true;
	private Vector3 m_velocity;
	
	private Rigidbody m_rigid;
	protected Rigidbody _rigidbody {
		get {
			if( m_rigid == null ) { m_rigid = GetComponent<Rigidbody>(); }
			return m_rigid;
		}
	}
	
#region Implementation
	void FixedUpdate() {
		_Move();
		_Pushout();
		
		var flatVelocity = _rigidbody.velocity.XZ();
		if( flatVelocity.magnitude > (m_speed /2) ) {
			m_positionsHistory.Enqueue( flatVelocity );
		}
		
		if( m_positionsHistory.Count > m_historyDepth ) {
			m_positionsHistory.Dequeue();
		}
		
		_UpdateTurn();
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	private void _Pushout() {
		RaycastHit hit;
		var ray = new Ray( transform.position + transform.up *m_castDepth, -transform.up );
		if( Caster.SphereCast( ray, m_castRadius, out hit, m_castDepth, Physics.DefaultRaycastLayers, m_shouldDraw ) ) {
			var factor = Mathf.InverseLerp( m_castDepth, 0f, hit.distance );
			
			factor = m_pushoutScale.Evaluate( factor );
			factor *= Physics.gravity.magnitude;
			
			var pushout = transform.up *factor;
			_rigidbody.AddForce( -_rigidbody.velocity, ForceMode.VelocityChange );
			_rigidbody.AddForce( pushout, ForceMode.Acceleration );
		}
	}
	
	protected virtual void _Move() {}
	
	private void _UpdateTurn() {
		var history = new List<Vector2>( m_positionsHistory );
		var totalDiff = Vector2.zero;
		for( var i = 0; i < history.Count; i++ ) {
			totalDiff += history[i] *i;
		}
		
		if( totalDiff.magnitude > 0.01f ) {
			// TODO: slerp here!
			transform.rotation = Quaternion.LookRotation( totalDiff.X0Y(), Vector3.up );
		}
	}
	
	protected void _ReachNewVelocity( Vector3 newVelocity ) {
		Vector3.SmoothDamp( _rigidbody.velocity, newVelocity, ref m_velocity, m_acceleration *Time.fixedDeltaTime, m_speed );
		
		Draw.RayCross( transform.position, m_velocity, Palette.red, 0.5f );
		
		_rigidbody.AddForce( m_velocity, ForceMode.VelocityChange );
	}
#endregion
	
	
#region Temporary
#endregion
}
