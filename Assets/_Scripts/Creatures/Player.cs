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
	[SerializeField] private Transform m_splitUI;
	
	protected override int _layerMask {
		get { return (1 << 8) + (1 << 9); }	// Note: because Player can actually walk on monsters
	}
	
#region Implementation
	void Update() {
		var xAxis = Input.GetAxis( "Horizontal" );
		var yAxis = Input.GetAxis( "Vertical" );
		m_moveDirection = (new Vector2( xAxis, yAxis )).X0Y();
		if( m_moveDirection.magnitude > 1f ) {
			m_moveDirection = m_moveDirection.normalized;
		}
		
		var point = Vector3.zero;
		var direction = Vector3.zero;
		_GetCut( out point, out direction );
		
		m_splitUI.position = point;
		m_splitUI.rotation = Quaternion.LookRotation( direction );
		
		if( Input.GetMouseButtonDown( 1 ) ) {
			if( m_iceberg != null ) {
				m_iceberg.Split( point, direction );
			}
		}
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	protected override void _Ignite() {
		var allChildren = GetComponentsInChildren<Transform>();
		foreach( var child in allChildren ) {
			child.gameObject.layer = 10;
		}
	}
	
	protected override void _Move() {
		base._Move();
	}
	
	private void _GetCut( out Vector3 point, out Vector3 direction ) {
		var plane = new Plane( Vector3.up, transform.position );
		var ray = Camera.main.ScreenPointToRay( Input.mousePosition );
		point = plane.Cast( ray );
		direction = (point - transform.position).normalized.XZ().X0Y() *m_splitReach;
		point = transform.position + direction;
	}
#endregion
	
	
#region Temporary
#endregion
}
