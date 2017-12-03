﻿using UnityEngine;
using System.Collections.Generic;

public abstract class CreatureBase : IcebergEntity {
	[Header( "Movement" )]
	[SerializeField] protected float m_speed = 4f;
	[SerializeField] protected float m_acceleration = 0.5f;
	[SerializeField] protected float m_deacceleration = 0.9f;
	
	[Header( "Physics" )]
	[SerializeField] private ValueCurve m_pushoutScale;
	[SerializeField] private float m_castRadius = 0.2f;
	[SerializeField] private float m_castDepth = 1f;
	
	[Header( "Misc" )]
	[SerializeField] private bool m_shouldDraw = false;
	[SerializeField] private int m_historyDepth = 35;
	
	private Queue<Vector2> m_positionsHistory = new Queue<Vector2>();
	private bool m_isInContact;
	protected Vector3 m_moveDirection;
	
	private Rigidbody m_rigid;
	protected Rigidbody _rigidbody {
		get {
			if( m_rigid == null ) { m_rigid = GetComponent<Rigidbody>(); }
			return m_rigid;
		}
	}
	
	protected virtual int _layerMask {
		get { return (1 << 8); }
	}
	
	public bool IsAlive { get; private set; }
	
#region Implementation
	void Awake() {
		_Ignite();
	}
	
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
	public virtual void GetKilled() {
		IsAlive = false;
		// TODO: particle explosion!
		
	}
#endregion
	
	
#region Private
	private void _Pushout() {
		m_isInContact = false;
		RaycastHit hit;
		var ray = new Ray( transform.position + transform.up *m_castDepth, -transform.up );
		if( Caster.SphereCast( ray, m_castRadius, out hit, m_castDepth, _layerMask, m_shouldDraw ) ) {
			var factor = Mathf.InverseLerp( m_castDepth, 0f, hit.distance );
			
			factor = m_pushoutScale.Evaluate( factor );
			factor *= Physics.gravity.magnitude;
			
			var pushout = transform.up *factor;
			if( !IsAlive ) { pushout *= 0f; }
			
			var vertical = Vector3.Project( _rigidbody.velocity, Vector3.up );
			_rigidbody.AddForce( -vertical, ForceMode.VelocityChange );
			_rigidbody.AddForce( pushout, ForceMode.Acceleration );
			m_isInContact = true;
		}
		else {
			_rigidbody.AddForce( Physics.gravity, ForceMode.Acceleration );
		}
	}
	
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
	
	private void _ReachNewVelocity( Vector3 newVelocity ) {
		var current = _rigidbody.velocity.XZ().X0Y();
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
			child.gameObject.layer = 9;
		}
		IsAlive = true;
	}
	
	protected virtual void _Move() {
		if( !IsAlive ) {
			m_moveDirection = Vector3.zero;
		}
		_ReachNewVelocity( m_moveDirection );
	}
#endregion
	
	
#region Temporary
#endregion
}
