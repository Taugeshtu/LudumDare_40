using UnityEngine;
using System.Collections.Generic;

public class IcebergEntity : MonoBehaviour {
	private Iceberg m_iceberg;
	
	public Vector3 Position { get { return transform.localPosition; } }
	public Iceberg Iceberg { get { return m_iceberg; } }
	
#region Implementation
#endregion
	
	
#region Public
	public void Link( Iceberg iceberg ) {
		m_iceberg = iceberg;
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
