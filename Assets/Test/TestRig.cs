using UnityEngine;
using System.Collections.Generic;

public class TestRig : MonoBehaviour {
	[SerializeField] private Player m_player;
	[SerializeField] private int m_iceIterations;
	
#region Implementation
	void Awake() {
		var ice = IceGenerator.GenerateNew( m_iceIterations );
		ice.AddEntity( m_player );
		m_player.Spawn();
	}
	
	void Update() {
		if( Input.GetKeyDown( KeyCode.R ) ) {
			m_player.Spawn();
		}
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
