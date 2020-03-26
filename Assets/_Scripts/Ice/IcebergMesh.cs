using UnityEngine;
using System.Collections.Generic;

using Clutter.Mesh;

public class IcebergMesh : MorphMesh {
	private MorphMesh m_skirtMesh;
	private Vector3 m_skirtNormal;
	
	public bool ShouldDraw = false;
	
	public Collider Collider { get; private set; }
	
#region Implementation
	public IcebergMesh( MorphMesh mesh, Component mainTarget, Component skirtTarget, Vector3 skirtNormal ) : this( mainTarget, skirtTarget, skirtNormal ) {
		m_positions = mesh.m_positions;
		m_colors = mesh.m_colors;
		m_ownersCount = mesh.m_ownersCount;
		m_ownersFast = mesh.m_ownersFast;
		m_ownersExt = mesh.m_ownersExt;
		
		m_indeces = mesh.m_indeces;
		
		m_topVertexIndex = m_positions.Count - 1;
		m_topTriangleIndex = (m_indeces.Count /3) - 1;
	}
	
	public IcebergMesh( Component mainTarget, Component skirtTarget, Vector3 skirtNormal ) : base( mainTarget ) {
		Collider = mainTarget.GetComponent<MeshCollider>();
		
		m_skirtNormal = skirtNormal;
		m_skirtMesh = new MorphMesh( skirtTarget );
	}
#endregion
	
	
#region Public
	public new void Write( GameObject target ) {
		Write( target.transform );
	}
	public new void Write( Component target = null ) {
		base.Write( target );
		m_skirtMesh.Write();
	}
	
	public void Coerce( float min, float max ) {
		for( var index = 0; index < m_topVertexIndex; index++ ) {
			var heightChange = Random.Range( min, max );
			var newPosition = m_positions[index] + Vector3.up *heightChange;
			m_positions[index] = newPosition;
		}
		
		ClearDeadTriangles();
		MergeVertices();
		
		_RegenerateSkirt();
		
		UnweldVertices();
		ClearDeadTriangles();
		ClearDeadVertices();
	}
	
	public MorphMesh Split( Vector3 position, Vector3 direction ) {
		// Note: doing merge here because otherwise bound Slice will get bound by one tris
		MergeVertices();
		
		var drifters = base.Slice( position, direction, true );
		var driftMesh = new MorphMesh();
		foreach( var tris in drifters ) {
			driftMesh.EmitTriangle( tris.A.Position, tris.B.Position, tris.C.Position );
			DeleteTriangle( tris );
		}
		ClearDeadTriangles();	// needed because we JUST performed a tris deletion, to bring ownership data in line
		
		OptimizeEdgeVertices();	// fixes the splits
		
		_RegenerateSkirt();
		
		UnweldVertices();
		ClearDeadTriangles();
		ClearDeadVertices();
		
		return driftMesh;
	}
	
	private void _RegenerateSkirt() {
		var selection = new Selection( this, GetAllTriangles( false ) );
		
		m_skirtMesh.Clear();
		foreach( var edge in selection.OutlineEdges ) {
			var a = m_skirtMesh.EmitVertex( edge.A.Position );
			var b = m_skirtMesh.EmitVertex( edge.A.Position - m_skirtNormal );
			var c = m_skirtMesh.EmitVertex( edge.B.Position - m_skirtNormal );
			var d = m_skirtMesh.EmitVertex( edge.B.Position );
			
			m_skirtMesh.EmitTriangle( ref a, ref b, ref c );
			m_skirtMesh.EmitTriangle( ref c, ref d, ref a );
		}
	}
	
	public void DrawSkirt() {
		var arrowSize = 1f;
		var allTris = GetAllTriangles( false );
		var selection = new Selection( this, allTris );
		
		foreach( var edge in selection.OutlineEdges ) {
			edge.Draw( Palette.orange, arrowSize, 5f );
		}
		
		Debug.LogError( "Skirt debug, mesh: "+Dump() );
		
		var edgeDebs = "Edges: ";
		foreach( var tris in allTris ) {
			foreach( var edge in tris.Edges ) {
				edgeDebs += "\n"+edge+" | owners: "+edge.OwnersCount;
			}
		}
		Debug.Log( edgeDebs );
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
