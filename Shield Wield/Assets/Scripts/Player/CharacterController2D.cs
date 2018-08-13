using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour {

    public LayerMask onGroundLayerMask;
    public float onGroundRaycastDistance = 0.1f;

    CapsuleCollider2D coll;
    Rigidbody2D rb;
    ContactFilter2D contactFilter;
    RaycastHit2D[] hitResults = new RaycastHit2D[5];
    RaycastHit2D[] foundHits = new RaycastHit2D[3];
    Collider2D[] groundColliders = new Collider2D[3];
    Vector2[] raycastPositions = new Vector2[3];
    Vector2 previousPosition;
    Vector2 currentPosition;
    Vector2 nextMovement;

    public bool OnGround { get; protected set; }
    public bool OnCeiling { get; protected set; }
    public Vector2 Velocity { get; protected set; }
    public Rigidbody2D RB { get { return rb; } }
    public Collider2D[] GroundColliders { get { return groundColliders; } }
    public ContactFilter2D ContactFilter { get { return contactFilter; } }


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();

        currentPosition = rb.position;
        previousPosition = rb.position;

        contactFilter.layerMask = onGroundLayerMask;
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = false;

        Physics2D.queriesStartInColliders = false;
    }

    void FixedUpdate()
    {
        previousPosition = rb.position;
        currentPosition = previousPosition + nextMovement;
        Velocity = (currentPosition - previousPosition) / Time.deltaTime;

        rb.MovePosition(currentPosition);
        nextMovement = Vector2.zero;

        CheckCapsuleEndCollisions();
        CheckCapsuleEndCollisions(false);
    }

    public void Move(Vector2 movement)
    {
        nextMovement += movement;
    }

    public void CheckCapsuleEndCollisions(bool bottom = true)
    {
        Vector2 raycastDirection;
        Vector2 raycastStart;
        float raycastDistance;

        if (coll == null)
        {
            raycastStart = rb.position + Vector2.up;
            raycastDistance = 1f + onGroundRaycastDistance;

            if (bottom)
            {
                raycastDirection = Vector2.down;

                raycastPositions[0] = raycastStart + Vector2.left * 0.4f;
                raycastPositions[1] = raycastStart;
                raycastPositions[2] = raycastStart + Vector2.right * 0.4f;
            }
            else
            {
                raycastDirection = Vector2.up;

                raycastPositions[0] = raycastStart + Vector2.left * 0.4f;
                raycastPositions[1] = raycastStart;
                raycastPositions[2] = raycastStart + Vector2.right * 0.4f;
            }
        }
        else
        {
            raycastStart = rb.position + coll.offset;
            raycastDistance = coll.size.x * .5f + onGroundRaycastDistance * 2f;

            if (bottom)
            {
                raycastDirection = Vector2.down;
                Vector2 raycastStartBottomCenter = raycastStart + Vector2.down * (coll.size.y * 0.5f - coll.size.x * 0.5f);

                raycastPositions[0] = raycastStartBottomCenter + Vector2.left * coll.size.x * 0.5f;
                raycastPositions[1] = raycastStartBottomCenter;
                raycastPositions[2] = raycastStartBottomCenter + Vector2.right * coll.size.x * 0.5f;
            }
            else
            {
                raycastDirection = Vector2.up;
                Vector2 raycastStartTopCenter = raycastStart + Vector2.up * (coll.size.y * 0.5f - coll.size.x * 0.5f);

                raycastPositions[0] = raycastStartTopCenter + Vector2.left * coll.size.x * 0.5f;
                raycastPositions[1] = raycastStartTopCenter;
                raycastPositions[2] = raycastStartTopCenter + Vector2.right * coll.size.x * 0.5f;
            }
        }

        for (int i = 0; i < raycastPositions.Length; i++)
        {
            int count = Physics2D.Raycast(raycastPositions[i], raycastDirection, contactFilter, hitResults, raycastDistance);

            if (bottom)
            {
                foundHits[i] = count > 0 ? hitResults[0] : new RaycastHit2D();
                groundColliders[i] = foundHits[i].collider;
            }
            else
            {
                OnCeiling = false;

                for (int j = 0; j < hitResults.Length; j++)
                {
                    if (hitResults[j].collider != null)
                    {
                        if (!PhysicsHelper.ColliderHasPlatformEffector(hitResults[j].collider))
                        {
                            OnCeiling = true;
                        }
                    }
                }
            }
        }

        if (bottom)
        {
            Vector2 groundNormal = Vector2.zero;
            int hitCount = 0;

            for (int i = 0; i < foundHits.Length; i++)
            {
                if (foundHits[i].collider != null)
                {
                    groundNormal += foundHits[i].normal;
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
                groundNormal.Normalize();
            }

            Vector2 relativeVelocity = rb.velocity;

            /*
            for (int i = 0; i < groundColliders.Length; i++)
            {
                if (groundColliders[i] == null)
                {
                    continue;
                }

                MovingPlatform movingPlatform;

                if (PhysicsHelper.TryGetMovingPlatform(groundColliders[i], out movingPlatform))
                {
                    relativeVelocity -= movingPlatform.Velocity / Time.deltaTime;
                    break;
                }
            }
            */

            if (Mathf.Approximately(groundNormal.x, 0f) && Mathf.Approximately(groundNormal.y, 0f))
            {
                OnGround = false;
            }
            else
            {
                OnGround = (relativeVelocity.y <= 0f);

                if (coll != null)
                {
                    if (groundColliders[1] != null)
                    {
                        float colliderBottomHeight = rb.position.y + coll.offset.y - coll.size.y * 0.5f;
                        float middleHitHeight = foundHits[1].point.y;
                        OnGround &= (middleHitHeight < colliderBottomHeight + onGroundRaycastDistance);
                    }
                }
            }
        }

        for (int i = 0; i < hitResults.Length; i++)
        {
            hitResults[i] = new RaycastHit2D();
        }
    }
}
