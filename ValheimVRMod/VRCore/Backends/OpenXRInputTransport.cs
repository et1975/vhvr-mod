using System;
using System.Collections.Generic;

namespace ValheimVRMod.VRCore.Backends
{
    public interface IOpenXRInputTransport
    {
        bool TryRead(out OpenXRInputSnapshot snapshot);
    }

    public sealed class OpenXRInputSnapshotDispatcher
    {
        private bool hasDispatchedFrame;
        private long lastDispatchedFrame;

        public bool TryDispatch(IOpenXRInputTransport transport, Action<OpenXRInputSnapshot> onSnapshot)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (onSnapshot == null) throw new ArgumentNullException(nameof(onSnapshot));
            if (!transport.TryRead(out OpenXRInputSnapshot snapshot)) return false;
            if (snapshot == null) throw new InvalidOperationException("The input transport returned a null snapshot.");
            if (hasDispatchedFrame && snapshot.FrameNumber <= lastDispatchedFrame) return false;

            hasDispatchedFrame = true;
            lastDispatchedFrame = snapshot.FrameNumber;
            onSnapshot(snapshot);
            return true;
        }
    }

    public sealed class OpenXRInputSnapshot
    {
        public long FrameNumber { get; }
        public OpenXRControllerSnapshot LeftController { get; }
        public OpenXRControllerSnapshot RightController { get; }

        public OpenXRInputSnapshot(long frameNumber, OpenXRControllerSnapshot leftController,
            OpenXRControllerSnapshot rightController)
        {
            if (frameNumber < 0) throw new ArgumentOutOfRangeException(nameof(frameNumber));
            FrameNumber = frameNumber;
            LeftController = leftController ?? throw new ArgumentNullException(nameof(leftController));
            RightController = rightController ?? throw new ArgumentNullException(nameof(rightController));
        }
    }

    public sealed class OpenXRControllerSnapshot
    {
        private readonly Dictionary<string, OpenXRInputControlSnapshot> controls;

        public bool IsConnected { get; }
        public OpenXRTrackingSnapshot Tracking { get; }

        public OpenXRControllerSnapshot(bool isConnected, OpenXRTrackingSnapshot tracking,
            IEnumerable<KeyValuePair<string, OpenXRInputControlSnapshot>> controls)
        {
            if (controls == null) throw new ArgumentNullException(nameof(controls));
            IsConnected = isConnected;
            Tracking = tracking;
            this.controls = new Dictionary<string, OpenXRInputControlSnapshot>(StringComparer.Ordinal);
            foreach (var control in controls)
            {
                if (string.IsNullOrEmpty(control.Key)) throw new ArgumentException("Control paths must not be empty.", nameof(controls));
                this.controls.Add(control.Key, control.Value);
            }
        }

        public bool TryGetControl(string path, out OpenXRInputControlSnapshot control)
        {
            if (IsConnected && !string.IsNullOrEmpty(path) && controls.TryGetValue(path, out control) && control.IsAvailable)
            {
                return true;
            }
            control = default;
            return false;
        }
    }

    public enum OpenXRInputControlType
    {
        Boolean,
        Scalar,
        Vector2
    }

    public readonly struct OpenXRInputControlSnapshot
    {
        public OpenXRInputControlType Type { get; }
        public bool IsAvailable { get; }
        public bool IsActive { get; }
        public bool BooleanValue { get; }
        public float ScalarValue { get; }
        public OpenXRVector2 Axis { get; }

        public OpenXRInputControlSnapshot(OpenXRInputControlType type, bool isAvailable, bool isActive,
            bool booleanValue, float scalarValue, OpenXRVector2 axis)
        {
            Type = type;
            IsAvailable = isAvailable;
            IsActive = isActive;
            BooleanValue = booleanValue;
            ScalarValue = scalarValue;
            Axis = axis;
        }
    }

    public readonly struct OpenXRTrackingSnapshot
    {
        public bool IsPositionValid { get; }
        public OpenXRVector3 Position { get; }
        public bool IsRotationValid { get; }
        public OpenXRQuaternion Rotation { get; }
        public bool IsLinearVelocityValid { get; }
        public OpenXRVector3 LinearVelocity { get; }
        public bool IsAngularVelocityValid { get; }
        public OpenXRVector3 AngularVelocity { get; }

        public OpenXRTrackingSnapshot(bool isPositionValid, OpenXRVector3 position, bool isRotationValid,
            OpenXRQuaternion rotation, bool isLinearVelocityValid, OpenXRVector3 linearVelocity,
            bool isAngularVelocityValid, OpenXRVector3 angularVelocity)
        {
            IsPositionValid = isPositionValid;
            Position = position;
            IsRotationValid = isRotationValid;
            Rotation = rotation;
            IsLinearVelocityValid = isLinearVelocityValid;
            LinearVelocity = linearVelocity;
            IsAngularVelocityValid = isAngularVelocityValid;
            AngularVelocity = angularVelocity;
        }
    }

    public readonly struct OpenXRVector2
    {
        public float X { get; }
        public float Y { get; }

        public OpenXRVector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public readonly struct OpenXRVector3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public OpenXRVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public readonly struct OpenXRQuaternion
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }

        public OpenXRQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }
}
