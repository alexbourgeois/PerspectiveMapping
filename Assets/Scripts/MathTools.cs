using UnityEngine;

public class MathTools {
        public static Vector2 Barycenter(Vector2[] points) {
            Vector2 barycenter = Vector2.zero;
            foreach (Vector2 point in points) {
                barycenter += point;
            }
            return barycenter / points.Length;
        }

        public static Vector2 Normal(Vector2 v) {
            Vector2 normal = Vector2.zero;
            normal.x = -v.y;
            normal.y = v.x;
            return normal;
        }

        // Intersection of diagonals of a quadrilateral
        // assumes corners ordered along the perimeter, for example:
        // 0---1
        // |   |
        // |   2
        // |  /
        // | /
        // |/
        // 3
        public static Vector2 QuadrilateralCenter(Vector2[] corners) {
            // TODO: self intersecting special case
            // TODO: some corners have same positions -> barycenter?
            Vector2 center = Vector2.zero;
            Vector2 a = corners[0];
            Vector2 b = corners[1];
            Vector2 c = corners[2];
            Vector2 d = corners[3];

            // Debug.Log($"Quadrilateral: a=({a.x},{a.y}), b=({b.x},{b.y}), c=({c.x},{c.y}), d=({d.x},{d.y})");

            float t =   (
                          (b.y-d.y) * (c.x-d.x)
                         +(d.y-c.y) * (b.x-d.x)
                        )
                      /
                        (
                          (a.y-c.y) * (b.x-d.x)
                         -(b.y-d.y) * (a.x-c.x)
                        )
                      ;

            // Debug.Log($"t = {t}");

            center.x = c.x + (a.x-c.x) * t;
            center.y = c.y + (a.y-c.y) * t;
            // Debug.Log($"Center: ({center.x},{center.y})");
            return center;
        }
    
}



