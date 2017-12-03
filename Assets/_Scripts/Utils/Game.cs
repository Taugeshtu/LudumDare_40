using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoSingular<Game> {
	private enum GameState {
		InGame
	}
	
	[SerializeField] private int m_iceIterations = 40;
	[SerializeField] private Player m_player;
	
	[Header( "Penguins" )]
	[SerializeField] private Penguin m_penguinPrefab;
	[SerializeField] private Vector2i m_populationSize;
	
	[Header( "Monsters" )]
	[SerializeField] private Monster m_monsterPrefab;
	
	[Header( "Pace" )]
	[SerializeField] private float m_majorPaceLoop = 120f;
	[SerializeField] private float m_majorAmplitude = 3f;
	
	[SerializeField] private float m_minorsCount = 4f;
	[SerializeField] private float m_minorAmplitude = 1f;
	
	[Header( "Balancing" )]
	[SerializeField] private float m_monsterValue = 1f;
	[SerializeField] private float m_singleMonsterChance = 0.25f;
	
	private GameState m_state;
	private float m_gameStartTime;
	private Iceberg m_playerIceberg;
	
	private float m_currentValue;
	
	private float _timeRunning { get { return (Time.time - m_gameStartTime); } }
	
	public static Penguin PenguinPrefab { get { return s_Instance.m_penguinPrefab; } }
	public static Monster MonsterPrefab { get { return s_Instance.m_monsterPrefab; } }
	
#region Implementation
	void Awake() {
		m_penguinPrefab.gameObject.SetActive( false );
		StartGame();
		_DebugPace();
	}
	
	void Update() {
		if( m_state == GameState.InGame ) {
			_RunGameLogic();
		}
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
	
	private void _DebugPace() {
		var last = 0f;
		var xScale = m_minorsCount /60f;
		for( var i = 0; i < m_majorPaceLoop; i++ ) {
			var pace = _GetPace( i );
			
			var p1 = Vector3.right *xScale *(i - 1) + Vector3.up *last + Vector3.up *10;
			var p2 = Vector3.right *xScale *(i) + Vector3.up *pace + Vector3.up *10;
			last = pace;
			Draw.Line( p1, p2, Palette.yellow, 5 );
		}
	}
	
	private float _GetPace( float time ) {
		var factor = Mathf.InverseLerp( 0f, m_majorPaceLoop, time );
		
		var majorMax = 0.7f *Mathf.PI;
		var majorPace = Mathf.Sin( factor *majorMax );
		
		var minorMax = (m_minorsCount - 0.5f) *Mathf.PI *2;
		var minorPace = Mathf.Cos( factor *minorMax );
		if( minorPace < 0 ) {
			minorPace /= 2;
		}
		
		var pace = (majorPace *m_majorAmplitude) + (minorPace *m_minorAmplitude);
		return pace;
	}
	
	private void _RunGameLogic() {
		var monstersAlive = 0;
		foreach( var monster in m_playerIceberg.Monsters ) {
			if( monster.IsAlive ) {
				monstersAlive += 1;
			}
		}
		
		if( monstersAlive > 0 ) {
			return;
		}
		
		if( m_currentValue > 0 ) {
			return;
		}
		
		var pace = _GetPace( _timeRunning );
		var monstersToSpawn = Mathf.Round( pace );
		if( Dice.Roll( m_singleMonsterChance ) ) {
			monstersToSpawn = 1;
		}
		
		for( var i = 0; i < monstersToSpawn; i++ ) {
			m_playerIceberg.SpawnMonster();
		}
	}
#endregion
	
	
#region Temporary
#endregion
}
