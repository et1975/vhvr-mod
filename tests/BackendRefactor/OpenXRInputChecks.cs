using System;
using System.Collections.Generic;
using ValheimVRMod.VRCore.Backends;

internal static class OpenXRInputChecks
{
    public static int Run()
    {
        int checks = 0;
        void Require(bool condition, string label)
        {
            if (!condition) throw new Exception("FAIL: " + label);
            checks++;
        }

        const string actionPath = "/actions/Valheim/in/Grab";
        var pressed = new OpenXRInputControlSnapshot(
            OpenXRInputControlType.Boolean, true, true, true, 0f, default(OpenXRVector2));
        var tracking = new OpenXRTrackingSnapshot(
            true, new OpenXRVector3(1, 2, 3),
            false, default(OpenXRQuaternion),
            false, default(OpenXRVector3),
            true, new OpenXRVector3(4, 5, 6));
        var disconnected = new OpenXRControllerSnapshot(
            false, tracking,
            new Dictionary<string, OpenXRInputControlSnapshot> { [actionPath] = pressed });
        var connectedWithoutControl = new OpenXRControllerSnapshot(
            true, tracking, new Dictionary<string, OpenXRInputControlSnapshot>());

        var transport = new FakeInputTransport(
            new OpenXRInputSnapshot(9, disconnected, connectedWithoutControl),
            Snapshot(10), Snapshot(10), Snapshot(11));
        var dispatcher = new OpenXRInputSnapshotDispatcher();
        var deliveredFrames = new List<long>();
        bool disconnectedControlVisible = true;
        bool missingControlVisible = true;
        Require(dispatcher.TryDispatch(transport, snapshot =>
        {
            disconnectedControlVisible = snapshot.LeftController.TryGetControl(actionPath, out _);
            missingControlVisible = snapshot.RightController.TryGetControl(actionPath, out _);
            deliveredFrames.Add(snapshot.FrameNumber);
        }),
            "first snapshot in a frame is dispatched");
        Require(!disconnectedControlVisible, "disconnected controller does not expose a control");
        Require(!missingControlVisible, "missing control is reported as unavailable");
        Require(dispatcher.TryDispatch(transport, snapshot => deliveredFrames.Add(snapshot.FrameNumber)),
            "snapshot in the next frame is dispatched");
        Require(!dispatcher.TryDispatch(transport, snapshot => deliveredFrames.Add(snapshot.FrameNumber)),
            "duplicate snapshot in a frame is ignored");
        Require(dispatcher.TryDispatch(transport, snapshot => deliveredFrames.Add(snapshot.FrameNumber)),
            "snapshot in the next frame is dispatched");
        Require(deliveredFrames.Count == 3 && deliveredFrames[0] == 9 &&
                deliveredFrames[1] == 10 && deliveredFrames[2] == 11,
            "callback is delivered once per frame");

        var positionOnly = new OpenXRTrackingSnapshot(
            true, new OpenXRVector3(1, 2, 3),
            false, default(OpenXRQuaternion),
            false, default(OpenXRVector3),
            true, new OpenXRVector3(4, 5, 6));
        Require(positionOnly.IsPositionValid && !positionOnly.IsRotationValid &&
                !positionOnly.IsLinearVelocityValid && positionOnly.IsAngularVelocityValid,
            "position, rotation and velocity validity remain independent");

        var rotationAndLinear = new OpenXRTrackingSnapshot(
            false, default(OpenXRVector3),
            true, new OpenXRQuaternion(0, 0, 0, 1),
            true, new OpenXRVector3(7, 8, 9),
            false, default(OpenXRVector3));
        Require(!rotationAndLinear.IsPositionValid && rotationAndLinear.IsRotationValid &&
                rotationAndLinear.IsLinearVelocityValid && !rotationAndLinear.IsAngularVelocityValid,
            "rotation and linear velocity can be valid independently");

        return checks;
    }

    private static OpenXRInputSnapshot Snapshot(long frameNumber) =>
        new OpenXRInputSnapshot(frameNumber,
            new OpenXRControllerSnapshot(false, default, new Dictionary<string, OpenXRInputControlSnapshot>()),
            new OpenXRControllerSnapshot(false, default, new Dictionary<string, OpenXRInputControlSnapshot>()));

    private sealed class FakeInputTransport : IOpenXRInputTransport
    {
        private readonly Queue<OpenXRInputSnapshot> snapshots;

        public FakeInputTransport(params OpenXRInputSnapshot[] snapshots)
        {
            this.snapshots = new Queue<OpenXRInputSnapshot>(snapshots);
        }

        public bool TryRead(out OpenXRInputSnapshot snapshot)
        {
            if (snapshots.Count == 0)
            {
                snapshot = null!;
                return false;
            }
            snapshot = snapshots.Dequeue();
            return true;
        }
    }
}
