﻿using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class IceGenerator : MonoSingular<IceGenerator> {
	[SerializeField] private Material m_material;
	[SerializeField] private float m_genRadius = 3f;
	[SerializeField] private float m_stitchRadius = 5f;
	[SerializeField] private Vector2 m_coercion = Vector2.up *0.5f;
	
	public static Material Material {
		get { return s_Instance.m_material; }
		set { s_Instance.m_material = value; }
	}
	public static float GenRadius {
		get { return s_Instance.m_genRadius; }
		set { s_Instance.m_genRadius = value; }
	}
	public static float StitchRadius {
		get { return s_Instance.m_stitchRadius; }
		set { s_Instance.m_stitchRadius = value; }
	}
	public static Vector2 Coercion {
		get { return s_Instance.m_coercion; }
		set { s_Instance.m_coercion = value; }
	}
	
#region Implementation
#endregion
	
	
#region Public
	public static Iceberg Generate( int iterations ) {
		var prep = _PrepareMesh();
		
		var iceberg = prep.Filter.gameObject.AddComponent<Iceberg>();
		iceberg.Init( prep );
		
		for( var i = 0; i < iterations; i++ ) {
			iceberg.Mesh.SpawnTriangle( GenRadius, StitchRadius );
		}
		
		iceberg.Mesh.Coerce( Coercion.x, Coercion.y );
		iceberg.Mesh.WriteToMesh();
		
		return iceberg;
	}
#endregion
	
	
#region Private
	private static IcebergMesh _PrepareMesh() {
		var holder = new GameObject( "Iceberg" );
		var filter = holder.AddComponent<MeshFilter>();
		var collider = holder.AddComponent<MeshCollider>();
		var render = holder.AddComponent<MeshRenderer>();
		
		var mesh = new Mesh();
		filter.mesh = mesh;
		collider.sharedMesh = mesh;
		render.material = Material;
		
		var mathMesh = new IcebergMesh( filter, collider, false );
		return mathMesh;
	}
	
	private void _GenIteration( IcebergMesh mesh ) {
		mesh.SpawnTriangle( m_genRadius, m_stitchRadius );
		mesh.WriteToMesh();
	}
#endregion
	
	
#region Temporary
#endregion
}