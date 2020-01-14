using UnityEngine;
using System.Collections.Generic;

public static class Dice {
	
#region Implementation
#endregion
	
	
#region Public
	public static bool Roll( float chanceToPass ) {
		if( Random.value <= chanceToPass ) {
			return true;
		}
		return false;
	}
	
	public static T PickRandom<T>( this IList<T> list ) {
		if( list.Count == 0 ) {
			return default( T );
		}
		
		var roll = Random.Range( 0, list.Count );
		return list[roll];
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
