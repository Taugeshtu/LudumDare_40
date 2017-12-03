using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoSingular<Game> {
	[SerializeField] private int m_iceIterations = 40;
	[SerializeField] private Player m_player;
	[SerializeField] private Penguin m_penguinPrefab;
	[SerializeField] private Vector2i m_populationSize;
	
	private Iceberg m_playerIceberg;
	
	public static Penguin PenguinPrefab {
		get { return s_Instance.m_penguinPrefab; }
	}
	
#region Implementation
	void Awake() {
		m_penguinPrefab.gameObject.SetActive( false );
		StartGame();
	}
#endregion
	
	
#region Public
	public void StartGame() {
		m_playerIceberg = IceGenerator.Generate( m_iceIterations );
		
		if( m_player != null ) {
			m_playerIceberg.LinkPlayer( m_player );
		}
		
		StopAllCoroutines();
		StartCoroutine( _SpawnPenguinsRoutine() );
	}
#endregion
	
	
#region Private
	private IEnumerator _SpawnPenguinsRoutine() {
		yield return new WaitForSeconds( 0.3f );
		
		var penguinsCount = Random.Range( m_populationSize.x, m_populationSize.y );
		Extensions.TimeLog( "Settled on "+penguinsCount+" penguins" );
		for( var i = 0; i < penguinsCount; i++ ) {
			m_playerIceberg.SpawnPenguins( penguinsCount );
			yield return new WaitForSeconds( 0.05f);
		}
	}
#endregion
	
	
#region Temporary
#endregion
}
