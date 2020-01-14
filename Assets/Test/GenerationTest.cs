using UnityEngine;
using System.Collections.Generic;

public class GenerationTest : MonoBehaviour {
	[SerializeField] private int m_iterations;
	
#region Implementation
	void Awake() {
		var ice = IceGenerator.GenerateNew( m_iterations );
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
