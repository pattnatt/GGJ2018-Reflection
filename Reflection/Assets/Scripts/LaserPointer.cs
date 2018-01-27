﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LineRenderer))]
public class LaserPointer : MonoBehaviour {

    public GameObject firePositionObject;
    public float lineLength = 100;
    public int maxReflectCount = 100;
    public bool isAlwaysActive = false;
    public bool isAlwaysActiveAfterHit = false;

    private bool isActive = false;
    private LineRenderer lineRenderer;

    public LaserPointer (GameObject firePositionObject, LineRenderer lineRenderer) {
        this.firePositionObject = firePositionObject;
        this.lineRenderer = lineRenderer;
    }

    // Use this for initialization
    void Start () {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.SetColors(Color.white, Color.white);
        Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
        lineRenderer.material = whiteDiffuseMat;
    }
	
	// Update is called once per frame
	void Update () {
        Vector2 firePosition = firePositionObject.transform.position;
        float fireAngle = firePositionObject.transform.eulerAngles.z;
        float xFireVector = Mathf.Cos(fireAngle / 180f * Mathf.PI);
        float yFireVector = Mathf.Sin(fireAngle / 180f * Mathf.PI);
        Vector2 fireDirection = new Vector2(xFireVector, yFireVector);

        if (isAlwaysActive || isActive) {
            DrawLaser(firePosition, fireDirection, false);
        }
    }

    public void Activate () {
        isActive = true;
    }

    public void Deactivate () {
        isActive = false;
        ResetLineRenderer();
    }

    public void Attack () {
        Vector2 firePosition = firePositionObject.transform.position;
        float fireAngle = firePositionObject.transform.eulerAngles.z;
        float xFireVector = Mathf.Cos(fireAngle / 180f * Mathf.PI);
        float yFireVector = Mathf.Sin(fireAngle / 180f * Mathf.PI);
        Vector2 fireDirection = new Vector2(xFireVector, yFireVector);
        DrawLaser(firePosition, fireDirection, true);
    }

    private void DrawLaser (Vector2 firePosition, Vector2 fireDirection, bool isAttack) {
        ResetLineRenderer();
        AddPositionToLineRenderer(firePosition);
        bool isLineEnd = false;
        GameObject lastHitObject = null;

        Vector2 currentPosition = new Vector2(firePosition.x, firePosition.y);
        Vector2 currentDirection = new Vector2(fireDirection.x, fireDirection.y);

        RaycastHit2D objectHitData = Physics2D.Raycast(currentPosition, currentDirection, lineLength, 1 << StaticVar.LAYER_BLOCK);

        while (objectHitData && lineRenderer.positionCount <= maxReflectCount) {
            if (isAttack) {
                DetectEnemy(currentPosition, currentDirection, objectHitData.distance, 1);
            }
            lastHitObject = objectHitData.collider.gameObject;

            BlockAdapter blockAdapter = lastHitObject.GetComponent<BlockAdapter>();
            if(blockAdapter) blockAdapter.HitByLaser();

            AddPositionToLineRenderer(new Vector3(objectHitData.point.x, objectHitData.point.y));

            if (objectHitData.collider.gameObject.tag == StaticVar.BLOCK_TAG_REFLECTIVE) {
                Vector2 reflectDirection = Vector2.Reflect(currentDirection, objectHitData.normal);
                //print(objectHit.collider.gameObject.ToString() + " " + reflectDirection.ToString());

                currentPosition = new Vector2(objectHitData.point.x, objectHitData.point.y);
                currentDirection = reflectDirection;
            }
            else {
                isLineEnd = true;
                break;
            }

            lastHitObject.GetComponent<Collider2D>().enabled = false;
            objectHitData = Physics2D.Raycast(currentPosition, currentDirection, lineLength, 1 << StaticVar.LAYER_BLOCK);
            lastHitObject.GetComponent<Collider2D>().enabled = true;
        }

        if (!isLineEnd) {
            if (isAttack) {
                DetectEnemy(currentPosition, currentDirection, lineLength, 1);
            }
            AddPositionToLineRenderer(new Vector3(currentPosition.x + (currentDirection.x * lineLength),
                                       currentPosition.y + (currentDirection.y * lineLength)));
        }
    }

    private void AddPositionToLineRenderer(Vector2 position) {
        lineRenderer.positionCount += 1;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(position.x, position.y, -5));
    }

    private void ResetLineRenderer () {
        lineRenderer.positionCount = 0;
    }

    private void DetectEnemy (Vector2 position, Vector2 direction, float range, int damage) {
        GameObject lastEnemyHit = null;
        RaycastHit2D enemyHitData = Physics2D.Raycast(position, direction, range, 1 << StaticVar.LAYER_ENEMY);

        while (enemyHitData) {
            lastEnemyHit = enemyHitData.collider.gameObject;
            print(lastEnemyHit.ToString());
            range -= enemyHitData.distance;

            lastEnemyHit.GetComponent<Collider2D>().enabled = false;
            enemyHitData = Physics2D.Raycast(enemyHitData.point, direction, range, 1 << StaticVar.LAYER_ENEMY);
            lastEnemyHit.GetComponent<Collider2D>().enabled = true;

            Enemy enemy = lastEnemyHit.GetComponent<Enemy>();
            if (enemy) {
                enemy.GetHit(damage);
            }
        }
    }
}
