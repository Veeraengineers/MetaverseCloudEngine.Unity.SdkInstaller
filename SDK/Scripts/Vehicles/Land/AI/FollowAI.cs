﻿using UnityEngine;
using System.Collections;

namespace MetaverseCloudEngine.Unity.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehicleParent))]
    [AddComponentMenu(MetaverseConstants.MenuItems.MenuRootPath + "Vehicles/Land/AI/Vehicles - Follow AI", 0)]

    // Class for following AI
    public class FollowAI : MonoBehaviour
    {
        Transform tr;
        Rigidbody rb;
        VehicleParent vp;
        VehicleAssist va;
        public Transform target;
        Transform targetPrev;
        Rigidbody targetBody;
        Vector3 targetPoint;
        bool targetVisible;
        bool targetIsWaypoint;
        VehicleWaypoint targetWaypoint;

        public float followDistance;
        bool close;

        [Tooltip("Percentage of maximum speed to drive at")]
        [Range(0, 1)]
        public float speed = 1;
        float initialSpeed;
        float prevSpeed;
        public float targetVelocity = -1;
        float speedLimit = 1;
        float brakeTime;

        [Tooltip("Mask for which objects can block the view of the target")]
        public LayerMask viewBlockMask;
        Vector3 dirToTarget; // Normalized direction to target
        float lookDot; // Dot product of forward direction and dirToTarget
        float steerDot; // Dot product of right direction and dirToTarget

        float stoppedTime;
        float reverseTime;

        [Tooltip("Time limit in seconds which the vehicle is stuck before attempting to reverse")]
        public float stopTimeReverse = 1;

        [Tooltip("Duration in seconds the vehicle will reverse after getting stuck")]
        public float reverseAttemptTime = 1;

        [Tooltip("How many times the vehicle will attempt reversing before resetting, -1 = no reset")]
        public int resetReverseCount = 1;
        int reverseAttempts;

        [Tooltip("Seconds a vehicle will be rolled over before resetting, -1 = no reset")]
        public float rollResetTime = 3;
        float rolledOverTime;

        void Start() {
            tr = transform;
            rb = GetComponent<Rigidbody>();
            vp = GetComponent<VehicleParent>();
            va = GetComponent<VehicleAssist>();
            initialSpeed = speed;

            InitializeTarget();
        }

        void FixedUpdate() {
            if (target) {
                if (target != targetPrev) {
                    InitializeTarget();
                }

                targetPrev = target;

                // Is the target a waypoint?
                targetIsWaypoint = target.GetComponent<VehicleWaypoint>();
                // Can I see the target?
                targetVisible = !Physics.Linecast(tr.position, target.position, viewBlockMask);

                if (targetVisible || targetIsWaypoint) {
                    targetPoint = targetBody ? target.position + targetBody.GetLinearVelocity() : target.position;
                }

                if (targetIsWaypoint) {
                    // if vehicle is close enough to target waypoint, switch to the next one
                    if ((tr.position - target.position).sqrMagnitude <= targetWaypoint.radius * targetWaypoint.radius) {
                        target = targetWaypoint.nextPoint.transform;
                        targetWaypoint = targetWaypoint.nextPoint;
                        prevSpeed = speed;
                        speed = Mathf.Clamp01(targetWaypoint.speed * initialSpeed);
                        brakeTime = prevSpeed / speed;

                        if (brakeTime <= 1) {
                            brakeTime = 0;
                        }
                    }
                }

                brakeTime = Mathf.Max(0, brakeTime - Time.fixedDeltaTime);
                // Is the distance to the target less than the follow distance?
                close = (tr.position - target.position).sqrMagnitude <= Mathf.Pow(followDistance, 2) && !targetIsWaypoint;
                dirToTarget = (targetPoint - tr.position).normalized;
                lookDot = Vector3.Dot(vp.forwardDir, dirToTarget);
                steerDot = Vector3.Dot(vp.rightDir, dirToTarget);

                // Attempt to reverse if vehicle is stuck
                stoppedTime = Mathf.Abs(vp.localVelocity.z) < 1 && !close && vp.groundedWheels > 0 ? stoppedTime + Time.fixedDeltaTime : 0;

                if (stoppedTime > stopTimeReverse && reverseTime == 0) {
                    reverseTime = reverseAttemptTime;
                    reverseAttempts++;
                }

                // Reset if reversed too many times
                if (reverseAttempts > resetReverseCount && resetReverseCount >= 0) {
                    StartCoroutine(ReverseReset());
                }

                reverseTime = Mathf.Max(0, reverseTime - Time.fixedDeltaTime);

                if (targetVelocity > 0) {
                    speedLimit = Mathf.Clamp01(targetVelocity - vp.localVelocity.z);
                }
                else {
                    speedLimit = 1;
                }

                // Set accel input
                if (!close && (lookDot > 0 || vp.localVelocity.z < 5) && vp.groundedWheels > 0 && reverseTime == 0) {
                    vp.SetAccel(speed * speedLimit);
                }
                else {
                    vp.SetAccel(0);
                }

                // Set brake input
                if (reverseTime == 0 && brakeTime == 0 && !(close && vp.localVelocity.z > 0.1f)) {
                    if (lookDot < 0.5f && lookDot > 0 && vp.localVelocity.z > 10) {
                        vp.SetBrake(0.5f - lookDot);
                    }
                    else {
                        vp.SetBrake(0);
                    }
                }
                else {
                    if (reverseTime > 0) {
                        vp.SetBrake(1);
                    }
                    else {
                        if (brakeTime > 0) {
                            vp.SetBrake(brakeTime * 0.2f);
                        }
                        else {
                            vp.SetBrake(1 - Mathf.Clamp01(Vector3.Distance(tr.position, target.position) / Mathf.Max(0.01f, followDistance)));
                        }
                    }
                }

                // Set steer input
                if (reverseTime == 0) {
                    vp.SetSteer(Mathf.Abs(Mathf.Pow(steerDot, (tr.position - target.position).sqrMagnitude > 20 ? 1 : 2)) * Mathf.Sign(steerDot));
                }
                else {
                    vp.SetSteer(-Mathf.Sign(steerDot) * (close ? 0 : 1));
                }

                // Set ebrake input
                if ((close && vp.localVelocity.z <= 0.1f) || (lookDot <= 0 && vp.velMag > 20)) {
                    vp.SetEbrake(1);
                }
                else {
                    vp.SetEbrake(0);
                }
            }

            rolledOverTime = va.rolledOver ? rolledOverTime + Time.fixedDeltaTime : 0;

            // Reset if stuck rolled over
            if (rolledOverTime > rollResetTime && rollResetTime >= 0) {
                StartCoroutine(ResetRotation());
            }
        }

        IEnumerator ReverseReset() {
            reverseAttempts = 0;
            reverseTime = 0;
            yield return new WaitForFixedUpdate();
            tr.position = targetPoint;
            tr.rotation = Quaternion.LookRotation(targetIsWaypoint ? (targetWaypoint.nextPoint.transform.position - targetPoint).normalized : Vector3.forward, Vector3.up);
            rb.SetLinearVelocity(Vector3.zero);
            rb.angularVelocity = Vector3.zero;
        }

        IEnumerator ResetRotation() {
            yield return new WaitForFixedUpdate();
            tr.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            tr.Translate(Vector3.up, Space.World);
            rb.SetLinearVelocity(Vector3.zero);
            rb.angularVelocity = Vector3.zero;
        }

        public void InitializeTarget() {
            if (target) {
                // if target is a vehicle
                targetBody = target.GetTopmostParentComponent<Rigidbody>();

                // if target is a waypoint
                targetWaypoint = target.GetComponent<VehicleWaypoint>();
                if (targetWaypoint) {
                    prevSpeed = targetWaypoint.speed;
                }
            }
        }
    }
}
