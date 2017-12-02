using UnityEngine;
using System.Collections.Generic;

public class Player : CreatureBase {
	[SerializeField] private float m_splitReach = 1.5f;
	
	private Vector2 m_moveDirection;
	
#region Implementation
	void Update() {
		var xAxis = Input.GetAxis( "Horizontal" );
		var yAxis = Input.GetAxis( "Vertical" );
		m_moveDirection = new Vector2( xAxis, yAxis );
		
		if( Input.GetKeyDown( KeyCode.L ) ) {
			if( _iceberg != null ) {
				var plane = new Plane( Vector3.up, transform.position );
				var ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				var point = plane.Cast( ray );
				var direction = (point - transform.position).normalized.XZ().X0Y() *m_splitReach;
				point = transform.position + direction;
				
				Draw.RayCross( point, direction, Palette.blue, 0.5f, 2f );
				
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
#endregion
	
	
#region Temporary
#endregion
}
