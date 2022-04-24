using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using com.jon_skoberne.Reader;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshCreator : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)] // Please C# do not optimize the layout
    struct Vertex
    {
        public float3 position, texCoord0;
    } // 12B + 12B = 24B

    // temporary
    public Texture3D activeGrid;
    public Material objectSkipMaterial;


    // TEMPORARY
    private ImageDataObject ido; 
    public void SetIdo(ImageDataObject ido)
    {
        this.ido = ido;
    }

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
        CreateActiveCubes();
    }

    void SetShaderTexture(Texture3D activeGrid)
    {
        this.activeGrid = activeGrid;
        this.objectSkipMaterial.SetTexture("_ActiveGridTex", activeGrid);
        this.objectSkipMaterial.SetVector("_ActiveGridDims", new Vector4(this.activeGrid.width, this.activeGrid.height, this.activeGrid.depth));
        this.objectSkipMaterial.SetTexture("_CompTopTex", this.ido.tex3D);
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
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3, 0)
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
                position = new Vector3((float)x / activeGrid.width, (float)y / activeGrid.height, (float)z / activeGrid.depth),
                texCoord0 = new Vector3(x, y, z),
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
            //bounds = bounds,
            vertexCount = vertexCount
        }, MeshUpdateFlags.DontRecalculateBounds);


        GetComponent<MeshFilter>().mesh = mesh;

        // CLEANUP
        verts.Dispose();
        pointIndices.Dispose();
    }
}
