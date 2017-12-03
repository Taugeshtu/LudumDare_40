using UnityEngine;
using System.Collections.Generic;

public class Monster : CreatureBase {
	
	public bool IsAlive = true;
	
	protected override int _layerMask {
		get { return (1 << 8) + (1 << 4); }	// Note: because Monster can swim can actually walk on monsters
	}
	
#region Implementation
#endregion
	
	
#region Public
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
