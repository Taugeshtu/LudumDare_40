using UnityEngine;
using System.Collections.Generic;

public class Game : MonoSingular<Game> {
	[SerializeField] private int m_iceIterations = 40;
	[SerializeField] private Player m_player;
	
	private Iceberg m_playerIceberg;
	
#region Implementation
	void Awake() {
		StartGame();
	}
#endregion
	
	
#region Public
	public void StartGame() {
		m_playerIceberg = IceGenerator.Generate( m_iceIterations );
		
		if( m_player != null ) {
			m_playerIceberg.LinkPlayer( m_player );
		}
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
