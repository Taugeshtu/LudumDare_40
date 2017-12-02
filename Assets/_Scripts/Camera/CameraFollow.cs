using UnityEngine;
using System.Collections.Generic;

public class CameraFollow : MonoBehaviour {
	public Transform Target;
	
#region Implementation
#endregion
	
	
#region Public
	void LateUpdate() {
		if( Target != null ) {
			transform.position = Target.position;
		}
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
