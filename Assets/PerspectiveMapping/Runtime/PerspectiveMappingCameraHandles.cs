using UnityEngine;
using System;

public class MappingHandles {
    private Vector2[] _sources = new Vector2[4];

    // FIXME: remove setter side effect and use initialization function instead when resetting targets is necessary
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

    public Handle[] all = new Handle[5]; // Center and Target handles

    public void SelectNone()
    {
        this.current = this.none;
    }

    // index: 1-4 for target handles, 5 for center handle
    public void SelectHandle(int index)
    {
        if (index < 1 || index > 5)
            throw new Exception("Handle index out of range");

        if (index == 5) // Center handle
            this.current = this.center;
        else
            this.current = this.targets[index-1];
    }

    public void SelectClosestHandle(Vector2 mousePos)
    {
        this.current = GetClosestHandle(mousePos);
    }

    public Vector2[] GetTargetPositions() {
        Vector2[] result = new Vector2[4];
        for (int i = 0; i < this.targets.Length; i++) {
            result[i] = this.targets[i].GetPosition();
        }
        return result;
    }

    public void SetTargetPositions(Vector2[] values) {
        if (values.Length != 4)
            throw new Exception("Expecting exactly 4 positions");

        for (int i = 0; i < values.Length; i++) {
            this.targets[i].SetPosition(values[i]);
        }
    }

    public MappingHandles() {
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
            foreach (Target target in _targets) {
                target.SetPosition(target.GetPosition() + translation);
            }
        }

        // TODO: use homography instead
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

    public Vector2 GetDiagonalNotIncluding(Target target) {
        int n = this.targets.Length;
        int i = Array.IndexOf(this.targets, target);
        int prevTargetIndex = (i + n - 1) % n; // avoid modulo of negative number
        int nextTargetIndex = (i + 1) % n;
        Vector2[] targetPositions = GetTargetPositions();
        return targetPositions[prevTargetIndex] - targetPositions[nextTargetIndex];
    }

    public Vector2[] GetOppositeSides(Target target) {
        int n = this.targets.Length;
        int i = Array.IndexOf(this.targets, target);
        int prevTargetIndex = (i + n - 1) % n; // avoid modulo of negative number
        int nextTargetIndex = (i + 1) % n;
        int oppositeTargetIndex = (i + 2) % n;
        Vector2[] targetPositions = GetTargetPositions();
        Vector2[] oppositeSides = new Vector2[2];
        oppositeSides[0] = targetPositions[prevTargetIndex] - targetPositions[oppositeTargetIndex];
        oppositeSides[1] = targetPositions[nextTargetIndex] - targetPositions[oppositeTargetIndex];
        return oppositeSides;
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

        // Debug.Log($"closest handle type: {closest.GetType()}");
        return closest;
    }

    public bool isCrossingLine(Target target, Vector2 newTargetPosition, Vector2 lineDirection) {
            Vector2 normal = MathTools.Normal(lineDirection);
            Vector2 centerPos = center.GetPosition();
            Vector2 centerToTarget = target.GetPosition() - centerPos;
            Vector2 centerToCandidatePos = newTargetPosition - centerPos;

            // colinear product between normal to line should not change sign
            // otherwise the line is crossed.
            return Mathf.Sign(Vector2.Dot(centerToTarget, normal)) != Mathf.Sign(Vector2.Dot(centerToCandidatePos, normal));
    }

    public void SetPosition(Handle handle, Vector2 pos) {
        if (handle.type == HandleType.None)
            return;

        if (handle.type == HandleType.Target) {
            // Make sure targets do not cross diagonal
            Vector2 diag = GetDiagonalNotIncluding(handle as Target);
            if (isCrossingLine(handle as Target, pos, diag))
                return;

            // Make sure targets do not cross oposite sides
            Vector2[] oppositeSides = GetOppositeSides(handle as Target);
            if (isCrossingLine(handle as Target, pos, oppositeSides[0]) || isCrossingLine(handle as Target, pos, oppositeSides[1]))
                return;
        }

        handle.SetPosition(pos);
    }

    // Update sources without side effect of resetting targets
    // TODO: this shouldd be default behavior, and sources setter should be removed
    public void UpdateSources(Vector2[] new_sources) {
        _sources = new_sources;
    }
}

public enum HandleType {
    None,
    Center,
    Target,
}

