using UnityEngine;
using System.Collections.Generic;

public class Player : CreatureBase {
	[SerializeField] private float m_splitReach = 1.5f;
	[SerializeField] private Transform m_splitUI;
	
	private Vector2 m_moveDirection;
	
#region Implementation
	void Update() {
		var xAxis = Input.GetAxis( "Horizontal" );
		var yAxis = Input.GetAxis( "Vertical" );
		m_moveDirection = new Vector2( xAxis, yAxis );
		
		var point = Vector3.zero;
		var direction = Vector3.zero;
		_GetCut( out point, out direction );
		
		m_splitUI.position = point;
		m_splitUI.rotation = Quaternion.LookRotation( direction );
		
		if( Input.GetMouseButtonDown( 1 ) ) {
			if( _iceberg != null ) {
				_iceberg.Split( point, direction );
			}
		}
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	protected override void _Move() {
		_ReachNewVelocity( m_moveDirection.X0Y() );
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
