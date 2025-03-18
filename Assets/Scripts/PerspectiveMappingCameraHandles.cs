using UnityEngine;
using System;
using static MathTools;

class Handles {
    private Vector2[] _sources = new Vector2[4];

    public Vector2[] sources {
        get { return _sources; }
        set {
            if (value.Length != 4)
                throw new Exception("Handles constructor requires exactly 4 source points");

            for (int i = 0; i < value.Length; i++) {
                this._sources[i] = value[i];
                this.targets[i] = new Target(value[i]);
                this.all[i] = this.targets[i];
            }
        }
    }

    public Handle none = new None();
    public Target[] targets = new Target[4];
    public Center center;

    public Handle current;

    public float magneticDistance = 0.2f;

    public Matrix4x4 homography;

    private Handle[] all = new Handle[5]; // Center and Target handles

    public void SelectNone() {
        this.current = this.none;
    }

    public void SelectClosestHandle(Vector2 mousePos) {
        this.current = GetClosestHandle(mousePos);
    }

    public Handles() {
        int hIdx = 0;

        for (int i = 0; i < targets.Length; i++) {
            this.targets[i] = new Target(Vector2.zero);
            this.all[hIdx++] = this.targets[i];
        }

        this.center = new Center(this.targets);
        this.all[hIdx++] = this.center;

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

    public class None: Handle {
        public None() {
            this.type = HandleType.None;
        }
    }

    public class Center: Handle {
        private Target[] _targets;

        public Center(Target[] targets) {
            this.type = HandleType.Center;
            this._targets = targets;
        }

        // translate all targets
        public override void SetPosition(Vector2 pos) {
            Vector2 translation = pos - GetPosition();
            Debug.Log(translation);
            foreach (Target target in _targets) {
                target.SetPosition(target.GetPosition() + translation);
            }
        }

        // Intersection of diagonals between targets
        public override Vector2 GetPosition() {
            Vector2[] targetPositions = new Vector2[4];
            for (int i = 0; i < this._targets.Length; i++) {
                targetPositions[i] = this._targets[i].GetPosition();
            }
            return MathTools.QuadrilateralCenter(targetPositions);
        }
    }

    public class Target: Handle {
        public Target(Vector2 pos) {
            this.type = HandleType.Target;
            this.SetPosition(pos);
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

        Debug.Log($"closest handle type: {closest.GetType()}");
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
    Target,
}

