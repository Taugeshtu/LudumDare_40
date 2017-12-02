using UnityEngine;
using System.Collections.Generic;

public class Player : CreatureBase {
	private Vector2 m_moveDirection;
	
#region Implementation
	void Update() {
		var xAxis = Input.GetAxis( "Horizontal" );
		var yAxis = Input.GetAxis( "Vertical" );
		m_moveDirection = new Vector2( xAxis, yAxis );
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
