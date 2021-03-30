using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSim : MonoBehaviour
{
    public class Node
    {
        public Vector3 position;
        public Vector3 velocity;
        public bool is_stationary;
        
        public Node(Vector3 pos, Vector3 vel, bool stationary = false) {
            position = pos;
            is_stationary = false;
            velocity = vel;
        }

    }

    public class Spring
    {
        public int node1;
        public int node2;
        public float k;
        public float x;
        public bool active;
        public Vector3 force;

        public Spring(int n1, int n2, float sc, float rest_len, bool act = true) {
            node1 = n1;
            node2 = n2;
            k = sc;
            x = rest_len;
            active = act;
        }
    }


    public int w = 100;
    public int h = 100;
    public float width = 10;
    public float height = 10;
    public float gravity = 0.01f;
    public float dampening = 0.5f;
    public float mass = 1.0f;
    public float spring_constant = 100000.0f;
    public float resting_len = 0.0f;
    public float dt = 0.01f;

    public SphereCollider sphere;
    public Transform sphere_trans;


    [HideInInspector]
    public Vector3[] vertices;
    [HideInInspector]
    public int[] tris;
    [HideInInspector]
    public Vector3[] normals;
    [HideInInspector]
    public Vector2[] uv;
    [HideInInspector]
    public Node[] nodes;
    [HideInInspector]
    public Spring[] springs;
    [HideInInspector]
    public MeshRenderer meshRenderer;
    [HideInInspector]
    public MeshFilter meshFilter;
    [HideInInspector]
    public Mesh mesh;
    [HideInInspector]
    public Vector3[] forces;
    [HideInInspector]
    public int net_count = 0;
	[HideInInspector]
	public Color[] colors;
	[HideInInspector]
	public Material material;


    public void Init() {
    	material = GetComponent<Renderer> ().material;
    	material.SetColor("_Color", Color.blue);
    	material.SetFloat("_Metallic", 0.6f);
 		material.SetFloat("_GlossMapScale", 0.43f);
        w = 40;
        h = 40;
        width = 10;
        height = 10;
        gravity = 10f;
        mass = 1f;
        spring_constant = 1000f;
        resting_len = 0.0f;
        dampening = 3f;
        dt = 0.01f;

        //Fill in default vertex positions
        forces = new Vector3[w*h];
        resting_len = (float)(width/((w-1)));
        vertices = new Vector3[w*h];
        nodes = new Node[w*h];
        tris = new int[(w-1)*(h-1)*3*4];
        normals = new Vector3[w*h];
        uv = new Vector2[w*h];
        colors = new Color[vertices.Length];

        for (int i = 0; i < h; i++) {
            for (int j = 0; j < w; j++) {
                nodes[i*h + j] = new Node(new Vector3((float)width*j/(w-1), (float)4.0, (float)height*i/(h-1)), new Vector3(0.0f,0.0f,0.0f));
                vertices[i*h + j] = new Vector3((float)width*j/(w-1), (float)4.0, (float)height*i/(h-1));
                forces[i*h + j] = new Vector3(0, -gravity*mass, 0);
                uv[i*h + j] = new Vector2(0.5f, 0.5f);
                normals[i*h + j] = -Vector3.forward;
            }
        }

        springs = new Spring[(w-1)*h + (h-1)*w + (w-2)*h + (h-2)*w + 2*(w-1)*(h-1)];
        int count = 0;

        // Structural Springs
        for (int i = 0; i < w-1; i++) {
            for (int j = 0; j < h; j++) {
                Spring curr_spring = new Spring(h*j+i, h*j+i+1, spring_constant, resting_len);
                springs[count] = curr_spring;
                count++;
            }
        }
        for (int i = 0; i < w; i++) {
            for (int j = 0; j < h-1; j++) {
                Spring curr_spring = new Spring(h*j+i, h*(j+1)+i, spring_constant, resting_len);
                springs[count] = curr_spring;
                count++;
            }
        }


        // Bend Springs
        for (int i = 0; i < w-2; i++) {
            for (int j = 0; j < h; j++) {
                Spring curr_spring = new Spring(h*j+i, h*j+i+2, spring_constant, resting_len*2);
                springs[count] = curr_spring;
                count++;
            }
        }
        for (int i = 0; i < w; i++) {
            for (int j = 0; j < h-2; j++) {
                Spring curr_spring = new Spring(h*j+i, h*(j+2)+i, spring_constant, resting_len*2);
                springs[count] = curr_spring;
                count++;
            }
        }

        // Shear Springs
        for (int i = 0; i < w-1; i++) {
            for (int j = 0; j < h-1; j++) {
                Spring curr_spring = new Spring(h*j+i, h*(j+1)+i+1, spring_constant, resting_len*Mathf.Sqrt(2));
                springs[count] = curr_spring;
                count++;
            }
        }
        for (int i = 1; i < w; i++) {
            for (int j = 0; j < h-1; j++) {
                Spring curr_spring = new Spring(h*j+i, h*(j+1)+i-1, spring_constant, resting_len*Mathf.Sqrt(2));
                springs[count] = curr_spring;
                count++;
            }
        }

        
        // Define Mesh Faces
        for (int i = 0; i < h-1; i++) {
        	for (int j = 0; j < w-1; j++) {
        		int start = 2*3*2*((i*(h-1)) + j);
        		tris[start    ] = (i*h) + j + 1;
        		tris[start + 1] = (i*h) + j + 0;
        		tris[start + 2] = (i+1)*h + j;

        		tris[start + 3] = (i*h) + j + 1;
        		tris[start + 4] = (i+1)*h + j;
        		tris[start + 5] = (i+1)*h + j + 1;

        		tris[start + 6] = (i*h) + j + 0;
        		tris[start + 7] = (i*h) + j + 1;
        		tris[start + 8] = (i+1)*h + j;

        		tris[start + 9] = (i+1)*h + j;
        		tris[start + 10] = (i*h) + j + 1;
        		tris[start + 11] = (i+1)*h + j + 1;
        	}
        } 

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;
        meshFilter.mesh = mesh;

    }

    private void DrawNet()
    {
        int spring_count = (w-1)*h + (h-1)*w;
        for (int i = 0; i < spring_count; i++) {
            Spring curr_spring = springs[i];
            if (!curr_spring.active) continue;
            int n1 = curr_spring.node1;
            int n2 = curr_spring.node2;
            Debug.DrawLine(nodes[n1].position, nodes[n2].position, Color.blue, Time.deltaTime);
        }

    }


    private void FixedUpdate()
    {   
        net_count++;
        ClearForces();
        GetPositions();
        

        Vector3[] curr_vertices = new Vector3[w*h];

        for (int i = 0; i < w*h; i ++) {
        	curr_vertices[i] = new Vector3(nodes[i].position.x, nodes[i].position.y, nodes[i].position.z);
        }
        mesh.vertices = curr_vertices;
    }

    private void ClearForces()
    {
        for (int i = 0; i < w*h; i++) {
            forces[i] = new Vector3(0.0f, -gravity*mass, 0.0f);
        }
    }

    public void Start()
    {
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        meshFilter = gameObject.AddComponent<MeshFilter>();

        mesh = new Mesh();
        Init();
    }

    private void GetPositions()
    {
    	// Aggregate Forces
        int spring_count = (w-1)*h + (h-1)*w + (w-2)*h + (h-2)*w + 2*(w-1)*(h-1);
        for (int i = 0; i < spring_count; i++) {
            Spring curr_spring = springs[i];
            int n1 = curr_spring.node1;
            int n2 = curr_spring.node2;
            Vector3 p1 = nodes[n1].position;
            Vector3 p2 = nodes[n2].position;
            Vector3 v1 = nodes[n1].velocity;
            Vector3 v2 = nodes[n2].velocity;
            float k = curr_spring.k;
            float l = curr_spring.x;
            float p12 = (p1-p2).magnitude;

            if (curr_spring.active ) {
                curr_spring.force = -( (k*(p12 - l)) + (dampening*(Vector3.Dot((v1-v2),(p1-p2))/p12))  ) * ((p1-p2) / p12);
            }
            forces[n1] += curr_spring.force;
            forces[n2] -= curr_spring.force;
            
            /// Attempt at Tearing
            /*
            if ((p12) > 1.3*l) {
            	curr_spring.active = false;
            }*/
        }

        /*
        // Naive n^2 Self Collisions
        for (int i = 0; i < w*h; i++) {
            for (int j = 0; j < w*h; j++) {
                Vector3 p1 = nodes[i].position;
                Vector3 p2 = nodes[j].position;
                Vector3 v1 = nodes[i].velocity;
                Vector3 v2 = nodes[j].velocity;
                if (i != j && (p1-p2).magnitude < 0.07) {
                    float k = 1000;
                    forces[i] -= v1*k/((p1-p2).magnitude+5f);
                    forces[j] -= v2*k/((p1-p2).magnitude+5f);
                }
            }
        }*/


        float sphere_r = sphere_trans.GetComponent<SphereCollider>().radius * sphere_trans.transform.localScale.x;
        Vector3 sphere_pos = sphere_trans.position;
        float cc = 10000;
        float cr = sphere_r + 0.1f;
        
        // Explicit Euler Integration
        for (int j = 0; j < w*h; j++) {
            if (j == 0 || j == w - 1 || j == w*h - 1 || j == w*h - w) {
                continue;
            } else {    
            	/*
            	// Constriant Based Contact
                forces[j] -= dampening*nodes[j].velocity;
                Vector3 a = forces[j]/mass;
                nodes[j].velocity = nodes[j].velocity + dt*a;
                nodes[j].position = nodes[j].position + dt*nodes[j].velocity; 
                if ((nodes[j].position - sphere_pos).magnitude < cr) {
                    
                    Vector3 diff = nodes[j].position - sphere_pos;
                    //forces[j] += (cr-diff.magnitude) * (diff/diff.magnitude) * cc;
                    nodes[j].position = sphere_pos + (diff*(cr)/diff.magnitude);
                }*/

                // Penalty Based Contact
                
                if ((nodes[j].position - sphere_pos).magnitude < cr) {
                    
                    Vector3 diff = nodes[j].position - sphere_pos;
                    forces[j] += (cr-diff.magnitude) * (diff/diff.magnitude) * cc;
                }
                forces[j] -= dampening*nodes[j].velocity;
                Vector3 a = forces[j]/mass;
                nodes[j].velocity = nodes[j].velocity + dt*a;
                nodes[j].position = nodes[j].position + dt*nodes[j].velocity; 
                     
            }
        }

    }

    


}
