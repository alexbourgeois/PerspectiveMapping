using UnityEngine;
using static MathTools;

class Handles {
    public Handle none = new Handle();
    public Corner[] corners = new Corner[4];
    public CircleCorner[] circleCorners = new CircleCorner[4];
    public Center center;

    public Handle current;

    public float magneticDistance = 0.2f;

    private Handle[] all = new Handle[9]; // Corners, Center and Circle Corners

    public void SelectNone() {
        this.current = this.none;
    }

    public void SelectClosestHandle(Vector2 mousePos) {
        this.current = GetClosestHandle(mousePos);
    }

    public Handles() {
        int hIdx = 0;

        for (int i = 0; i < corners.Length; i++) {
            this.corners[i] = new Corner(Vector2.zero);
            this.all[hIdx++] = this.corners[i];
        }

        this.center = new Center(this.corners);
        this.all[hIdx++] = this.center;

        for (int i = 0; i < this.corners.Length; i++) {
            this.circleCorners[i] = new CircleCorner(
                    this.corners[i],
                    this.center,
                    this.corners
                    );
            this.all[hIdx++] = this.circleCorners[i];
        }

        this.current = this.none;
    }

    public class Handle {
        public HandleType type = HandleType.None;
        protected Vector2 _position = Vector2.zero;

        public virtual void SetPosition(Vector2 pos) {
            _position = pos;
        }

        public virtual Vector2 GetPosition() {
            return _position;
        }
    }

    public class Center: Handle {
        private Corner[] _corners;

        public Center(Corner[] corners) {
            this.type = HandleType.Center;
            this._corners = corners;
        }

        // translate all corners
        public override void SetPosition(Vector2 pos) {
            Vector2 translation = pos - GetPosition();
            Debug.Log(translation);
            foreach (Corner corner in _corners) {
                corner.SetPosition(corner.GetPosition() + translation);
            }
        }

        // Intersection of diagonals between corners
        public override Vector2 GetPosition() {
            Vector2[] cornerPositions = new Vector2[4];
            for (int i = 0; i < this._corners.Length; i++) {
                cornerPositions[i] = this._corners[i].GetPosition();
            }
            return MathTools.QuadrilateralCenter(cornerPositions);
        }
    }

    public class Corner: Handle {
        public Corner(Vector2 pos) {
            this.type = HandleType.Corner;
            this.SetPosition(pos);
        }
    }

    public class CircleCorner: Handle {
        private Corner _corner;
        private Center _center;
        private Corner[] _corners;

        public CircleCorner(Corner corner, Center center, Corner[] corners) {
            this.type = HandleType.CircleCorner;
            this._corner = corner;
            this._center = center;
            this._corners = corners;
        }

        // set corresponding corner position
        public override void SetPosition(Vector2 pos) {
            Vector2[] circleCornerPos = new Vector2[4];
            for (int i = 0; i < _corners.Length; i++) {
                if (_corners[i] == this._corner)
                    circleCornerPos[i] = pos;
                else
                    circleCornerPos[i] = GetCircleCornerPosition(_corners[i]);
            }

            // new center will be the intersections of diagonals of corners
            // which is also intersection of diagonals of circle corners
            Vector2 newCenterPos = MathTools.QuadrilateralCenter(circleCornerPos);
            Debug.Log($"New Center: {newCenterPos}");

            Vector2 cornerPos = newCenterPos + (pos-newCenterPos) * Mathf.Sqrt(2);
            Debug.Log($"New corner: {cornerPos}");
            _corner.SetPosition(cornerPos);
        }

        private Vector2 GetCircleCornerPosition(Corner corner) {
            Vector2 centerPos = _center.GetPosition();
            Vector2 cornerPos = corner.GetPosition();
            return centerPos + (cornerPos-centerPos) * Mathf.Sqrt(2) / 2f;
        }

        public override Vector2 GetPosition() {
            Debug.Log($"Circle corner: {GetCircleCornerPosition(_corner)}");
            return GetCircleCornerPosition(_corner);
        }

        public Corner GetCorner() {
            return _corner;
        }
    }

    public Handle GetClosestHandle(Vector2 mousePos) {
        float minDist = Mathf.Infinity;
        Handle closest = this.none;
        foreach (Handle h in this.all) {
            float dist = Vector2.Distance(h.GetPosition(), mousePos);
            if (dist < minDist) {
                minDist = dist;
                closest = h;
            }
        }
        if (minDist > magneticDistance) 
            closest = this.none;

        Debug.Log(closest.GetType());
        return closest;
    }

    public void SetPosition(Handle handle, Vector2 pos) {
        if (handle.type == HandleType.None)
            return;

        handle.SetPosition(pos);
    }
}

public enum HandleType {
    None,
    Center,
    Corner,
    CircleCorner,
}

