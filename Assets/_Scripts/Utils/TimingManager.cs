using UnityEngine;
using System.Collections.Generic;

public class TimingManager : MonoSingular<TimingManager> {
	[SerializeField] private float m_chargeTime = 0.7f;
	[SerializeField] private float m_splitTime = 0.7f;
	[SerializeField] private float m_attackTime = 0.7f;
	
#region Implementation
	public static float ChargeTime { get { return s_Instance.m_chargeTime; } }
	public static float SplitTime { get { return s_Instance.m_splitTime; } }
	public static float AttackTime { get { return s_Instance.m_attackTime; } }
#endregion
	
	
#region Public
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
