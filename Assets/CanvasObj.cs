using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using PDollarGestureRecognizer;
using UnityEngine;
using QDollarGestureRecognizer;
using Unity.PlasticSCM.Editor.WebApi;

public class CanvasObj : MonoBehaviour
{
    [SerializeField] private float minPointDistance = 0.1f;
    [SerializeField] private GameObject line;
    
    private LineRenderer currLine;
    private int lineIndex = 0;
    private Vector3 prevPosition;
    private List<Gesture> storedGestures;
    private string currName = "default";

    private void Start()
    {
        storedGestures = new List<Gesture>();
    }

    private void OnMouseDown()
    {
        if (currLine)
        {
            Destroy(currLine.gameObject);
        }
        
        var pos = RaycastToPlane();
        currLine = Instantiate(line, pos, Quaternion.identity).GetComponent<LineRenderer>();
        lineIndex = 0;
        currLine.SetPosition(0, pos + GetDirectionToMouse(pos));
        prevPosition = pos;
    }

    private void OnMouseDrag()
    {
        var pos = RaycastToPlane();

        if (Vector3.Distance(prevPosition, pos) > minPointDistance) {
            currLine.positionCount++;
            currLine.SetPosition(++lineIndex, pos + GetDirectionToMouse(pos));
            prevPosition = pos;
        }
    }
    
    private Vector3 GetDirectionToMouse(Vector3 pos)
    {
        var heading = Camera.main.ScreenToWorldPoint(Input.mousePosition) - pos;
        return heading / heading.magnitude;
    }

    private Vector3 RaycastToPlane()
    {
        var plane = new Plane(transform.up, transform.position);

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out var distance))
        {
            //print(plane.normal);
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    public void UpdateName(string s)
    {
        currName = s;
    }

    public void Submit()
    {
        var gesture = loadGesture();
        
        gesture.Normalize(true);
        
        storedGestures.Add(gesture);
    }

    public Gesture loadGesture()
    {
        int length = currLine.positionCount;
        
        var points = new Point[length];
        var positions = new Vector3[length];
        currLine.GetPositions(positions);
            
        for (int i = 0; i < length; i++)
        {
            var localPos = transform.InverseTransformPoint(positions[i]);
            points[i] = new Point(localPos.x, localPos.z, 0);
        }
        
        return new Gesture(points, currName);
    }

    public void Compare()
    {
        if (storedGestures.Count > 0) {
            print(QPointCloudRecognizer.Classify(loadGesture(), storedGestures));
        }
        else
        {
            print("No gestures loaded");
        }
    }

    void Update()
    {
        
    }
}
