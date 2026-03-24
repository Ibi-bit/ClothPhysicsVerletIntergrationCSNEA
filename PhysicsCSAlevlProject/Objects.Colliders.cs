using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public abstract class Collider
{
    public virtual Vector2 Position { get; set; }
    public abstract bool ContainsPoint(Vector2 point, out Vector2 closestPoint);
    public abstract Collider DeepCopy();

    public virtual void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch) { }
}

public class CircleCollider : Collider
{
    public float Radius;
    public Rectangle BroadPhase =>
        new Rectangle(
            (int)(Position.X - Radius),
            (int)(Position.Y - Radius),
            (int)(Radius * 2),
            (int)(Radius * 2)
        );

    public CircleCollider(Vector2 center, float radius)
    {
        Position = center;
        Radius = radius;
    }

    public override Collider DeepCopy() => new CircleCollider(Position, Radius);

    public override bool ContainsPoint(Vector2 point, out Vector2 closestPoint)
    {
        if (!BroadPhase.Contains(point))
        {
            closestPoint = point;
            return false;
        }
        Vector2 direction = point - Position;
        float distance = direction.LengthSquared();

        if (distance <= Radius * Radius)
        {
            closestPoint = Position + Vector2.Normalize(direction) * Radius;
            return true;
        }

        closestPoint = point;
        return false;
    }

    public override void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        var circle = new PrimitiveBatch.Circle(Position, Radius, Color.Red, false);
        circle.Draw(spriteBatch, primitiveBatch);
    }
}

public class RectangleCollider(Rectangle rectangle) : Collider
{
    public Rectangle Rectangle = rectangle;

    public override Collider DeepCopy() => new RectangleCollider(Rectangle);

    public override bool ContainsPoint(Vector2 point, out Vector2 closestPoint)
    {
        if (Rectangle.Contains(point))
        {
            float leftDist = point.X - Rectangle.Left;
            float rightDist = Rectangle.Right - point.X;
            float topDist = point.Y - Rectangle.Top;
            float bottomDist = Rectangle.Bottom - point.Y;

            float minDist = leftDist;
            closestPoint = new Vector2(Rectangle.Left, point.Y);

            if (rightDist < minDist)
            {
                minDist = rightDist;
                closestPoint = new Vector2(Rectangle.Right, point.Y);
            }
            if (topDist < minDist)
            {
                minDist = topDist;
                closestPoint = new Vector2(point.X, Rectangle.Top);
            }
            if (bottomDist < minDist)
            {
                closestPoint = new Vector2(point.X, Rectangle.Bottom);
            }
            return true;
        }
        closestPoint = point;

        return false;
    }

    public override void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        var rect = new PrimitiveBatch.Rectangle(Rectangle, Color.Red);
        rect.Draw(spriteBatch, primitiveBatch);
    }
}

public class SeperatedAxisRectangleCollider : PolygonSeperatedAxisCollider
{
    public Vector2[] Axis { get; private set; } = new Vector2[4];
    Vector2[] _axisPrivate = new Vector2[4];
    public float HalfWidth;
    public float HalfHeight;
    private Vector2 _position;
    private float angle;
    private PrimitiveBatch.Rectangle rectangleDraw;

    public override Vector2 Position
    {
        get { return _position; }
        set
        {
            _position = value;
            if (rectangleDraw != null)
            {
                rectangleDraw = new PrimitiveBatch.Rectangle(
                    new Rectangle(
                        (int)(_position.X - HalfWidth),
                        (int)(_position.Y - HalfHeight),
                        (int)(HalfWidth * 2),
                        (int)(HalfHeight * 2)
                    ),
                    Color.Red
                )
                {
                    rotation = angle,
                };
            }
        }
    }

    public float Angle
    {
        get { return angle; }
        set
        {
            angle = value;
            SetAxis();
            if (rectangleDraw != null)
            {
                rectangleDraw.rotation = angle;
            }
        }
    }

    public override Collider DeepCopy() =>
        new SeperatedAxisRectangleCollider(
            new Rectangle(
                (int)(Position.X - HalfWidth),
                (int)(Position.Y - HalfHeight),
                (int)(HalfWidth * 2),
                (int)(HalfHeight * 2)
            ),
            angle
        );

    private void SetAxis()
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);

        Axis[0] = new Vector2(cos, sin);
        Axis[1] = new Vector2(-sin, cos);
        Axis[2] = -Axis[0];
        Axis[3] = -Axis[1];
        Vertices =
        [
            new Vector2(-HalfWidth, -HalfHeight),
            new Vector2(HalfWidth, -HalfHeight),
            new Vector2(HalfWidth, HalfHeight),
            new Vector2(-HalfWidth, HalfHeight),
        ];
    }

    public SeperatedAxisRectangleCollider(Rectangle rectangle, float angle)
        : base(
            [
                new Vector2(-rectangle.Width / 2f, -rectangle.Height / 2f),
                new Vector2(rectangle.Width / 2f, -rectangle.Height / 2f),
                new Vector2(rectangle.Width / 2f, rectangle.Height / 2f),
                new Vector2(-rectangle.Width / 2f, rectangle.Height / 2f),
            ]
        )
    {
        HalfWidth = rectangle.Width / 2f;
        HalfHeight = rectangle.Height / 2f;
        _position = new Vector2(
            rectangle.X + rectangle.Width / 2,
            rectangle.Y + rectangle.Height / 2
        );
        rectangleDraw = new PrimitiveBatch.Rectangle(rectangle, Color.Red);
        Angle = angle;
        Vertices =
        [
            new Vector2(-HalfWidth, -HalfHeight),
            new Vector2(HalfWidth, -HalfHeight),
            new Vector2(HalfWidth, HalfHeight),
            new Vector2(-HalfWidth, HalfHeight),
        ];

        SetAxis();
    }

    public override bool ContainsPoint(Vector2 point, out Vector2 closestPoint)
    {
        Vector2 localPoint = point - Position;
        float xProjection = Vector2.Dot(localPoint, Axis[0]);
        float yProjection = Vector2.Dot(localPoint, Axis[1]);

        float clampedX = Math.Clamp(xProjection, -HalfWidth, HalfWidth);
        float clampedY = Math.Clamp(yProjection, -HalfHeight, HalfHeight);

        bool isInside =
            (Math.Abs(xProjection - clampedX) < 0.001f)
            && (Math.Abs(yProjection - clampedY) < 0.001f);

        if (!isInside)
        {
            closestPoint = point;
            return false;
        }

        float dx = HalfWidth - Math.Abs(xProjection);
        float dy = HalfHeight - Math.Abs(yProjection);

        if (dx < dy)
        {
            float targetX = Math.Sign(xProjection) * HalfWidth;
            closestPoint = Position + Axis[0] * targetX + Axis[1] * yProjection;
        }
        else
        {
            float targetY = Math.Sign(yProjection) * HalfHeight;
            closestPoint = Position + Axis[0] * xProjection + Axis[1] * targetY;
        }

        return true;
    }

    public override void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        rectangleDraw.Draw(spriteBatch, primitiveBatch);
    }
}

public class PolygonSeperatedAxisCollider : Collider
{
    public Vector2[] Vertices;
    public Vector2[] worldVertices;
    private List<Vector2[]> Triangles = new();
    private Vector2[] _axisPrivate;
    private float _angle;
    private Rectangle broadPhase;
    public float MaxArea = 1000f;

    public float angle
    {
        get { return _angle; }
        set
        {
            _angle = value;
            if (Vertices == null || _axisPrivate == null)
                return;

            float cos = MathF.Cos(_angle);
            float sin = MathF.Sin(_angle);
            for (int i = 0; i < Vertices.Length; i++)
            {
                _axisPrivate[i] = new Vector2(
                    Vertices[i].X * cos - Vertices[i].Y * sin,
                    Vertices[i].X * sin + Vertices[i].Y * cos
                );
            }
        }
    }

    public override Collider DeepCopy() =>
        new PolygonSeperatedAxisCollider(Vertices.Select(v => v).ToArray())
        {
            Position = Position,
            angle = angle,
        };

    public PolygonSeperatedAxisCollider(Vector2[] vertices)
    {
        Vertices = vertices ?? Array.Empty<Vector2>();
        worldVertices = vertices;
        _axisPrivate = new Vector2[Vertices.Length];

        broadPhase = CreateBroadPhase(vertices);

        List<Vector2> vertsList = Vertices.ToList();

        while (vertsList.Count > 3)
        {
            bool foundEar = false;
            for (int i = 0; i < vertsList.Count; i++)
            {
                var A = vertsList[i];
                var B = vertsList[(i + 1) % vertsList.Count];
                var C = vertsList[(i + 2) % vertsList.Count];
                if (IsEar(A, B, C, vertsList.ToArray()))
                {
                    Triangles.Add([A, B, C]);
                    vertsList.RemoveAt((i + 1) % vertsList.Count);
                    foundEar = true;
                    break;
                }
            }
            if (!foundEar)
                break;
            Triangles.Add(vertsList.Take(3).ToArray());
        }
    }

    private static Rectangle CreateBroadPhase(Vector2[] vertices)
    {
        Vector2 xmin = Vector2.Zero;
        Vector2 xmax = Vector2.Zero;
        foreach (var v in vertices)
        {
            if (v.X < xmin.X)
                xmin.X = v.X;
            if (v.Y < xmin.Y)
                xmin.Y = v.Y;
            if (v.X > xmax.X)
                xmax.X = v.X;
            if (v.Y > xmax.Y)
                xmax.Y = v.Y;
        }
        return new Rectangle(
            (int)xmin.X,
            (int)xmin.Y,
            (int)(xmax.X - xmin.X),
            (int)(xmax.Y - xmin.Y)
        );
    }

    private bool IsEar(Vector2 A, Vector2 B, Vector2 C, Vector2[] points)
    {
        var cross = (B.X - A.X) * (C.Y - B.Y) - (B.Y - A.Y) * (C.X - B.X);
        if (cross <= 0)
            return false;
        foreach (var P in points)
        {
            if (P == A || P == B || P == C)
                continue;
            if (PointInTriangle(P, A, B, C))
                return false;
        }
        return true;
    }

    private bool PointInTriangle(Vector2 P, Vector2 A, Vector2 B, Vector2 C)
    {
        var areaABC = Area(A, B, C);
        var areaPAB = Area(P, A, B);
        var areaPBC = Area(P, B, C);
        var areaPCA = Area(P, C, A);
        return Math.Abs(areaABC - (areaPAB + areaPBC + areaPCA)) < 0.01f;
    }

    private Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
    {
        Vector2 AB = B - A;
        float t = Vector2.Dot(P - A, AB) / AB.LengthSquared();
        t = MathHelper.Clamp(t, 0f, 1f);
        return A + AB * t;
    }

    private float Area(Vector2 A, Vector2 B, Vector2 C)
    {
        return Math.Abs((A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y)) / 2.0f);
    }

    public override bool ContainsPoint(Vector2 point, out Vector2 closestPoint)
    {
        closestPoint = point;
        if (Vertices == null || Vertices.Length < 3)
        {
            return false;
        }
        if (!broadPhase.Contains(point))
        {
            return false;
        }

        for (int i = 0; i < Vertices.Length; i++)
        {
            worldVertices[i] = Vertices[i] + Position;
        }

        bool isInside = IsPointInPolygon(point, worldVertices);

        float closestDistanceSq = float.MaxValue;
        Vector2 bestPoint = point;

        for (int i = 0; i < worldVertices.Length; i++)
        {
            Vector2 a = worldVertices[i];
            Vector2 b = worldVertices[(i + 1) % worldVertices.Length];
            Vector2 projected = GetClosestPointOnLineSegment(a, b, point);

            float distSq = Vector2.DistanceSquared(point, projected);
            if (distSq < closestDistanceSq)
            {
                closestDistanceSq = distSq;
                bestPoint = projected;
            }
        }

        closestPoint = bestPoint;
        return isInside;
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            bool intersects =
                ((pi.Y > point.Y) != (pj.Y > point.Y))
                && (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / ((pj.Y - pi.Y) + 1e-6f) + pi.X);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public override void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        for (int i = 0; i < worldVertices.Length; i++)
        {
            var line = new PrimitiveBatch.Line(
                worldVertices[i],
                worldVertices[(i + 1) % worldVertices.Length],
                Color.Red,
                2
            );
            line.Draw(spriteBatch, primitiveBatch);
        }
    }
}
