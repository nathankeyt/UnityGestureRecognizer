using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Palmmedia.ReportGenerator.Core.Common;
using PDollarGestureRecognizer;
using UnityEngine;
using QDollarGestureRecognizer;
using Unity.PlasticSCM.Editor.WebApi;
using System.Text.Json;
using DG.Tweening;
using UnityEditorInternal;
using UnityEngine.Serialization;

public class CanvasObj : MonoBehaviour
{
    [SerializeField] private float minPointDistance = 0.1f;
    [SerializeField] private GameObject line;
    [SerializeField] private GameObject fizzleEffect;
    [SerializeField] private float gestureThreshold = 25f;
    
    private LineRenderer currLine;
    private int lineIndex = 0;
    private Vector3 prevPosition;
    private List<Gesture> storedGestures;
    private string currName = "default";
    private bool cancelDraw = false;
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        storedGestures = new List<Gesture>();
        
        var d = new DirectoryInfo(Application.dataPath + "/Runes/");
        foreach (var file in d.GetFiles("*.json"))
        {
            LoadFile(file.FullName);
        }
        
        print(storedGestures.Count);
    }

    private void LoadFile(string fileName)
    {
        print(fileName);
        var gesture = JsonUtility.FromJson<Gesture>(File.ReadAllText(fileName));
            
        gesture.Normalize(true);
        storedGestures.Add(gesture);
    }

    private void OnMouseDown()
    {
        cancelDraw = false;
        
        if (currLine)
        {
            Destroy(currLine.gameObject);
        }
        
        var pos = RaycastToPlane();
        currLine = Instantiate(line, pos, Quaternion.identity).GetComponent<LineRenderer>();
        lineIndex = 0;
        currLine.SetPosition(0, pos + (GetDirectionToMouse(pos) * 0.1f));

        mpb = new MaterialPropertyBlock();
        
        prevPosition = pos;
    }

    private void OnMouseDrag()
    {
        if (cancelDraw) return;
        
        var pos = RaycastToPlane();

        if (Vector3.Distance(prevPosition, pos) > minPointDistance) {
            currLine.positionCount++;
            currLine.SetPosition(++lineIndex, pos + (GetDirectionToMouse(pos) * 0.1f));
            prevPosition = pos;
        }
    }

    private void OnMouseExit()
    {
        cancelDraw = true;
        Compare();
    }

    private void OnMouseUp()
    {
        if (cancelDraw) return;
        
        Compare();
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
        
        var json = JsonUtility.ToJson(gesture);
        print(json);
        string path = Application.dataPath + "/Runes/" + currName + ".json";
        File.WriteAllText(path, json);

        LoadFile(path);
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
        if (storedGestures.Count > 0)
        {
            var result = QPointCloudRecognizer.Classify(loadGesture(), storedGestures);
            var type = result.Item1;
            var distance = result.Item2;

            if (distance < gestureThreshold)
            {
                if (type == "fire")
                {
                    currLine.material.SetColor("_Color", Color.red);
                }
                else if (type == "lightning")
                {
                    currLine.material.SetColor("_Color", Color.blue);
                }
                else if (type == "star")
                {
                    currLine.material.SetColor("_Color", Color.yellow);
                }
            }
            else
            {
                var m = new Mesh();

                currLine.BakeMesh(m);
                var fizzleParticles = Instantiate(fizzleEffect).GetComponent<ParticleSystem>();
                fizzleParticles.gameObject.transform.position = currLine.gameObject.transform.position;
                
                var shape = fizzleParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Mesh;
                shape.meshShapeType = ParticleSystemMeshShapeType.Vertex;
                shape.mesh = m;
                
                var lineRef = currLine;
                print("No gesture similar enough " + distance);
                var dissolveAmount = 0f;
                DOTween.To(() => dissolveAmount, x => dissolveAmount = x, 1f, 1)
                    .OnUpdate(() =>
                    {
                        lineRef.GetPropertyBlock(mpb);
                        
                        mpb.SetFloat("_Dissolve", dissolveAmount);
                        
                        lineRef.SetPropertyBlock(mpb);
                    });
            }
        }
        else
        {
            print("No gestures loaded");
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Submit"))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
