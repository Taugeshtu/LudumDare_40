using UnityEngine;
using System.Collections.Generic;

using Clutter;

public class TimingManager : MonoSingular<TimingManager> {
	protected override BehaviourSettings Behaviour { get { return new BehaviourSettings( true, true, true ); } }
	
	[SerializeField] private float m_chargeTime = 0.7f;
	[SerializeField] private float m_splitTime = 0.7f;
	[SerializeField] private float m_attackTime = 0.7f;
	[SerializeField] private float m_climbTime = 0.7f;
	
#region Implementation
	public static float ChargeTime { get { return s_Instance.m_chargeTime; } }
	public static float SplitTime { get { return s_Instance.m_splitTime; } }
	public static float AttackTime { get { return s_Instance.m_attackTime; } }
	public static float ClimbTime { get { return s_Instance.m_climbTime; } }
#endregion
	
	
#region Public
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
