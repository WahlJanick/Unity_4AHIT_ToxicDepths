using UnityEngine;

namespace ToxicDepths
{
    internal static class ItemDropPhysics
    {
        internal static bool TryGetPose(Camera camera, Bounds bounds, out Pose pose)
        {
            pose = default;
            if (camera == null) return false;
            Vector3 forward = camera.transform.forward;
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f) flatForward = camera.transform.up;
            Quaternion rotation = Quaternion.LookRotation(flatForward.normalized);
            float distance = Mathf.Max(0.9f, Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.75f);
            Vector3 center = camera.transform.position + forward * distance;
            if (!IsClear(camera.transform.position, center, bounds.extents, rotation))
            {
                center = camera.transform.position + flatForward.normalized * distance;
                if (!IsClear(camera.transform.position, center, bounds.extents, rotation)) return false;
            }
            pose = new Pose(center - rotation * bounds.center, rotation);
            return true;
        }

        private static bool IsClear(Vector3 origin, Vector3 center, Vector3 halfSize, Quaternion rotation) =>
            !Physics.Linecast(origin, center, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            !Physics.CheckBox(center, halfSize, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        internal static Vector3 Velocity(Camera camera, float speed, float charge)
        {
            Vector3 velocity = camera.transform.forward * speed;
            velocity.y = Mathf.Max(velocity.y + 0.8f, Mathf.Lerp(1f, 2f, Mathf.Clamp01(charge)));
            return velocity;
        }

        internal static void Launch(Rigidbody body, Camera camera, float speed, float charge)
        {
            body.isKinematic = false;
            body.linearDamping = 0f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = Velocity(camera, speed, charge);
            body.angularVelocity = camera.transform.right * 2f;
        }
    }
}
