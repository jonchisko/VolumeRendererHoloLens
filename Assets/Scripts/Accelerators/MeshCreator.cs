using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Profiling;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshCreator : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)] // Please C# do not optimize the layout
    struct Vertex
    {
        public float3 position, normal;
        public half4 tangent;
        public half2 texCoord0;
    } // 12B + 12B + 8B + 4B == 36B

    // temporary
    public Texture3D activeGrid;
    public Material objectSkipMaterial;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        RegisterToEvents();
    }

    void OnDisable()
    {
        DeregisterFromEvents();
    }

    void RegisterToEvents()
    {
        BounderBuilder.OnFinishedActiveGrid += ObjectSkipSpaceRender;
    }

    void DeregisterFromEvents()
    {
        BounderBuilder.OnFinishedActiveGrid -= ObjectSkipSpaceRender;
    }

    void ObjectSkipSpaceRender(Texture3D activeGrid)
    {
        SetShaderTexture(activeGrid);
        //CreateCube();
        CreateActiveCubes();
    }

    void SetShaderTexture(Texture3D activeGrid)
    {
        this.activeGrid = activeGrid;
        objectSkipMaterial.SetTexture("_ActiveGridTex", activeGrid);
    }

    void CreateCube()
    {
        Debug.Log("Creating Cube in Mesh Creator");
        int vertexCount = 8;
        int triangleIndexCount = 36;


        var bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f));

        var mesh = new Mesh
        {
            name = "Procedural Cube Mesh",
            bounds = bounds,
        };

        // VERTEX ATTRIBUTES
        var vertexAttributesLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 0)
        };
        mesh.SetVertexBufferParams(vertexCount, vertexAttributesLayout);

        // VERTEX DATA
        var verts = new NativeArray<Vertex>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        // FRONT 
        verts[0] = new Vertex
        {
            position = Vector3.zero,
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(0), new half(0)),
        };

        verts[1] = new Vertex
        {
            position = Vector3.right,
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(1), new half(0)),
        };

        verts[2] = new Vertex
        {
            position = Vector3.up,
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(0), new half(1)),
        };

        verts[3] = new Vertex
        {
            position = new Vector3(1f, 1f, 0f),
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(1), new half(1)),
        };
        // BACK
        verts[4] = new Vertex
        {
            position = new Vector3(0f, 0f, 1f),
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(0), new half(0)),
        };

        verts[5] = new Vertex
        {
            position = new Vector3(1f, 0f, 1f),
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(1), new half(0)),
        };

        verts[6] = new Vertex
        {
            position = new Vector3(0f, 1f, 1f),
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(0), new half(1)),
        };

        verts[7] = new Vertex
        {
            position = new Vector3(1f, 1f, 1f),
            normal = Vector3.back,
            tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
            texCoord0 = new half2(new half(1), new half(1)),
        };

        mesh.SetVertexBufferData(verts, 0, 0, vertexCount);

        // INDEX TRIANGLE DATA
        mesh.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt16);
        var triangleIndices = new NativeArray<ushort>(triangleIndexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        // front face
        triangleIndices[0] = 0;
        triangleIndices[1] = 2;
        triangleIndices[2] = 1;
        triangleIndices[3] = 1;
        triangleIndices[4] = 2;
        triangleIndices[5] = 3;
        // left face
        triangleIndices[6] = 4;
        triangleIndices[7] = 6;
        triangleIndices[8] = 0;
        triangleIndices[9] = 0;
        triangleIndices[10] = 6;
        triangleIndices[11] = 2;
        // back face
        triangleIndices[12] = 5;
        triangleIndices[13] = 6;
        triangleIndices[14] = 4;
        triangleIndices[15] = 5;
        triangleIndices[16] = 7;
        triangleIndices[17] = 6;
        // right face
        triangleIndices[18] = 1;
        triangleIndices[19] = 3;
        triangleIndices[20] = 5;
        triangleIndices[21] = 5;
        triangleIndices[22] = 3;
        triangleIndices[23] = 7;
        // bottom face
        triangleIndices[24] = 0;
        triangleIndices[25] = 1;
        triangleIndices[26] = 4;
        triangleIndices[27] = 1;
        triangleIndices[28] = 5;
        triangleIndices[29] = 4;
        // top face
        triangleIndices[30] = 2;
        triangleIndices[31] = 6;
        triangleIndices[32] = 3;
        triangleIndices[33] = 3;
        triangleIndices[34] = 6;
        triangleIndices[35] = 7;

        mesh.SetIndexBufferData(triangleIndices, 0, 0, triangleIndexCount);

        // SUBMESH
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
        {
            bounds = bounds,
            vertexCount = vertexCount
        }, MeshUpdateFlags.DontRecalculateBounds);


        GetComponent<MeshFilter>().mesh = mesh;

        // CLEANUP
        verts.Dispose();
        triangleIndices.Dispose();
    }

    void CreateActiveCubes()
    {
        Debug.Log("Creating ActiveGrid in Mesh Creator");
        if (activeGrid == null)
        {
            Debug.Log("Aborting because activeGrid is null");
            return;
        }
        
        int vertexCount = activeGrid.width * activeGrid.height * activeGrid.depth;

        var bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f));

        var mesh = new Mesh
        {
            name = "Procedural ActiveGrid Mesh",
            bounds = bounds,
        };

        // VERTEX ATTRIBUTES
        var vertexAttributesLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float16, 4, 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 2, 0)
        };
        mesh.SetVertexBufferParams(vertexCount, vertexAttributesLayout);

        // VERTEX DATA
        var verts = new NativeArray<Vertex>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        for(int i = 0; i < vertexCount; i++)
        {
            int slice = activeGrid.width * activeGrid.height;
            int x = i % activeGrid.width;
            int z = i / slice;
            int y = (i - z * slice) / activeGrid.width;
            

            verts[i] = new Vertex
            {
                position = new Vector3(x, y, z),
                normal = Vector3.back,
                tangent = new half4(new half(1), new half(0), new half(0), new half(-1)),
                texCoord0 = new half2(new half(0), new half(0)),
            };
        }
        mesh.SetVertexBufferData(verts, 0, 0, vertexCount);

        // INDEX POINT ("triangle") DATA
        mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);
        var pointIndices = new NativeArray<uint>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for(int i = 0; i < vertexCount; i++)
        {
            pointIndices[i] = (uint)i;
        }
        mesh.SetIndexBufferData(pointIndices, 0, 0, vertexCount);

        // SUBMESH
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount, MeshTopology.Points)
        {
            bounds = bounds,
            vertexCount = vertexCount
        }, MeshUpdateFlags.DontRecalculateBounds);


        GetComponent<MeshFilter>().mesh = mesh;

        // CLEANUP
        verts.Dispose();
        pointIndices.Dispose();
    }

}
