using UnityEngine;
using System.Collections.Generic;

public class AttackIndicator : MonoBehaviour {
	
#region Implementation
#endregion
	
	
#region Public
	public void Position( Vector3 point, Vector3 direction ) {
		transform.position = point;
		transform.rotation = Quaternion.LookRotation( direction );
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
