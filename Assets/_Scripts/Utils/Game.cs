using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoSingular<Game> {
	[SerializeField] private int m_iceIterations = 40;
	[SerializeField] private Player m_player;
	
	[Header( "Penguins" )]
	[SerializeField] private Penguin m_penguinPrefab;
	[SerializeField] private Vector2i m_populationSize;
	
	[Header( "Penguins" )]
	[SerializeField] private float m_majorPaceLoop = 60f;
	[SerializeField] private float m_minorsCount = 4f;
	
	private Iceberg m_playerIceberg;
	private float m_gameStartTime;
	
	private float _timeRunning {
		get { return (Time.time - m_gameStartTime); }
	}
	
	public static Penguin PenguinPrefab {
		get { return s_Instance.m_penguinPrefab; }
	}
	
#region Implementation
	void Awake() {
		m_penguinPrefab.gameObject.SetActive( false );
		StartGame();
	}
	
	void Update() {
		_RunPacemaker();
	}
#endregion
	
	
#region Public
	public void StartGame() {
		m_gameStartTime = Time.time;
		
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
	
	private void _RunPacemaker() {
		var progress = _timeRunning;
		
		var majorMax = 0.7f *Mathf.PI;
		var majorFactor = Mathf.InverseLerp( 0f, m_majorPaceLoop, progress );
		var majorPace = Mathf.Sin( majorFactor *majorMax );
		
		var minorMax = (m_minorsCount - 0.5f) *Mathf.PI *2;
		var minorFactor = majorFactor;
		var minorPace = Mathf.Cos( minorFactor *minorMax );
		minorPace = Mathf.Max( minorPace, 0 );
		
		var pace = majorPace + minorPace;
		
	}
#endregion
	
	
#region Temporary
#endregion
}
