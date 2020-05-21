using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using Clutter;

public class Game : MonoSingular<Game> {
	public const int c_layerWater = 4;
	public const int c_layerIceberg = 8;
	public const int c_layerCreature = 9;
	public const int c_layerPlayer = 10;
	
	protected override BehaviourSettings Behaviour { get { return new BehaviourSettings( false, true, true ); } }
	
	private enum GameState {
		NotReady,
		Menu,
		Tutorial,
		InGame,
		StatsScreen,
		Results
	}
	
	[SerializeField] private int m_seed;
	[SerializeField] private int m_iceIterations = 40;
	[SerializeField] private Player m_player;
	[SerializeField] private bool m_startWithMenu;
	
	[Header( "Penguins" )]
	[SerializeField] private Penguin m_penguinPrefab;
	[SerializeField] private Vector2Int m_populationSize;
	
	[Header( "Monsters" )]
	[SerializeField] private Monster m_monsterPrefab;
	
	[Header( "Pace" )]
	[SerializeField] private float m_majorPaceLoop = 120f;
	[SerializeField] private float m_majorAmplitude = 3f;
	
	[SerializeField] private float m_minorsCount = 4f;
	[SerializeField] private float m_minorAmplitude = 1f;
	
	[Header( "Balancing" )]
	[SerializeField] private float m_valueDropSeconds = 5f;
	[SerializeField] private float m_monsterValue = 1f;
	[SerializeField] private float m_singleMonsterChance = 0.25f;
	[SerializeField] private int m_debugMonstersScaler = 1;
	
	[Header( "Links" )]
	[SerializeField] private GameObject m_menuRoot;
	[SerializeField] private Slider m_gameProgressRoot;
	[SerializeField] private GameObject m_resultsRoot;
	[SerializeField] private Text m_resultText;
	
	private GameState m_state = GameState.NotReady;
	private float m_gameStartTime;
	private Iceberg m_playerIceberg;	// TODO: switch it to a dynamic link
	
	private float m_currentValue;
	
	private int m_penguinsSpawned;
	
	private float _timeRunning { get { return (Time.time - m_gameStartTime); } }
	private float _progress { get { return _timeRunning/m_majorPaceLoop; } }
	
	public static Penguin PenguinPrefab { get { return s_Instance.m_penguinPrefab; } }
	public static Monster MonsterPrefab { get { return s_Instance.m_monsterPrefab; } }
	
#region Implementation
	private void _ToggleUI() {
		if( (m_menuRoot == null) || (m_gameProgressRoot == null) || (m_resultsRoot == null) ) {
			return;
		}
		
		m_menuRoot.SetActive( m_state == GameState.Menu );
		m_gameProgressRoot.gameObject.SetActive( m_state == GameState.InGame );
		m_resultsRoot.SetActive( m_state == GameState.Results );
	}
	
	void Awake() {
		if( m_seed >= 0 ) {
			Random.InitState( m_seed );
		}
		
		if( m_penguinPrefab != null ) {
			m_penguinPrefab.gameObject.SetActive( false );
		}
		if( m_monsterPrefab != null ) {
			m_monsterPrefab.gameObject.SetActive( false );
		}
		
		// StartGame();
		// _DebugPace();
		
		if( m_startWithMenu ) {
			_GoMenu();
		}
		else {
			StartGame();
		}
	}
	
	void Update() {
		if( m_state == GameState.InGame ) {
			_RunGameLogic();
		}
		
		if( Input.GetKeyDown( KeyCode.Escape ) ) {
			if( m_state == GameState.InGame ) {
				_GoMenu();
			}
		}
	}
	
	public void Exit() {
		Application.Quit();
	}
	
	private void _GoMenu() {
		m_state = GameState.Menu;
		_ToggleUI();
		
		m_player.Despawn();
	}
	
	private void _GoResults() {
		m_state = GameState.Results;
		_ToggleUI();
		
		var alive = 0;
		foreach( var pengu in m_player.Iceberg.Penguins ) {
			if( pengu.IsAlive ) {
				alive += 1;
			}
		}
		
		if( m_resultText != null ) {
			m_resultText.text = "Penguins saved: "+alive+"/"+m_penguinsSpawned+"\nMonsters killed: "+m_player.Kills;
		}
		
		StartCoroutine( _ReturnToMenuRoutine() );
	}
	
	private IEnumerator _ReturnToMenuRoutine() {
		yield return new WaitForSeconds( 5 );
		_GoMenu();
	}
#endregion
	
	
#region Public
	public void StartGame() {
		m_player.transform.SetParent( null );
		
		if( m_playerIceberg != null ) {
			Destroy( m_playerIceberg.gameObject );
		}
		
		m_playerIceberg = IceGenerator.GenerateNew( m_iceIterations );
		if( m_player != null ) {
			m_playerIceberg.AddEntity( m_player );
		}
		
		StopAllCoroutines();
		StartCoroutine( _DelayedStart() );
	}
#endregion
	
	
#region Private
	private IEnumerator _DelayedStart() {
		yield return new WaitForSeconds( 0.3f );
		m_gameStartTime = Time.time;
		m_state = GameState.InGame;
		_ToggleUI();
		
		m_player.Spawn();
		
		m_penguinsSpawned = Random.Range( m_populationSize.x, m_populationSize.y );
		Debug.Log( "Settled on "+m_penguinsSpawned+" penguins" );
		for( var i = 0; i < m_penguinsSpawned; i++ ) {
			m_playerIceberg.SpawnPenguins( 1 );
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
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
		if( m_gameProgressRoot != null ) {
			m_gameProgressRoot.value = _progress;
		}
		
		if( _progress >= 0.98f ) {
			_GoResults();
		}
		
		var monstersAlive = 0;
		foreach( var monster in m_playerIceberg.Monsters ) {
			if( monster.AliveAndActive ) {
				monstersAlive += 1;
			}
		}
		
		if( monstersAlive > 5 ) {
			return;
		}
		
		m_currentValue -= Time.deltaTime /m_valueDropSeconds;
		if( m_currentValue > 0 ) {
			return;
		}
		
		var pace = _GetPace( _timeRunning );
		var monstersToSpawn = Mathf.Round( pace );
		if( Dice.Roll( m_singleMonsterChance ) ) {
			monstersToSpawn = 1;
		}
		monstersToSpawn *= m_debugMonstersScaler;
		
		if( monstersToSpawn > 0 ) {
			Debug.LogError( "Going to spawn "+monstersToSpawn+" monsters!" );
		}
		
		for( var i = 0; i < monstersToSpawn; i++ ) {
			m_playerIceberg.SpawnMonster();
			m_currentValue += m_monsterValue;
		}
	}
#endregion
	
	
#region Temporary
#endregion
}
