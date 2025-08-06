using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape : MonoBehaviour
{
    public struct Data
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3 Color;
        public int ShapeType;
        public int Operation;
        public float BlendStrength;
        public int NumChildren;
    }

    public enum Type {Sphere, Cube, Torus};
    public enum OperationType {None, Blend, Cut, Mask};

    public Type ShapeType;
    public OperationType Operation;
    public Color Color = Color.white;
    [Range(0,1)]
    public float BlendStrength;
    [HideInInspector]
    public int NumChildren;

    public bool ShowWireframes = false;

    public Vector3 Position {
        get {
            return transform.position;
        }
    }

    public Vector3 Rotation
    {
        get
        {
            return transform.eulerAngles;
        }
    }

    public Vector3 Scale {
        get {
            Vector3 parentScale = Vector3.one;
            if (transform.parent != null && transform.parent.GetComponent<Shape>() != null) {
                parentScale = transform.parent.GetComponent<Shape>().Scale;
            }
            return Vector3.Scale(transform.localScale, parentScale);
        }
    }

    /*
    public Vector3 Scale {
        get {
            return transform.lossyScale;
        }
    }
    */

    public Data ToStruct() {
        Data data = new Data();
        data.Position = Position;
        data.Rotation = Rotation;
        data.Scale = Scale;
        data.Color = new Vector3(Color.r, Color.g, Color.b);
        data.ShapeType = (int)ShapeType;
        data.Operation = (int)Operation;
        data.BlendStrength = BlendStrength; // * 3
        data.NumChildren = NumChildren;
        return data;
    }

    private void OnDrawGizmos()
    {
        if (!ShowWireframes) return;

        Gizmos.color = Color;

        // Create de transformation matrix with position, rotation and scale
        Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = matrix;

        switch (ShapeType)
        {
            case Type.Sphere:
                Gizmos.DrawWireSphere(Vector3.zero, Scale.x);
                break;

            case Type.Cube:
                Gizmos.DrawWireCube(Vector3.zero, 2 * Scale);
                break;
        }

        // Restore the matriz to not affect other gizmos
        Gizmos.matrix = Matrix4x4.identity;
    }

    public override string ToString()
    {
        return $"Shape:\n" +
            $"- Type: {ShapeType}\n" +
            $"- Position: {Position}\n" +
            $"- Rotation: {Rotation}\n" +
            $"- Scale: {Scale}\n" +
            $"- Operation: {Operation}\n" +
            $"- Color: (R: {Color.r:F2}, G: {Color.g:F2}, B: {Color.b:F2})\n" +
            $"- BlendStrength: {BlendStrength:F2}\n" +
            $"- NumChildren: {NumChildren}";
    }
}