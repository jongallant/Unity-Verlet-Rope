using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    LineRenderer LineRenderer;
    Vector3[] LinePositions;

    private List<RopeNode> RopeNodes = new List<RopeNode>();
    private float NodeDistance = 0.2f;
    private int TotalNodes = 50;
    private float RopeWidth = 0.1f;

    Camera Camera;

    int LayerMask = 1;
    ContactFilter2D ContactFilter;    
    RaycastHit2D[] RaycastHitBuffer = new RaycastHit2D[10];
    Collider2D[] ColliderHitBuffer = new Collider2D[10];

    Vector3 Gravity = new Vector2(0f, -5f);
    Vector2 Node1Lock;

    void Awake()
    {
        Camera = Camera.main;

        ContactFilter = new ContactFilter2D
        {
            layerMask = LayerMask,
            useTriggers = false,
        };

        LineRenderer = this.GetComponent<LineRenderer>();

        // Generate some rope nodes based on properties
        Vector3 startPosition = Vector2.zero;
        for (int i = 0; i < TotalNodes; i++)
        {            
            RopeNode node = (GameObject.Instantiate(Resources.Load("RopeNode") as GameObject)).GetComponent<RopeNode>();
            node.transform.position = startPosition;
            node.PreviousPosition = startPosition;
            RopeNodes.Add(node);

            startPosition.y -= NodeDistance;
        }

        // for line renderer data
        LinePositions = new Vector3[TotalNodes];
    }


    void Update()
    {
        // Attach rope end to mouse click position
        if (Input.GetMouseButtonDown(0))
        {
            Node1Lock = Camera.ScreenToWorldPoint(Input.mousePosition);
        }

        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();
                       
        // Higher iteration results in stiffer ropes and stable simulation
        for (int i = 0; i < 80; i++)
        {
            ApplyConstraint();

            // Playing around with adjusting collisions at intervals - still stable when iterations are skipped
            if (i % 2 == 1)
                AdjustCollisions();
        }
    }

    private void Simulate()
    {
        // step each node in rope
        for (int i = 0; i < TotalNodes; i++)
        {            
            // derive the velocity from previous frame
            Vector3 velocity = RopeNodes[i].transform.position - RopeNodes[i].PreviousPosition;
            RopeNodes[i].PreviousPosition = RopeNodes[i].transform.position;

            // calculate new position
            Vector3 newPos = RopeNodes[i].transform.position + velocity;
            newPos += Gravity * Time.fixedDeltaTime;
            Vector3 direction = RopeNodes[i].transform.position - newPos;
                        
            // cast ray towards this position to check for a collision
            int result = -1;
            result = Physics2D.CircleCast(RopeNodes[i].transform.position, RopeNodes[i].transform.localScale.x / 2f, -direction.normalized, ContactFilter, RaycastHitBuffer, direction.magnitude);

            if (result > 0)
            {
                for (int n = 0; n < result; n++)
                {                    
                    if (RaycastHitBuffer[n].collider.gameObject.layer == 9)
                    {
                        Vector2 collidercenter = new Vector2(RaycastHitBuffer[n].collider.transform.position.x, RaycastHitBuffer[n].collider.transform.position.y);
                        Vector2 collisionDirection = RaycastHitBuffer[n].point - collidercenter;
                        // adjusts the position based on a circle collider
                        Vector2 hitPos = collidercenter + collisionDirection.normalized * (RaycastHitBuffer[n].collider.transform.localScale.x / 2f + RopeNodes[i].transform.localScale.x / 2f);
                        newPos = hitPos;
                        break;              //Just assuming a single collision to simplify the model
                    }
                }
            }

            RopeNodes[i].transform.position = newPos;
        }
    }
    
    private void AdjustCollisions()
    {
        // Loop rope nodes and check if currently colliding
        for (int i = 0; i < TotalNodes - 1; i++)
        {
            RopeNode node = this.RopeNodes[i];

            int result = -1;
            result = Physics2D.OverlapCircleNonAlloc(node.transform.position, node.transform.localScale.x / 2f, ColliderHitBuffer);

            if (result > 0)
            {
                for (int n = 0; n < result; n++)
                {
                    if (ColliderHitBuffer[n].gameObject.layer != 8)
                    {
                        // Adjust the rope node position to be outside collision
                        Vector3 collidercenter = ColliderHitBuffer[n].transform.position;
                        Vector3 collisionDirection = node.transform.position - collidercenter;

                        Vector3 hitPos = collidercenter + collisionDirection.normalized * ((ColliderHitBuffer[n].transform.localScale.x / 2f) + (node.transform.localScale.x / 2f));
                        node.transform.position = hitPos;
                        break;
                    }
                }
            }
        }    
    }

    private void ApplyConstraint()
    {
        // Check if the first node is clamped to the scene or is follwing the mouse
        if (Node1Lock != Vector2.zero)
        {
            RopeNodes[0].transform.position = Node1Lock;
        }
        else
        {
            RopeNodes[0].transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        for (int i = 0; i < TotalNodes - 1; i++)
        {
            RopeNode node1 = this.RopeNodes[i];
            RopeNode node2 = this.RopeNodes[i + 1];

            // Get the current distance between rope nodes
            float currentDistance = (node1.transform.position - node2.transform.position).magnitude;
            float difference = Mathf.Abs(currentDistance - NodeDistance);
            Vector2 direction = Vector2.zero;
           
            // determine what direction we need to adjust our nodes
            if (currentDistance > NodeDistance)
            {
                direction = (node1.transform.position - node2.transform.position).normalized;
            }
            else if (currentDistance < NodeDistance)
            {
                direction = (node2.transform.position - node1.transform.position).normalized;
            }

            // calculate the movement vector
            Vector3 movement = direction * difference;

            // apply correction
            node1.transform.position -= (movement * 0.5f);
            node2.transform.position += (movement * 0.5f);
        }
    }

    private void DrawRope()
    {
        LineRenderer.startWidth = RopeWidth;
        LineRenderer.endWidth = RopeWidth;

        for (int n = 0; n < TotalNodes; n++)
        {
            LinePositions[n] = new Vector3(RopeNodes[n].transform.position.x, RopeNodes[n].transform.position.y, 0);
        }

        LineRenderer.positionCount = LinePositions.Length;
        LineRenderer.SetPositions(LinePositions);
    }

}
